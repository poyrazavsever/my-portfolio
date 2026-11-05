namespace Portfolio.Services
{
    public record CvSaveResult(bool Success, string? RelativePath, string? Error);

    /// <summary>
    /// CV (PDF) dosyasını wwwroot DIŞINDA, App_Data/cv altında saklar.
    /// wwwroot'taki her dosya URL'i bilen herkese doğrudan açıktır; burada ise dosyaya
    /// yalnızca DefaultController.DownloadCv üzerinden, bizim belirlediğimiz ad ve türle erişilir.
    /// </summary>
    public interface ICvStorage
    {
        Task<CvSaveResult> SaveAsync(IFormFile file);

        bool Exists(string? relativePath);

        /// <summary>Diskteki tam yol; dosya yoksa veya yol güvenli değilse null.</summary>
        string? GetFullPath(string? relativePath);

        void Delete(string? relativePath);
    }

    public class CvStorage : ICvStorage
    {
        public const long MaxBytes = 5 * 1024 * 1024; // 5 MB
        private const string Folder = "cv";

        // Her PDF dosyası "%PDF-" ile başlar.
        private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

        private readonly string _root;

        public CvStorage(IWebHostEnvironment environment)
        {
            // ContentRootPath = projenin kök klasörü (wwwroot'un bir üstü).
            _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data"));
        }

        public async Task<CvSaveResult> SaveAsync(IFormFile file)
        {
            if (file is null || file.Length == 0)
                return new(false, null, "Dosya boş.");

            if (file.Length > MaxBytes)
                return new(false, null, "CV en fazla 5 MB olabilir.");

            // 1) Uzantı  2) Tarayıcının bildirdiği tür  3) Dosyanın gerçek içeriği.
            // İlk ikisi istemciden gelir ve sahte olabilir; asıl güvence üçüncüsü.
            if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                return new(false, null, "Yalnızca PDF dosyası yükleyebilirsiniz.");

            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                return new(false, null, "Dosya türü PDF değil.");

            var header = new byte[PdfSignature.Length];
            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAsync(header);
                if (read < header.Length || !header.AsSpan().SequenceEqual(PdfSignature))
                    return new(false, null, "Dosyanın içeriği geçerli bir PDF değil.");
            }

            var directory = Path.Combine(_root, Folder);
            Directory.CreateDirectory(directory);

            var relativePath = $"{Folder}/{Guid.NewGuid():N}.pdf";
            var fullPath = Path.Combine(_root, relativePath);

            await using (var target = new FileStream(fullPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(target);
            }

            return new(true, relativePath, null);
        }

        public bool Exists(string? relativePath) => GetFullPath(relativePath) is not null;

        public string? GetFullPath(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            // Yol DB'den gelse bile App_Data dışına çıkmasına izin verme ("../" saldırısına karşı).
            var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
            if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return null;

            return File.Exists(fullPath) ? fullPath : null;
        }

        public void Delete(string? relativePath)
        {
            var fullPath = GetFullPath(relativePath);
            if (fullPath is not null)
            {
                File.Delete(fullPath);
            }
        }
    }
}
