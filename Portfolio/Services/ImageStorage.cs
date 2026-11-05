namespace Portfolio.Services
{
    public record ImageSaveResult(bool Success, string? Url, string? Error)
    {
        public static ImageSaveResult Ok(string url) => new(true, url, null);
        public static ImageSaveResult Fail(string error) => new(false, null, error);
    }

    /// <summary>
    /// Görsel dosyalarını wwwroot/uploads altına güvenli şekilde kaydeder ve siler.
    /// Controller'lar bu arayüze bağımlıdır, sınıfın kendisine değil; DI hangisini vereceğine karar verir.
    /// </summary>
    public interface IImageStorage
    {
        Task<ImageSaveResult> SaveAsync(IFormFile file, string folder);

        void Delete(string? url);
    }

    public class ImageStorage : IImageStorage
    {
        private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
        private const string UploadRoot = "uploads";

        // Uzantı -> dosyanın ilk baytlarındaki imza ("magic number").
        // Uzantı tek başına güvenilmez: "virus.exe" adı "kedi.jpg" yapılarak yüklenebilir.
        private static readonly Dictionary<string, byte[][]> Signatures = new()
        {
            [".jpg"] = [[0xFF, 0xD8, 0xFF]],
            [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
            [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
            [".webp"] = [[0x52, 0x49, 0x46, 0x46]], // "RIFF" (+ 8. bayttan itibaren "WEBP" ayrıca kontrol edilir)
        };

        private readonly IWebHostEnvironment _environment;

        public ImageStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<ImageSaveResult> SaveAsync(IFormFile file, string folder)
        {
            if (file is null || file.Length == 0)
                return ImageSaveResult.Fail("Dosya boş.");

            if (file.Length > MaxBytes)
                return ImageSaveResult.Fail("Görsel en fazla 2 MB olabilir.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Signatures.TryGetValue(extension, out var signatures))
                return ImageSaveResult.Fail("Yalnızca JPG, PNG veya WEBP yükleyebilirsiniz.");

            if (!await HasValidSignatureAsync(file, extension, signatures))
                return ImageSaveResult.Fail("Dosyanın içeriği uzantısıyla uyuşmuyor.");

            // Kullanıcının verdiği dosya adını asla kullanmıyoruz: çakışma ve "../" saldırısı riski.
            var safeFolder = new string(folder.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            var directory = Path.Combine(_environment.WebRootPath, UploadRoot, safeFolder);
            Directory.CreateDirectory(directory);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(directory, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return ImageSaveResult.Ok($"/{UploadRoot}/{safeFolder}/{fileName}");
        }

        public void Delete(string? url)
        {
            // Sadece bizim yüklediğimiz dosyaları sil; dış URL'lere ve wwwroot dışına dokunma.
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith($"/{UploadRoot}/", StringComparison.Ordinal))
                return;

            var uploadsRoot = Path.GetFullPath(Path.Combine(_environment.WebRootPath, UploadRoot));
            var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, url.TrimStart('/')));

            if (fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension, byte[][] signatures)
        {
            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header);

            var matches = signatures.Any(signature =>
                read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));

            if (matches && extension == ".webp")
            {
                matches = read >= 12 && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
            }

            return matches;
        }
    }
}
