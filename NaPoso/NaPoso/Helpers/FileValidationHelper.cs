using System;
using System.IO;
using System.Threading.Tasks;

namespace NaPoso.Helpers
{
    /// <summary>
    /// Provides file validation utilities including extension whitelisting,
    /// magic number (file signature) verification, and MIME type mapping.
    /// </summary>
    public static class FileValidationHelper
    {
        // Allowed extensions (case-insensitive comparison used at call sites)
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".pdf"
        };

        // Magic number signatures for supported file types
        private static readonly byte[] JpgSignature = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47 };
        private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46 }; // %PDF

        /// <summary>
        /// Checks whether the given file extension is in the allowed set.
        /// </summary>
        public static bool IsAllowedExtension(string extension)
        {
            return AllowedExtensions.Contains(extension);
        }

        /// <summary>
        /// Reads the first bytes of the stream and verifies that the file signature
        /// matches the expected magic numbers for the declared extension.
        /// The stream position is reset after reading.
        /// </summary>
        public static async Task<bool> IsValidFileSignatureAsync(Stream stream, string extension)
        {
            if (stream == null || !stream.CanRead)
                return false;

            var originalPosition = stream.Position;
            try
            {
                stream.Position = 0;

                // Read up to 4 bytes (max signature length)
                var headerBytes = new byte[4];
                var bytesRead = await stream.ReadAsync(headerBytes, 0, 4);

                if (bytesRead < 3)
                    return false;

                var ext = extension.ToLowerInvariant();

                return ext switch
                {
                    ".jpg" or ".jpeg" => StartsWithSignature(headerBytes, bytesRead, JpgSignature),
                    ".png" => StartsWithSignature(headerBytes, bytesRead, PngSignature),
                    ".pdf" => StartsWithSignature(headerBytes, bytesRead, PdfSignature),
                    _ => false
                };
            }
            finally
            {
                // Reset stream position so subsequent operations work correctly
                if (stream.CanSeek)
                    stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Maps a file extension to its correct MIME Content-Type.
        /// Returns "application/octet-stream" for unrecognized extensions.
        /// </summary>
        public static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        private static bool StartsWithSignature(byte[] header, int bytesRead, byte[] signature)
        {
            if (bytesRead < signature.Length)
                return false;

            for (int i = 0; i < signature.Length; i++)
            {
                if (header[i] != signature[i])
                    return false;
            }

            return true;
        }
    }
}
