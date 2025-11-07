using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Services
{
    /// <summary>
    /// HTTP Handler tự động gắn token vào mọi request
    /// </summary>
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
            // Lấy token từ storage
            var token = _tokenService.GetAccessToken();

            // Nếu có token, gắn vào header
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Gửi request
            var response = await base.SendAsync(request, cancellationToken);

            // Nếu response là 401 (Unauthorized), có thể xử lý refresh token ở đây
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // TODO: Implement refresh token logic
                // var refreshed = await RefreshTokenAsync();
                // if (refreshed) retry request
            }

            return response;
        }
    }
}