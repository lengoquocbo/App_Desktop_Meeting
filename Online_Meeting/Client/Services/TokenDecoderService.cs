using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Online_Meeting.Client.Services
{
    public class TokenDecoderService
    {
        public static TokenInfo DecodeToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                return new TokenInfo
                {
                    UserId = GetClaim(jwtToken, "sub") ?? GetClaim(jwtToken, "nameid"),
                    Username = GetClaim(jwtToken, "unique_name") ?? GetClaim(jwtToken, "username"),
                    Email = GetClaim(jwtToken, "email"),
                    Fullname = GetClaim(jwtToken, "name") ?? GetClaim(jwtToken, "fullname"),
                    Role = GetClaim(jwtToken, "role"),
                    IssuedAt = jwtToken.IssuedAt,
                    ExpiresAt = jwtToken.ValidTo,
                    IsExpired = jwtToken.ValidTo < DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding token: {ex.Message}");
                return null;
            }
        }

        public static bool IsTokenExpired(string token)
        {
            var tokenInfo = DecodeToken(token);
            return tokenInfo?.IsExpired ?? true;
        }

        public static DateTime? GetTokenExpirationDate(string token)
        {
            var tokenInfo = DecodeToken(token);
            return tokenInfo?.ExpiresAt;
        }

        private static string GetClaim(JwtSecurityToken token, string claimType)
        {
            return token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }
    }

    public class TokenInfo
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Role { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }
}