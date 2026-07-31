using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NaPoso;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Middleware;
using NaPoso.Services;
using NaPoso.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Stripe;
using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using DotNetEnv;

Env.TraversePath().Load();
if (System.IO.File.Exists("/etc/secrets/.env")) Env.Load("/etc/secrets/.env");
if (System.IO.File.Exists("/app/.env")) Env.Load("/app/.env");

var builder = WebApplication.CreateBuilder(args);

// Structured logging with environment-aware sinks
builder.Logging.ClearProviders();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}
else
{
    builder.Logging.AddConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";
        options.DisableColors = true;
    });
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
}

// Email sender — provider-based (Brevo API or console fallback)
var emailProvider = Environment.GetEnvironmentVariable("EMAIL_PROVIDER") ?? builder.Configuration["Email:Provider"] ?? "console";
if (emailProvider.Equals("brevo", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
    {
        // Priority: BREVO_* (preporučeno) > postojeće EMAIL_BREVO_* > appsettings Email:Brevo > fallback
        var baseUrl =
            Environment.GetEnvironmentVariable("BREVO_BASE_URL")
            ?? Environment.GetEnvironmentVariable("EMAIL_BREVO_BASE_URL")
            ?? builder.Configuration["Email:Brevo:BaseUrl"]
            ?? "https://api.brevo.com/v3";
        if (!baseUrl.EndsWith("/", StringComparison.Ordinal)) baseUrl += "/";
        var apiKey =
            Environment.GetEnvironmentVariable("BREVO_API_KEY")
            ?? Environment.GetEnvironmentVariable("EMAIL_BREVO_API_KEY")
            ?? builder.Configuration["Email:Brevo:ApiKey"]
            ?? "";
        client.BaseAddress = new Uri(baseUrl);
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("api-key", apiKey);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}
else
{
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
}

// Legacy IEmailService registracija kroz Brevo API (koristi isti auth kao IEmailSender)
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
{
    var baseUrl =
        Environment.GetEnvironmentVariable("BREVO_BASE_URL")
        ?? Environment.GetEnvironmentVariable("EMAIL_BREVO_BASE_URL")
        ?? builder.Configuration["Email:Brevo:BaseUrl"]
        ?? "https://api.brevo.com/v3";
    if (!baseUrl.EndsWith("/", StringComparison.Ordinal)) baseUrl += "/";
    var apiKey =
        Environment.GetEnvironmentVariable("BREVO_API_KEY")
        ?? Environment.GetEnvironmentVariable("EMAIL_BREVO_API_KEY")
        ?? builder.Configuration["Email:Brevo:ApiKey"]
        ?? "";
    client.BaseAddress = new Uri(baseUrl);
    if (!string.IsNullOrWhiteSpace(apiKey))
        client.DefaultRequestHeaders.Add("api-key", apiKey);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Database — PostgreSQL via Npgsql
var connectionString = Environment.GetEnvironmentVariable("NEON_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<Korisnik, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(5);
});

// DataProtection: persist keys to Docker volume so sessions/cookies survive container rebuilds
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")))
    .SetApplicationName("NaPoso");

builder.Services.ConfigureApplicationCookie(options =>
{
    // ==========================================================
    // ISPRAVLJENO RBAC ZA NEPRIJAVLJENE KORISNIKE:
    // Eksplicitan LoginPath + AccessDeniedPath (sa returnUrl)
    // ==========================================================
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);

    // Zahtjevi koji ocekuju JSON (AJAX / API) vracaju 401/403,
    // za ostale (obicni browser posjetioci) nastavljamo sa default 302 redirectom
    static bool JsonRequest(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/api")) return true;
        if (request.Headers["X-Requested-With"] == "XMLHttpRequest") return true;
        var accept = request.Headers["Accept"].ToString();
        if (!string.IsNullOrEmpty(accept) && accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    options.Events.OnRedirectToLogin = context =>
    {
        if (JsonRequest(context.Request)) { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (JsonRequest(context.Request)) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Stripe configuration from environment / appsettings
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<StripeService>();
builder.Services.AddScoped<PaymentTransactionService>();
builder.Services.AddScoped<IStripeConnectService, StripeConnectService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IOglasService, OglasService>();
builder.Services.AddScoped<IRecenzijaService, RecenzijaService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("global", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "database", tags: new[] { "ready" });

// OpenTelemetry tracing with OTLP exporter
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("NaPoso")
        .ConfigureResource(r => r.AddService("NaPoso"))
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
        })
    );

// OpenTelemetry metrics + Prometheus exporter
builder.Services.AddOpenTelemetry()
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter()
    );

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiExplorerOptions>(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Seed roles and admin user
async Task CreateRoles(IServiceProvider serviceProvider, ILogger log)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = new[] { RoleConstants.Klijent, RoleConstants.Radnik, RoleConstants.Admin };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            log.LogInformation("[Seed] Created role: {Role}", role);
        }
    }
}

async Task CreateAdminUser(IServiceProvider serviceProvider, ILogger log)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<Korisnik>>();

    string adminEmail = builder.Configuration["Admin:Email"] ?? "admin@mail.com";
    string adminPassword = builder.Configuration["Admin:Password"] ?? "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var newAdmin = new Korisnik
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newAdmin, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin, RoleConstants.Admin);
            log.LogInformation("[Seed] Created admin user: {Email}", adminEmail);
        }
    }
    else
    {
        if (!await userManager.IsInRoleAsync(adminUser, RoleConstants.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, RoleConstants.Admin);
        }
    }

    // Seed test users
    var testUsers = new[]
    {
        new { Email = "radnik@mail.com", Password = builder.Configuration["Seed:RadnikPassword"] ?? "Test123!", Ime = "Test", Prezime = "Radnik", Role = RoleConstants.Radnik },
        new { Email = "klijent@mail.com", Password = builder.Configuration["Seed:KlijentPassword"] ?? "Test123!", Ime = "Test", Prezime = "Klijent", Role = RoleConstants.Klijent }
    };

    foreach (var tu in testUsers)
    {
        var user = await userManager.FindByEmailAsync(tu.Email);
        if (user == null)
        {
            var newUser = new Korisnik
            {
                UserName = tu.Email,
                Email = tu.Email,
                EmailConfirmed = true,
                Ime = tu.Ime,
                Prezime = tu.Prezime
            };

            var result = await userManager.CreateAsync(newUser, tu.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newUser, tu.Role);
                log.LogInformation("[Seed] Created {Role} user: {Email}", tu.Role, tu.Email);
            }
        }
    }
}

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Create/ensure database schema on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // ============================================================
        // AUTO-APPLY PENDING MIGRACIJE (ispravak za korisnika):
        // - MigrateAsync() primjenjuje SVE migracije koje nisu jos
        //   primijenjene na bazu (ukljucujuci nove kolone IsDeleted,
        //   DeletedAt i sve buduce migracije).
        // - EnsureCreated() samo KREIRA nove tabele, ali NE MIJENJA
        //   postojece (ne dodaje nove kolone), pa je zato uzrok
        //   prethodnih errora (nedostajala kolona IsDeleted nakon
        //   sto je model izmijenjen).
        // - Kombinacija oba je najsigurnija: MigrateAsync za schema,
        //   a EnsureCreated za slucaj da je baza potpuno prazna.
        // ============================================================
        await context.Database.MigrateAsync();

        try
        {
            // Fallback: MigrateAsync zahtjeva da migracije postoje u Assemblyju;
            // EnsureCreated ne skodi ako je MigrateAsync vec uspio.
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ensureEx)
        {
            logger.LogWarning(ensureEx, "EnsureCreated pao nakon MigrateAsync (ignorise se ako tabele vec postoje).");
        }

        var pending = await context.Database.GetPendingMigrationsAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();
        logger.LogInformation("[Startup] Database schema OK. Migrations applied: {AppliedCount}, pending: {PendingCount}",
            applied.Count(), pending.Count());

        // ============================================================
        // RAW SQL SCHEMA PATCHES: Add StripeSessionId column, make
        // StripeEventId nullable, and update indexes for idempotent
        // transaction creation from Stripe session_id.
        // These are safe to run multiple times (IF NOT EXISTS guards).
        // ============================================================
        try
        {
            // Add StripeSessionId column if missing
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'PaymentTransaction' AND column_name = 'StripeSessionId'
                    ) THEN
                        ALTER TABLE ""PaymentTransaction"" ADD COLUMN ""StripeSessionId"" text;
                    END IF;
                END $$;
            ");

            // Make StripeEventId nullable (ALTER COLUMN ... DROP NOT NULL is safe even if already nullable)
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE ""PaymentTransaction"" ALTER COLUMN ""StripeEventId"" DROP NOT NULL;
            ");

            // Nullify empty StripeEventId values
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""PaymentTransaction"" SET ""StripeEventId"" = NULL WHERE ""StripeEventId"" = '';
            ");

            // Drop old non-filtered StripeEventId unique index and recreate as filtered
            await context.Database.ExecuteSqlRawAsync(@"
                DROP INDEX IF EXISTS ""IX_PaymentTransaction_StripeEventId"";
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PaymentTransaction_StripeEventId""
                    ON ""PaymentTransaction"" (""StripeEventId"")
                    WHERE ""StripeEventId"" IS NOT NULL;
            ");

            // Create unique filtered index on StripeSessionId
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PaymentTransaction_StripeSessionId""
                    ON ""PaymentTransaction"" (""StripeSessionId"")
                    WHERE ""StripeSessionId"" IS NOT NULL;
            ");

            logger.LogInformation("[Startup] Schema patches (StripeSessionId, StripeEventId nullable, indexes) applied.");
        }
        catch (Exception patchEx)
        {
            logger.LogWarning(patchEx, "[Startup] Schema patches failed (may already be applied).");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Startup] Database setup / migrations failed.");
    }

    await CreateRoles(services, logger);
    await CreateAdminUser(services, logger);
    
    // Seed dummy data
    try 
    {
        bool seeded = await NaPoso.Data.DatabaseSeeder.SeedAsync(services);
        if (seeded)
        {
            logger.LogInformation("[Startup] Dummy data seeded successfully.");
        }
        else
        {
            logger.LogInformation("[Startup] Database already contains data. Seeding skipped.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Startup] Database seeding failed.");
    }
}

if (app.Environment.IsDevelopment())
{
    // U DEVELOPMENT okruženju:
    //  - Status stranice za 401/403 umjesto JSON errora
    //  - Developer exception page za prave exceptionse
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ============================================================
// RBAC ISPRAVAK: Redirect na Login / AccessDenied za 401/403
// (UMJESTO JSON ERRORA KOJI JE BIO VRACEN ZA NEPRIJAVLJENE)
// ============================================================
app.UseStatusCodePages(async context =>
{
    var ctx = context.HttpContext;
    var response = ctx.Response;
    var request = ctx.Request;

    // Preskoci API / AJAX zahtjeve (njima ostavljamo status kod)
    bool jeApiIliAjax =
        request.Path.StartsWithSegments("/api") ||
        request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
        (!string.IsNullOrEmpty(request.Headers["Accept"].ToString()) &&
         request.Headers["Accept"].ToString()!.Contains("application/json", StringComparison.OrdinalIgnoreCase));

    if (jeApiIliAjax) return;

    if (response.StatusCode == StatusCodes.Status401Unauthorized)
    {
        var returnUrl = request.PathBase + request.Path + request.QueryString;
        var loginUrl = "/Identity/Account/Login?returnUrl=" + Uri.EscapeDataString(returnUrl);
        response.Redirect(loginUrl);
    }
    else if (response.StatusCode == StatusCodes.Status403Forbidden)
    {
        response.Redirect("/Identity/Account/AccessDenied");
    }
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseSession();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // No checks — just confirms the app is running
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Stripe webhook endpoint
app.MapPost("/webhook/stripe", async (HttpContext context, ApplicationDbContext dbContext, IServiceProvider sp) =>
{
    var webhookLogger = sp.GetRequiredService<ILogger<Program>>();
    var json = await new StreamReader(context.Request.Body).ReadToEndAsync();

    webhookLogger.LogInformation("[Stripe Webhook] Raw payload received: {Payload}", json);

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(
            json,
            context.Request.Headers["Stripe-Signature"].FirstOrDefault(),
            builder.Configuration["Stripe:WebhookSecret"] ?? ""
        );
    }
    catch (StripeException ex)
    {
        webhookLogger.LogWarning(ex, "Invalid Stripe webhook signature");
        return Results.BadRequest("Invalid signature");
    }

    webhookLogger.LogInformation("Received Stripe event: {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

    switch (stripeEvent.Type)
    {
        case "checkout.session.completed":
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session != null)
            {
                webhookLogger.LogInformation(
                    "[Stripe Webhook] checkout.session.completed. SessionId={SessionId}, PaymentIntentId={PiId}, Metadata={@Metadata}",
                    session.Id, session.PaymentIntentId, session.Metadata);

                string? mdUserId = null;
                int? mdOglasId = null;
                string? mdRadnikId = null;
                long mdTipAmountFeninga = 0;

                if (session.Metadata != null)
                {
                    if (session.Metadata.TryGetValue("UserId", out var v))
                        mdUserId = v;
                    if (session.Metadata.TryGetValue("OglasId", out var oglasStr)
                        && int.TryParse(oglasStr, out var oglasParsed))
                        mdOglasId = oglasParsed;
                    if (session.Metadata.TryGetValue("RadnikId", out var r))
                        mdRadnikId = r;
                    if (session.Metadata.TryGetValue("TipAmountFeninga", out var tipStr)
                        && long.TryParse(tipStr, out var tipParsed))
                        mdTipAmountFeninga = tipParsed;
                }

                webhookLogger.LogInformation(
                    "[Stripe Webhook] Metadata parsed: UserId={U}, OglasId={O}, RadnikId={R}, TipAmountFeninga={Tip}",
                    mdUserId, mdOglasId, mdRadnikId, mdTipAmountFeninga);

                if (!string.IsNullOrEmpty(session.PaymentIntentId))
                {
                    var changed = await dbContext.ApplyCheckoutSessionMetadataAsync(
                        session.PaymentIntentId,
                        mdUserId,
                        mdOglasId,
                        mdRadnikId,
                        mdTipAmountFeninga);

                    webhookLogger.LogInformation(
                        "[Stripe Webhook] ApplyCheckoutSessionMetadataAsync completed. Changed={Changed}",
                        changed);
                }
            }
            break;
        }
        case "payment_intent.succeeded":
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent != null)
            {
                webhookLogger.LogInformation(
                    "[Stripe Webhook] payment_intent.succeeded. PaymentIntentId={PiId}, AmountReceived={A}, Currency={C}",
                    paymentIntent.Id, paymentIntent.AmountReceived, paymentIntent.Currency);

                await dbContext.HandleStripePaymentEventAsync(
                    paymentIntent.Id,
                    stripeEvent.Id,
                    PaymentStatus.Paid,
                    paymentIntent.AmountReceived,
                    paymentIntent.Currency);
            }
            break;
        }
        case "payment_intent.processing":
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent != null)
            {
                await dbContext.HandleStripePaymentEventAsync(
                    paymentIntent.Id,
                    stripeEvent.Id,
                    PaymentStatus.Pending,
                    paymentIntent.Amount,
                    paymentIntent.Currency);
            }
            break;
        }
        case "payment_intent.payment_failed":
        case "payment_intent.canceled":
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent != null)
            {
                await dbContext.HandleStripePaymentEventAsync(
                    paymentIntent.Id,
                    stripeEvent.Id,
                    PaymentStatus.Failed,
                    paymentIntent.Amount,
                    paymentIntent.Currency);
            }
            break;
        }
        case "charge.refunded":
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge != null)
            {
                var piId = charge.PaymentIntentId;
                if (!string.IsNullOrEmpty(piId))
                {
                    await dbContext.HandleStripePaymentEventAsync(
                        piId,
                        stripeEvent.Id,
                        PaymentStatus.Refunded,
                        charge.AmountRefunded,
                        charge.Currency);
                }
            }
            break;
        }
        case "account.updated":
        {
            var account = stripeEvent.Data.Object as Stripe.Account;
            if (account != null)
            {
                var connectService = sp.GetRequiredService<IStripeConnectService>();
                await connectService.UpdateAccountStatusAsync(account.Id);
            }
            break;
        }
        case "transfer.created":
        {
            var transfer = stripeEvent.Data.Object as Transfer;
            if (transfer != null)
            {
                webhookLogger.LogInformation(
                    "Transfer created: {TransferId}, Amount: {Amount} {Currency}, Destination: {Destination}",
                    transfer.Id, transfer.Amount, transfer.Currency, transfer.DestinationId);
            }
            break;
        }
        case "payout.paid":
            webhookLogger.LogInformation("Payout paid event received: {EventId}", stripeEvent.Id);
            break;
        case "payout.failed":
            webhookLogger.LogWarning("Payout FAILED event received: {EventId}", stripeEvent.Id);
            break;
        default:
            webhookLogger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
            break;
    }

    return Results.Ok();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// Expose Program class for WebApplicationFactory in tests
public partial class Program { }
