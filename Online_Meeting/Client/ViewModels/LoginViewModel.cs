using Online_Meeting.Client.Dtos.AccountDto;
using Online_Meeting.Client.Dtos;
using Online_Meeting.Client.Services;
using System.Threading.Tasks;
using Online_Meeting.Client.Interfaces;

namespace Online_Meeting.Client.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        private string _username;
        private string _password;
        private bool _isLoading;
        private string _errorMessage;

        public LoginViewModel(IAuthService authService)
        {
            var tokenService = new TokenService(); // tạo instance TokenService
            _authService = authService;
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public async Task<AuthResponse> LoginAsync()
        {
            ErrorMessage = string.Empty;

            // Validation client
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Vui lòng nhập tên đăng nhập";
                return new AuthResponse { IsSuccess = false, ErrorMessage = ErrorMessage };
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu";
                return new AuthResponse { IsSuccess = false, ErrorMessage = ErrorMessage };
            }

            IsLoading = true;

            try
            {
                var request = new LoginRequest
                {
                    Username = Username,
                    Password = Password
                };

                var response = await _authService.LoginAsync(request);

                if (!response.IsSuccess)
                {
                    ErrorMessage = response.ErrorMessage;
                }

                
                return response;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
