using Online_Meeting.Client.Dtos.AccountDto;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

    }
}