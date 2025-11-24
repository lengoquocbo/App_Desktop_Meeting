using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Interfaces
{
    public interface ITokenService
    {
        void SaveTokens(Guid userId, string username, string accessToken, string refreshToken);
        void SetAccessToken(string accessToken);
        string GetAccessToken();
        string GetRefreshToken();
        bool IsAuthenticated();
        void ClearAllTokens();
        string GetUsername();
        Guid GetUserId();
    }
}
