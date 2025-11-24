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
            sc.AddSingleton<TokenService>();
            sc.AddSingleton<AuthService>();
            sc.AddSingleton<MeetingSignalRServices>();
            sc.AddSingleton<ChatSignalRService>();

            sc.AddTransient<GroupChatView>();
            sc.AddTransient<JoinGroupViewModel>();


            sc.AddTransient<CreateGroupDialog>();
            sc.AddTransient<JoinGroupDialog>();

            sc.AddTransient<ChatViewModel>(); 



            sc.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthHttpClientHandler>();

            // Refit client cho IFileUploadService
            sc.AddRefitClient<IFileUploadService>()
              .ConfigureHttpClient(c => c.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
              .AddHttpMessageHandler<AuthHttpClientHandler>();

            // Refit client cho IChatService
            sc.AddRefitClient<IChatService>()
              .ConfigureHttpClient(c => c.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
              .AddHttpMessageHandler<AuthHttpClientHandler>();

            // Refit client cho IGroupService
            sc.AddRefitClient<IGroupService>()
              .ConfigureHttpClient(c => c.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
              .AddHttpMessageHandler<AuthHttpClientHandler>();

            // Refit client cho IMeetingService
            sc.AddRefitClient<IMeetingService>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
                .AddHttpMessageHandler<AuthHttpClientHandler>();

            Services = sc.BuildServiceProvider();

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
