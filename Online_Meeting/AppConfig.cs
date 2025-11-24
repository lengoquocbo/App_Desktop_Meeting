using Microsoft.Extensions.Configuration;
using System.IO;

namespace Online_Meeting.Client
{
    public static class AppConfig
    {
        public static IConfigurationRoot Configuration { get; }
        public const string BaseUrl = "https://keshia-overstrung-overnegligently.ngrok-free.dev";
        public static string serverOrigin = "";


        static AppConfig()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        // API Settings
        public static string ApiBaseUrl => Configuration["ApiBaseUrl"];
       
        public static int ApiTimeout => int.Parse(Configuration["ApiSettings:Timeout"] ?? "30");
        public static int MaxRetries => int.Parse(Configuration["ApiSettings:MaxRetries"] ?? "3");
        public static bool EnableLogging => bool.Parse(Configuration["ApiSettings:EnableLogging"] ?? "true");
        public static string MeetingBaseUrl => Configuration["MeetingBaseUrl"];
        public static string ChatBaseUrl => Configuration["ChatBaseUrl"];

        // Authentication
        public static string TokenKey => Configuration["Authentication:TokenKey"];
        public static string RefreshTokenKey => Configuration["Authentication:RefreshTokenKey"];
        public static int TokenExpirationMinutes => int.Parse(Configuration["Authentication:TokenExpirationMinutes"] ?? "60");
        public static int RememberMeDays => int.Parse(Configuration["Authentication:RememberMeDays"] ?? "30");

        // Endpoints
        public static class Endpoints
        {
            public static string Login => Configuration["Endpoints:Login"];
            public static string Register => Configuration["Endpoints:Register"];
            public static string Logout => Configuration["Endpoints:Logout"];
            public static string RefreshToken => Configuration["Endpoints:RefreshToken"];
            public static string ForgotPassword => Configuration["Endpoints:ForgotPassword"];
            public static string ResetPassword => Configuration["Endpoints:ResetPassword"];
            public static string ChangePassword => Configuration["Endpoints:ChangePassword"];
        }

        // Video Call Settings
        public static class VideoCall
        {
            public static string DefaultVideoQuality => Configuration["VideoCall:DefaultVideoQuality"];
            public static int MaxParticipants => int.Parse(Configuration["VideoCall:MaxParticipants"] ?? "10");
            public static bool EnableScreenShare => bool.Parse(Configuration["VideoCall:EnableScreenShare"] ?? "true");
            public static bool EnableChat => bool.Parse(Configuration["VideoCall:EnableChat"] ?? "true");
            public static bool RecordingEnabled => bool.Parse(Configuration["VideoCall:RecordingEnabled"] ?? "false");
        }

        // UI Settings
        public static class UI
        {
            public static string Theme => Configuration["UI:Theme"];
            public static string Language => Configuration["UI:Language"];
            public static bool AutoLogin => bool.Parse(Configuration["UI:AutoLogin"] ?? "false");
            public static bool MinimizeToTray => bool.Parse(Configuration["UI:MinimizeToTray"] ?? "true");
            public static bool NotificationEnabled => bool.Parse(Configuration["UI:NotificationEnabled"] ?? "true");
        }

        // Storage
        public static class Storage
        {
            public static string CachePath => Configuration["Storage:CachePath"];
            public static string LogPath => Configuration["Storage:LogPath"];
            public static int MaxLogSizeMB => int.Parse(Configuration["Storage:MaxLogSizeMB"] ?? "10");
            public static int MaxCacheSizeMB => int.Parse(Configuration["Storage:MaxCacheSizeMB"] ?? "100");
        }

        // WebRTC
        public static class WebRTC
        {
            public static string[] StunServers => Configuration.GetSection("WebRTC:StunServers").Get<string[]>();
            public static string TurnServer => Configuration["WebRTC:TurnServer"];
            public static string IceTransportPolicy => Configuration["WebRTC:IceTransportPolicy"];
        }

        // Features
        public static class Features
        {
            public static bool EnableSocialLogin => bool.Parse(Configuration["Features:EnableSocialLogin"] ?? "true");
            public static bool EnableGoogleLogin => bool.Parse(Configuration["Features:EnableGoogleLogin"] ?? "true");
            public static bool EnableMicrosoftLogin => bool.Parse(Configuration["Features:EnableMicrosoftLogin"] ?? "true");
            public static bool EnableRememberMe => bool.Parse(Configuration["Features:EnableRememberMe"] ?? "true");
            public static bool EnableBiometric => bool.Parse(Configuration["Features:EnableBiometric"] ?? "false");
            public static bool EnableOfflineMode => bool.Parse(Configuration["Features:EnableOfflineMode"] ?? "false");
        }

        // Debug
        public static class Debug
        {
            public static bool EnableDebugMode => bool.Parse(Configuration["Debug:EnableDebugMode"] ?? "true");
            public static bool ShowDetailedErrors => bool.Parse(Configuration["Debug:ShowDetailedErrors"] ?? "true");
            public static string LogLevel => Configuration["Debug:LogLevel"];
        }
    }
}