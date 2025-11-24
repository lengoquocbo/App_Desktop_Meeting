using Online_Meeting.Client.Dtos.AccountDto;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Share.Models;
using SIPSorcery.OpenAIWebRTC;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace Online_Meeting.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _token;

        public AuthService(ITokenService tokenService, IHttpClientFactory httpClientFactory)
            
        {
            _token = tokenService;
            _httpClient = httpClientFactory.CreateClient("PublicClient");

            if (_httpClient.DefaultRequestHeaders.Contains("ngrok-skip-browser-warning"))
            {
                System.Diagnostics.Debug.WriteLine("✅ PublicClient đã có header Ngrok");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ LỖI: PublicClient CHƯA có header Ngrok -> Kiểm tra lại App.xaml.cs");

                // [Giải pháp tạm thời] Nếu cấu hình DI lỗi, ta add thủ công ở đây để chạy được đã
                _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // DEBUG: Log URL thực tế
                var fullUrl = new Uri(_httpClient.BaseAddress, AppConfig.Endpoints.Login);
                Debug.WriteLine($"BaseAddress: {_httpClient.BaseAddress}");
                Debug.WriteLine($"Endpoint: {AppConfig.Endpoints.Login}");
                Debug.WriteLine($"Full URL: {fullUrl}");
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(AppConfig.Endpoints.Login, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    data.IsSuccess = true;
                  // GỌI  deserialize thành công
                if (!string.IsNullOrEmpty(data.Token))

                    {
                        _token.SaveTokens(data.userId, data.UserName,data.Token, "");

                        // TokenStorageService.SaveToken(data.Token);
                        Debug.WriteLine("Token đã được lưu vào bộ nhớ cục bộ.");
                    }

                    return data;
                }

                return new AuthResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Đăng nhập thất bại: {response.StatusCode} - {responseContent}"
                };
            }
            catch (Exception ex)
            {
               Debug.WriteLine($"Exception: {ex.Message}");
        return new AuthResponse
        {
            IsSuccess = false,
            ErrorMessage = $"Lỗi kết nối: {ex.Message}"
        };
            }
        }

        // DANG KY NHA
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(AppConfig.Endpoints.Register, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var data = JsonSerializer.Deserialize<AuthResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (response.IsSuccessStatusCode)
                {
                    data.IsSuccess = true;
                    return data;
                }

                return new AuthResponse
                {
                    IsSuccess = false,
                    ErrorMessage = data?.ErrorMessage ?? "Đăng ký thất bại"
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lỗi: {ex.Message}"
                };
            }
        }

    }
}