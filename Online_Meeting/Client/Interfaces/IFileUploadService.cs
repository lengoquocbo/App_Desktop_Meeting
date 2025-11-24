using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Refit;
using System.Net.Http;
namespace Online_Meeting.Client.Interfaces
{
    public interface IFileUploadService
    {
        [Multipart]
        [Post("/api/files/upload")]
        Task<ApiResponse<UploadResponse>> UploadFileAsync(
            [AliasAs("file")] StreamPart file,
            [AliasAs("fileType")] string fileType
        );

        [Get("/api/files/{fileUrl}")]
        Task<HttpResponseMessage> DownloadFileAsync(string fileUrl);

        [Delete("/api/files/{fileUrl}")]
        Task<ApiResponse<object>> DeleteFileAsync(string fileUrl);
    }

    // Response model
    public class UploadResponse
    {
        public string FileUrl { get; set; }
        public string FileName { get; set; }
    }
}
