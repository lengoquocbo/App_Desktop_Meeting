using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Dtos.AccountDto;


namespace Online_Meeting.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _token;





        public AuthService(TokenService tokenService)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(AppConfig.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(AppConfig.ApiTimeout)
            };
            _token = tokenService;
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
                        _token.SaveTokens(data.UserName,data.Token, "");
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