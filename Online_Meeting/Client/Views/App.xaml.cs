using Microsoft.Extensions.DependencyInjection;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Services;
using Online_Meeting.Client.ViewModels;
using Online_Meeting.Client.Views.Dialogs;
using Online_Meeting.Client.Views.Pages;
using Refit;
using System.Numerics;
using System.Windows;

namespace Online_Meeting.Client.Views
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        [STAThread]
        public static void Main()
        {
            var app = new App();
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--remote-debugging-port=9222");
            app.InitializeComponent(); // load App.xaml
                app.Run();
            var TokenService = new TokenService();

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var sc = new ServiceCollection();

            // Đăng ký service dùng DI
            sc.AddTransient<AuthHttpClientHandler>();
            sc.AddSingleton<MeetingSignalRServices>();
            sc.AddSingleton<ChatSignalRService>();
            sc.AddTransient<GroupChatView>();
            sc.AddTransient<JoinGroupViewModel>();
            sc.AddTransient<CreateGroupDialog>();
            sc.AddTransient<JoinGroupDialog>();
            sc.AddTransient<ChatViewModel>(); 
            sc.AddTransient<AuthHttpClientHandler>();
            sc.AddTransient<TokenService>();
            sc.AddSingleton<ITokenService, TokenService>();
            sc.AddSingleton<IAuthService, AuthService>();
            sc.AddTransient<MeetingPreviewViewModel>();
            sc.AddSingleton<MeetingService>();
            sc.AddTransient<LoginViewModel>();
            sc.AddTransient<RegisterViewModel>();
            sc.AddTransient<MeetingPreview>();
            sc.AddTransient<MeetingRoomView>();
            sc.AddTransient<ScheduleView>();

            sc.AddHttpClient("PublicClient", client =>
            {
                client.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);

                //  QUAN TRỌNG: Header để vượt qua màn hình cảnh báo của Ngrok
                client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            });
            sc.AddHttpClient("AuthorizedClient", client =>
            {
                client.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);

                //  Cũng cần header Ngrok
                client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            })
            .AddHttpMessageHandler<AuthHttpClientHandler>();

            // Refit client cho IFileUploadService
            sc.AddRefitClient<IFileUploadService>()
                .ConfigureHttpClient(c => {
                    c.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                    // Thêm header Ngrok cho Refit
                    c.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                })
                .AddHttpMessageHandler<AuthHttpClientHandler>();

            // Refit client cho IChatService
            sc.AddRefitClient<IChatService>()
                .ConfigureHttpClient(c => {
                    c.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                    // Thêm header Ngrok cho Refit
                    c.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                })
                .AddHttpMessageHandler<AuthHttpClientHandler>();

            // Refit client cho IGroupService
            sc.AddRefitClient<IGroupService>()
                .ConfigureHttpClient(c => {
                    c.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                    // Thêm header Ngrok cho Refit
                    c.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                })
                .AddHttpMessageHandler<AuthHttpClientHandler>();

            sc.AddRefitClient<IMeetingService>()
                .ConfigureHttpClient(c => {
                    c.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                    // Thêm header Ngrok cho Refit
                    c.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                })
                .AddHttpMessageHandler<AuthHttpClientHandler>();
            
            Services = sc.BuildServiceProvider();

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
