using Online_Meeting.Client.Dtos.AccountDto;
using Online_Meeting.Client.Services;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Online_Meeting.Client.Interfaces;

namespace Online_Meeting.Client.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        private string _username;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private bool _isLoading;
        private string _errorMessage;

        public RegisterViewModel()
        {
            var tokenService = new TokenService(); // tạo instance TokenService
            _authService = new AuthService(tokenService);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
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

        public async Task<AuthResponse> RegisterAsync()
        {
            ErrorMessage = string.Empty;

            // Client validation
            if (string.IsNullOrWhiteSpace(Username))
                return Error("Vui lòng nhập tên đăng nhập");

            if (Username.Length < 3)
                return Error("Tên đăng nhập phải có ít nhất 3 ký tự");

            if (string.IsNullOrWhiteSpace(Email))
                return Error("Vui lòng nhập email");

            if (!IsValidEmail(Email))
                return Error("Email không hợp lệ");

            if (string.IsNullOrWhiteSpace(Password))
                return Error("Vui lòng nhập mật khẩu");

            if (Password.Length < 6)
                return Error("Mật khẩu phải có ít nhất 6 ký tự");

            if (Password != ConfirmPassword)
                return Error("Mật khẩu xác nhận không khớp");

            IsLoading = true;

            try
            {
                var request = new RegisterRequest
                {
                    Username = Username,
                    Email = Email,
                    Password = Password
                };

                var response = await _authService.RegisterAsync(request);

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

        private AuthResponse Error(string msg)
        {
            ErrorMessage = msg;
            return new AuthResponse { IsSuccess = false, ErrorMessage = msg };
        }

        private bool IsValidEmail(string email)
        {
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern);
        }
    }
}
