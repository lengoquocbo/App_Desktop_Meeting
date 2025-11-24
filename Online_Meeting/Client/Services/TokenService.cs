using CredentialManagement;
using Online_Meeting.Client.Interfaces;

namespace Online_Meeting.Client.Services
{
    public class TokenService : ITokenService
    {
        private const string ACCESS_TOKEN_TARGET = "MyApp_AccessToken";
        private const string REFRESH_TOKEN_TARGET = "MyApp_RefreshToken";
        private const string USER_ID_TARGET = "MyApp_UserId";

        private readonly object _lock = new object();
        private string _accessToken; // Cache in memory
        private string _username;
        private Guid _userId;

        public TokenService()
        {
            LoadTokensFromCredentialManager();
        }

        /// Lưu cả Access Token và Refresh Token
        public void SaveTokens(Guid userId, string username, string accessToken, string refreshToken)
        {
            lock (_lock)
            {
                _userId = userId;
                _username = username;
                _accessToken = accessToken;

                // Lưu Access Token
                SaveCredential(ACCESS_TOKEN_TARGET, username, accessToken);

                // Lưu Refresh Token
                SaveCredential(REFRESH_TOKEN_TARGET, username, refreshToken);
                SaveCredential(USER_ID_TARGET, username, userId.ToString());
            }
        }

        /// Lưu chỉ Access Token (khi refresh)
        public void SetAccessToken(string accessToken)
        {
            lock (_lock)
            {
                _accessToken = accessToken;

                if (!string.IsNullOrEmpty(_username))
                {
                    SaveCredential(ACCESS_TOKEN_TARGET, _username, accessToken);
                }
            }
        }

        /// Lấy Access Token
        public string GetAccessToken()
        {
            lock (_lock)
            {
                return _accessToken;
            }
        }

        /// Lấy Refresh Token
        public string GetRefreshToken()
        {
            lock (_lock)
            {
                return LoadCredential(REFRESH_TOKEN_TARGET);
            }
        }

        public Guid GetUserId()
        {
            lock (_lock)
            {
                return _userId;
            }
        }

        /// Lấy username đã lưu
        public string GetUsername()
        {
            lock (_lock)
            {
                return _username;
            }
        }

        /// Kiểm tra đã có token hay chưa
        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(_accessToken);
        }

        /// Xóa tất cả tokens
        public void ClearAllTokens()
        {
            lock (_lock)
            {
                _accessToken = null;
                _username = null;
                _userId = Guid.Empty;

                DeleteCredential(ACCESS_TOKEN_TARGET);
                DeleteCredential(REFRESH_TOKEN_TARGET);
                DeleteCredential(USER_ID_TARGET);
            }
        }

        /// Load tokens từ Credential Manager khi khởi động
        private void LoadTokensFromCredentialManager()
        {
            try
            {
                _accessToken = LoadCredential(ACCESS_TOKEN_TARGET);

                // Lấy username từ credential
                using (var cred = new Credential { Target = ACCESS_TOKEN_TARGET })
                {
                    if (cred.Load())
                    {
                        _username = cred.Username;
                    }
                }

                string userIdString = LoadCredential(USER_ID_TARGET);
                if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out Guid parsedId))
                {
                    _userId = parsedId;
                }
                else
                {
                    // Trường hợp không tìm thấy hoặc lỗi, gán Empty để tránh null reference sau này
                    _userId = Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tokens: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private void SaveCredential(string target, string username, string password)
        {
            try
            {
                using (var cred = new Credential
                {
                    Target = target,
                    Username = username,
                    Password = password,
                    Type = CredentialType.Generic,
                    PersistanceType = PersistanceType.LocalComputer
                })
                {
                    cred.Save();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving credential {target}: {ex.Message}");
                throw;
            }
        }

        private string LoadCredential(string target)
        {
            try
            {
                using (var cred = new Credential { Target = target })
                {
                    if (cred.Load())
                    {
                        return cred.Password;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading credential {target}: {ex.Message}");
            }

            return null;
        }

        private void DeleteCredential(string target)
        {
            try
            {
                using (var cred = new Credential { Target = target })
                {
                    cred.Delete();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting credential {target}: {ex.Message}");
            }
        }

        


        #endregion
    }
}
