using Online_Meeting.Client.Models.Request;
using Online_Meeting.Client.Models.Responses;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

    }
}