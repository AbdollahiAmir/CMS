using BackEnd.IServices;
using SkiaSharp;

namespace BackEnd.Services
{
    public class FileService : IFileService
    {
        private const int MaxUploadBytes = 5 * 1024 * 1024;
        private const int MaxSourceSide = 10_000;
        private const long MaxSourcePixels = 40_000_000;
        private const int MainMaxSide = 1600;
        private const int ThumbMaxSide = 400;
        private const int MainQuality = 82;
        private const int ThumbQuality = 78;
        private const string ThumbSuffix = "_thumb";
        private const string StoredExtension = ".webp";

        private readonly string _productsFolder;
        private readonly string _categoriesFolder;
        private readonly string _brandsFolder;
        private readonly string _postsFolder;

        public FileService(IWebHostEnvironment env)
        {
            var uploadsRoot = Path.Combine(env.ContentRootPath, "PrivateUploads");

            _productsFolder = Path.Combine(uploadsRoot, "Products");
            _categoriesFolder = Path.Combine(uploadsRoot, "Categories");
            _brandsFolder = Path.Combine(uploadsRoot, "Brands");
            _postsFolder = Path.Combine(uploadsRoot, "Posts");
        }

        private static async Task<bool> HasValidSignatureAsync(IFormFile image, string extension)
        {
            var header = new byte[12];

            await using var stream = image.OpenReadStream();
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);

            if (read < header.Length)
            {
                return false;
            }

            return extension switch
            {
                ".jpg" or ".jpeg" =>
                    header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,

                ".png" =>
                    header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,

                ".webp" =>
                    header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                    && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,

                _ => false
            };
        }

        private static SKBitmap AutoOrient(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            if (origin == SKEncodedOrigin.TopLeft)
            {
                return bitmap;
            }

            var width = bitmap.Width;
            var height = bitmap.Height;

            var swapsSides = origin is SKEncodedOrigin.LeftTop
                or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom
                or SKEncodedOrigin.LeftBottom;

            var oriented = new SKBitmap(
                swapsSides ? height : width,
                swapsSides ? width : height,
                bitmap.ColorType,
                bitmap.AlphaType);

            using (var canvas = new SKCanvas(oriented))
            {
                canvas.Clear(SKColors.Transparent);

                switch (origin)
                {
                    case SKEncodedOrigin.TopRight:
                        canvas.Translate(width, 0);
                        canvas.Scale(-1, 1);
                        break;

                    case SKEncodedOrigin.BottomRight:
                        canvas.Translate(width, height);
                        canvas.RotateDegrees(180);
                        break;

                    case SKEncodedOrigin.BottomLeft:
                        canvas.Translate(0, height);
                        canvas.Scale(1, -1);
                        break;

                    case SKEncodedOrigin.LeftTop:
                    case SKEncodedOrigin.RightTop:
                        canvas.Translate(height, 0);
                        canvas.RotateDegrees(90);
                        break;

                    case SKEncodedOrigin.RightBottom:
                    case SKEncodedOrigin.LeftBottom:
                        canvas.Translate(0, width);
                        canvas.RotateDegrees(-90);
                        break;
                }

                canvas.DrawBitmap(bitmap, 0, 0);
            }

            return oriented;
        }

        private static byte[] EncodeWebp(SKBitmap source, int maxSide, int quality)
        {
            var longestSide = Math.Max(source.Width, source.Height);
            SKBitmap? resized = null;

            try
            {
                var target = source;

                if (longestSide > maxSide)
                {
                    var scale = (double)maxSide / longestSide;
                    var width = Math.Max(1, (int)Math.Round(source.Width * scale));
                    var height = Math.Max(1, (int)Math.Round(source.Height * scale));

                    resized = source.Resize(
                        new SKImageInfo(width, height, source.ColorType, source.AlphaType),
                        new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

                    target = resized ?? throw new ArgumentException("پردازش تصویر ممکن نشد");
                }

                using var image = SKImage.FromBitmap(target);
                using var data = image.Encode(SKEncodedImageFormat.Webp, quality);

                if (data is null)
                {
                    throw new ArgumentException("پردازش تصویر ممکن نشد");
                }

                return data.ToArray();
            }
            finally
            {
                resized?.Dispose();
            }
        }

        private static async Task<string> SaveImageAsync(IFormFile image, string folder, string folderName)
        {
            if (image is null || image.Length == 0)
            {
                throw new ArgumentException("تصویر خالی است");
            }

            if (image.Length > MaxUploadBytes)
            {
                throw new ArgumentException("حجم تصویر نباید بیشتر از 5 مگابایت باشد");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("فرمت تصویر پشتیبانی نمیشود");
            }

            if (!await HasValidSignatureAsync(image, extension))
            {
                throw new ArgumentException("محتوای فایل با فرمت تصویر مطابقت ندارد");
            }

            byte[] mainBytes;
            byte[] thumbBytes;

            await using (var input = image.OpenReadStream())
            {
                using var codec = SKCodec.Create(input);

                if (codec is null)
                {
                    throw new ArgumentException("تصویر معتبر نیست");
                }

                var info = codec.Info;

                if (info.Width <= 0 || info.Height <= 0
                    || info.Width > MaxSourceSide || info.Height > MaxSourceSide
                    || (long)info.Width * info.Height > MaxSourcePixels)
                {
                    throw new ArgumentException("ابعاد تصویر بیش از حد بزرگ است");
                }

                var origin = codec.EncodedOrigin;

                using var decoded = SKBitmap.Decode(codec);

                if (decoded is null)
                {
                    throw new ArgumentException("تصویر معتبر نیست");
                }

                var oriented = AutoOrient(decoded, origin);

                try
                {
                    mainBytes = EncodeWebp(oriented, MainMaxSide, MainQuality);
                    thumbBytes = EncodeWebp(oriented, ThumbMaxSide, ThumbQuality);
                }
                finally
                {
                    if (!ReferenceEquals(oriented, decoded))
                    {
                        oriented.Dispose();
                    }
                }
            }

            Directory.CreateDirectory(folder);

            var baseName = Guid.NewGuid().ToString("N");
            var mainPath = Path.Combine(folder, baseName + StoredExtension);
            var thumbPath = Path.Combine(folder, baseName + ThumbSuffix + StoredExtension);

            try
            {
                await File.WriteAllBytesAsync(mainPath, mainBytes);
                await File.WriteAllBytesAsync(thumbPath, thumbBytes);
            }
            catch
            {
                DeleteIfExists(mainPath);
                DeleteIfExists(thumbPath);
                throw;
            }

            return $"/Uploads/{folderName}/{baseName}{StoredExtension}";
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string ThumbnailFileName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName) + ThumbSuffix + StoredExtension;
        }

        private static Task<(Stream Stream, string ContentType)?> OpenImageFromFolderAsync(
            string? imagePath,
            string folder,
            bool thumbnail)
        {
            var notFound = Task.FromResult<(Stream Stream, string ContentType)?>(null);

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return notFound;
            }

            var fileName = Path.GetFileName(imagePath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return notFound;
            }

            var filePath = Path.Combine(folder, fileName);

            if (thumbnail)
            {
                var thumbPath = Path.Combine(folder, ThumbnailFileName(fileName));

                if (File.Exists(thumbPath))
                {
                    filePath = thumbPath;
                    fileName = Path.GetFileName(thumbPath);
                }
            }

            var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };

            if (contentType is null || !File.Exists(filePath))
            {
                return notFound;
            }

            Stream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            return Task.FromResult<(Stream Stream, string ContentType)?>((stream, contentType));
        }

        private static Task DeleteImageFromFolderAsync(string? imagePath, string folder)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return Task.CompletedTask;
            }

            var fileName = Path.GetFileName(imagePath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Task.CompletedTask;
            }

            DeleteIfExists(Path.Combine(folder, fileName));
            DeleteIfExists(Path.Combine(folder, ThumbnailFileName(fileName)));

            return Task.CompletedTask;
        }

        public Task<string> SaveProductImageAsync(IFormFile image)
        {
            return SaveImageAsync(image, _productsFolder, "Products");
        }

        public Task DeleteProductImageAsync(string? imagePath)
        {
            return DeleteImageFromFolderAsync(imagePath, _productsFolder);
        }

        public Task<(Stream Stream, string ContentType)?> OpenProductImageAsync(string? imagePath, bool thumbnail = false)
        {
            return OpenImageFromFolderAsync(imagePath, _productsFolder, thumbnail);
        }

        public Task<string> SaveBrandLogoAsync(IFormFile image)
        {
            return SaveImageAsync(image, _brandsFolder, "Brands");
        }

        public Task DeleteBrandLogoAsync(string? imagePath)
        {
            return DeleteImageFromFolderAsync(imagePath, _brandsFolder);
        }

        public Task<(Stream Stream, string ContentType)?> OpenBrandLogoAsync(string? imagePath, bool thumbnail = false)
        {
            return OpenImageFromFolderAsync(imagePath, _brandsFolder, thumbnail);
        }

        public Task<string> SaveCategoryImageAsync(IFormFile image)
        {
            return SaveImageAsync(image, _categoriesFolder, "Categories");
        }

        public Task DeleteCategoryImageAsync(string? imagePath)
        {
            return DeleteImageFromFolderAsync(imagePath, _categoriesFolder);
        }

        public Task<(Stream Stream, string ContentType)?> OpenCategoryImageAsync(string? imagePath, bool thumbnail = false)
        {
            return OpenImageFromFolderAsync(imagePath, _categoriesFolder, thumbnail);
        }

        public Task<string> SavePostFeaturedImageAsync(IFormFile image)
        {
            return SaveImageAsync(image, _postsFolder, "Posts");
        }

        public Task DeletePostFeaturedImageAsync(string? imagePath)
        {
            return DeleteImageFromFolderAsync(imagePath, _postsFolder);
        }

        public Task<(Stream Stream, string ContentType)?> OpenPostFeaturedImageAsync(string? imagePath, bool thumbnail = false)
        {
            return OpenImageFromFolderAsync(imagePath, _postsFolder, thumbnail);
        }
    }
}