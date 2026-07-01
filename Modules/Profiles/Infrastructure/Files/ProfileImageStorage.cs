namespace Glinter.Modules.Profiles.Infrastructure.Files;

public sealed class ProfileImageStorage(IWebHostEnvironment environment)
{
    public const long MaxImageBytes = 5L * 1024 * 1024;

    private static readonly Dictionary<string, (string Extension, string ContentType, byte[][] Signatures)>
        AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (".jpg", "image/jpeg", [[0xFF, 0xD8, 0xFF]]),
            ["image/png"] = (".png", "image/png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
            ["image/webp"] = (".webp", "image/webp", [[0x52, 0x49, 0x46, 0x46]])
        };

    private string StorageDirectory =>
        Path.Combine(environment.ContentRootPath, "Data", "Uploads", "Profiles");

    public async Task<(string FileName, long SizeBytes)> SaveAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException("A profile image is required.");
        if (file.Length > MaxImageBytes)
            throw new ValidationException("Profile images cannot exceed 5 MB.");
        if (!AllowedTypes.TryGetValue(file.ContentType, out var allowed))
            throw new ValidationException("Only JPEG, PNG, and WebP images are supported.");

        await using var source = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await source.ReadAsync(header, cancellationToken);
        var signatureMatches = allowed.Signatures.Any(signature =>
            bytesRead >= signature.Length &&
            header.AsSpan(0, signature.Length).SequenceEqual(signature));
        if (file.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
        {
            signatureMatches = signatureMatches &&
                               bytesRead >= 12 &&
                               header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        }
        if (!signatureMatches)
            throw new ValidationException("The uploaded file content does not match its image type.");

        Directory.CreateDirectory(StorageDirectory);
        var fileName = $"{Guid.NewGuid():N}{allowed.Extension}";
        var destinationPath = Path.Combine(StorageDirectory, fileName);
        source.Position = 0;
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);
        await source.CopyToAsync(destination, cancellationToken);
        return (fileName, file.Length);
    }

    public (string Path, string ContentType)? Find(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(safeName, fileName, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(safeName))
            return null;

        var extension = Path.GetExtension(safeName);
        var contentType = AllowedTypes.Values
            .FirstOrDefault(x => x.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
            .ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
            return null;

        var path = Path.Combine(StorageDirectory, safeName);
        return File.Exists(path) ? (path, contentType) : null;
    }
}
