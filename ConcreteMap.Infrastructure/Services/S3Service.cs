using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace ConcreteMap.Infrastructure.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _serviceUrl;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration["S3Settings:BucketName"];
            _serviceUrl = configuration["S3Settings:ServiceUrl"];
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            // 1. Генерируем уникальный ключ для хранения (чтобы не было коллизий)
            var extension = Path.GetExtension(fileName);
            var key = $"{Guid.NewGuid()}{extension}";

            // 2. Формируем заголовок, чтобы при скачивании имя было красивым
            // Используем UrlEncode для поддержки русских названий
            var encodedFileName = WebUtility.UrlEncode(fileName);
            
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                CannedACL = S3CannedACL.PublicRead, // Публичный доступ
            };
            
            // Магия: говорим браузеру "Сохрани этот файл вот с этим именем"
            request.Headers.ContentDisposition = $"attachment; filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}";

            await _s3Client.PutObjectAsync(request);

            return $"{_serviceUrl}/{_bucketName}/{key}";
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath;
            var key = path.Replace($"/{_bucketName}/", "").Trim('/');

            await _s3Client.DeleteObjectAsync(_bucketName, key);
        }

        public async Task<Stream> GetFileStreamAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return null;

            // Вытаскиваем ключ из URL
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath;
            // Удаляем начальный слэш и имя бакета, если оно есть в пути (зависит от структуры URL)
            // Для Timeweb Path Style: /bucketname/key
            var key = path.Replace($"/{_bucketName}/", "").Trim('/');

            try
            {
                var response = await _s3Client.GetObjectAsync(_bucketName, key);
                return response.ResponseStream;
            }
            catch (AmazonS3Exception)
            {
                return null;
            }
        }
    }
}