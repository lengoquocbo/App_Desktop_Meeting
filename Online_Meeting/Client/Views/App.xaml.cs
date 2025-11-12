using Microsoft.Extensions.DependencyInjection;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Services;
using Online_Meeting.Client.Views.Dialogs;
using Online_Meeting.Client.Views.Pages;
using Refit;
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
            sc.AddSingleton<AuthHttpClientHandler>();
            sc.AddSingleton<TokenService>();
            sc.AddSingleton<AuthService>();
            sc.AddSingleton<MeetingSignalRServices>();
            sc.AddTransient<GroupChatView>();
            sc.AddTransient<CreateGroupDialog>();


            sc.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
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
