using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Services
{
    public class AuthHttpClientHandler : DelegatingHandler
    {
        private readonly TokenService _tokenService;

        public AuthHttpClientHandler(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // DEBUG: Log request
            Debug.WriteLine($"=== HTTP REQUEST ===");
            Debug.WriteLine($"URL: {request.RequestUri}");
            Debug.WriteLine($"Method: {request.Method}");

            // Lấy token từ storage
            var token = _tokenService.GetAccessToken();

            // DEBUG: Log token
            Debug.WriteLine($"Token exists: {!string.IsNullOrEmpty(token)}");
            if (!string.IsNullOrEmpty(token))
            {
                Debug.WriteLine($"Token preview: {token.Substring(0, Math.Min(30, token.Length))}...");
            }

            // Nếu có token, gắn vào header
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Debug.WriteLine($"Authorization header set: Bearer {token.Substring(0, 10)}...");
            }
            else
            {
                Debug.WriteLine("WARNING: No token found!");
            }

            // Gửi request
            var response = await base.SendAsync(request, cancellationToken);

            // DEBUG: Log response
            Debug.WriteLine($"Response Status: {response.StatusCode}");
            Debug.WriteLine($"===================");

            // Nếu response là 401 (Unauthorized)
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Debug.WriteLine("⚠️ UNAUTHORIZED - Token may be invalid or expired");
                // TODO: Implement refresh token logic
            }

            return response;
        }
    }
}