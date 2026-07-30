using NaPoso.Helpers;
using Xunit;

namespace NaPoso.Tests.Unit;

/// <summary>
/// Unit tests for FileValidationHelper covering magic number (file signature)
/// validation for supported and unsupported file formats.
/// </summary>
public class FileValidationHelperTests
{
    /// <summary>
    /// A valid PDF stream (starts with %PDF / 25 50 44 46) must be accepted.
    /// </summary>
    [Fact]
    public async Task ReturnsTrue_ForValidPdfFile()
    {
        // %PDF-1.4 header
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        using var stream = new MemoryStream(pdfBytes);

        var result = await FileValidationHelper.IsValidFileSignatureAsync(stream, ".pdf");

        Assert.True(result);
    }

    /// <summary>
    /// A valid PNG stream (starts with 89 50 4E 47) must be accepted.
    /// </summary>
    [Fact]
    public async Task ReturnsTrue_ForValidPngFile()
    {
        // PNG header: 89 50 4E 47 0D 0A 1A 0A
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(pngBytes);

        var result = await FileValidationHelper.IsValidFileSignatureAsync(stream, ".png");

        Assert.True(result);
    }

    /// <summary>
    /// An EXE file (MZ header) renamed to .pdf must be rejected.
    /// This tests that magic number validation catches spoofed extensions.
    /// </summary>
    [Fact]
    public async Task ReturnsFalse_ForFakePdfFile()
    {
        // MZ header (Windows executable)
        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(exeBytes);

        var result = await FileValidationHelper.IsValidFileSignatureAsync(stream, ".pdf");

        Assert.False(result);
    }

    /// <summary>
    /// A .docx file (ZIP-based, starts with PK / 50 4B) must be rejected
    /// because we only support PDF, PNG, and JPG formats.
    /// </summary>
    [Fact]
    public async Task ReturnsFalse_ForUnsupportedFormat()
    {
        // PK (ZIP/docx) header: 50 4B 03 04
        var docxBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 };
        using var stream = new MemoryStream(docxBytes);

        var result = await FileValidationHelper.IsValidFileSignatureAsync(stream, ".docx");

        Assert.False(result);
    }

    [Fact]
    public async Task ReturnsTrue_ForValidJpgFile()
    {
        // JPEG header: FF D8 FF E0
        var jpgBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        using var stream = new MemoryStream(jpgBytes);

        var result = await FileValidationHelper.IsValidFileSignatureAsync(stream, ".jpg");

        Assert.True(result);
    }

    [Fact]
    public async Task IsAllowedExtension_AcceptsPdfPngJpg()
    {
        Assert.True(FileValidationHelper.IsAllowedExtension(".pdf"));
        Assert.True(FileValidationHelper.IsAllowedExtension(".png"));
        Assert.True(FileValidationHelper.IsAllowedExtension(".jpg"));
        Assert.True(FileValidationHelper.IsAllowedExtension(".jpeg"));
        Assert.True(FileValidationHelper.IsAllowedExtension(".PDF")); // case-insensitive
    }

    [Fact]
    public async Task IsAllowedExtension_RejectsUnsupported()
    {
        Assert.False(FileValidationHelper.IsAllowedExtension(".docx"));
        Assert.False(FileValidationHelper.IsAllowedExtension(".exe"));
        Assert.False(FileValidationHelper.IsAllowedExtension(".txt"));
    }

    [Fact]
    public async Task GetContentType_ReturnsCorrectMimeTypes()
    {
        Assert.Equal("image/jpeg", FileValidationHelper.GetContentType(".jpg"));
        Assert.Equal("image/jpeg", FileValidationHelper.GetContentType(".jpeg"));
        Assert.Equal("image/png", FileValidationHelper.GetContentType(".png"));
        Assert.Equal("application/pdf", FileValidationHelper.GetContentType(".pdf"));
        Assert.Equal("application/octet-stream", FileValidationHelper.GetContentType(".xyz"));
    }
}
