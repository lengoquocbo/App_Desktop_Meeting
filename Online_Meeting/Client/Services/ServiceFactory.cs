using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Services;
using Refit;
using System;
using System.Net.Http;

namespace Online_Meeting.Client.Services
{
    public static class ServiceFactory
    {
        public static IGroupService CreateGroupService(string token)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(AppConfig.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(AppConfig.ApiTimeout)
            };

            // Thêm Authorization header trực tiếp
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return RestService.For<IGroupService>(httpClient);
        }
    }
}