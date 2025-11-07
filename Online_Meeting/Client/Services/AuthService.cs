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

        public AuthService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(AppConfig.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(AppConfig.ApiTimeout)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
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
                        TokenStorageService.SaveToken(data.Token);
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