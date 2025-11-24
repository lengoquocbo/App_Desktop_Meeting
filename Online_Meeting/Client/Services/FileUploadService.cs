//using Microsoft.Extensions.Logging;
//using Online_Meeting.Client.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//// Services/FileUploadService.cs
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using System.Text;
//using System.Threading.Tasks;
//using System.IO; 


//namespace Online_Meeting.Client.Services
//{
//    public class FileUploadService : IFileUploadService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly ITokenService _tokenService;
//        private readonly ILogger<FileUploadService> _logger;

//        public FileUploadService(
//            IHttpClientFactory httpClientFactory,
//            TokenService tokenService,
//            ILogger<FileUploadService> logger)
//        {
//            _httpClient = httpClientFactory.CreateClient("ApiClient");
//            _tokenService = tokenService;
//            _logger = logger;
//        }

//        public async Task<string> UploadFileAsync(string filePath, string fileType)
//        {
//            try
//            {
//                using var form = new MultipartFormDataContent();
//                using var fileStream = File.OpenRead(filePath);
//                using var streamContent = new StreamContent(fileStream);

//                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
//                    GetMimeType(fileType));

//                form.Add(streamContent, "file", Path.GetFileName(filePath));
//                form.Add(new StringContent(fileType), "fileType");

//                var response = await _httpClient.PostAsync("/api/files/upload", form);
//                response.EnsureSuccessStatusCode();

//                var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>();
//                return result?.FileUrl ?? throw new Exception("Upload failed");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "File upload failed");
//                throw;
//            }
//        }

//        public async Task<byte[]> DownloadFileAsync(string fileUrl)
//        {
//            try
//            {
//                return await _httpClient.GetByteArrayAsync(fileUrl);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "File download failed");
//                throw;
//            }
//        }

//        public async Task<bool> DeleteFil
//            catch (Exception ex)eAsync(string fileUrl)
//        {
//            try
//            {
//                var response = await _httpClient.DeleteAsync($"/api/files?url={Uri.EscapeDataString(fileUrl)}");
//                return response.IsSuccessStatusCode;
//            }
//            {
//                _logger.LogError(ex, "File deletion failed");
//                return false;
//            }
//        }

//        private string GetMimeType(string fileType)
//        {
//            return fileType.ToLower() switch
//            {
//                "image" => "image/*",
//                "file" => "application/octet-stream",
//                "video" => "video/*",
//                "audio" => "audio/*",
//                _ => "application/octet-stream"
//            };
//        }
//    }

//    public class FileUploadResponse
//    {
//        public string FileUrl { get; set; } = string.Empty;
//        public string FileName { get; set; } = string.Empty;
//    }
//}