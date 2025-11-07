using Microsoft.Extensions.DependencyInjection;
using Online_Meeting.Client.Services;
using System.Windows;
using Refit;
using Online_Meeting.Client.Interfaces;

namespace Online_Meeting.Client.Views
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var sc = new ServiceCollection();

            // Đăng ký service dùng DI
            sc.AddSingleton<AuthHttpClientHandler>();
            sc.AddSingleton<TokenService>();
            sc.AddSingleton<AuthService>();
            sc.AddSingleton<MeetingSignalRServices>();

            sc.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthHttpClientHandler>();

            sc.AddRefitClient<IMeetingService>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
                .AddHttpMessageHandler<AuthHttpClientHandler>();

            Services = sc.BuildServiceProvider();
        }
    }
}
