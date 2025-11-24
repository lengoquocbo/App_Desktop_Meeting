using System;
﻿using Online_Meeting.Client.Interfaces;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Services
{
    public class AuthHttpClientHandler : DelegatingHandler
    {
        private readonly ITokenService _tokenService;

        public AuthHttpClientHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // =================================================================
            // 1. LOG REQUEST (Gửi đi)
            // =================================================================
            Debug.WriteLine($"\n┏━━━━━━━━━━━━━━ HTTP REQUEST ━━━━━━━━━━━━━━");
            Debug.WriteLine($"┃ URL: {request.RequestUri}");
            Debug.WriteLine($"┃ Method: {request.Method}");

            // Lấy token từ storage
            var token = _tokenService.GetAccessToken();

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Debug.WriteLine($"┃ Token: {token.Substring(0, Math.Min(20, token.Length))}...");
            }
            else
            {
                Debug.WriteLine("┃ WARNING: No token found!");
            }

            // [QUAN TRỌNG] Log nội dung JSON gửi đi (để kiểm tra DTO)
            if (request.Content != null)
            {
                var requestBody = await request.Content.ReadAsStringAsync();
                Debug.WriteLine($"┃ Body: {requestBody}");
            }
            Debug.WriteLine($"┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // =================================================================
            // 2. GỬI VÀ CHỜ KẾT QUẢ
            // =================================================================
            HttpResponseMessage response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken);

                // =================================================================
                // 3. LOG RESPONSE (Trả về)
                // =================================================================
                Debug.WriteLine($"\n┏━━━━━━━━━━━━━━ HTTP RESPONSE ━━━━━━━━━━━━━");
                Debug.WriteLine($"┃ URL: {request.RequestUri}"); // Nhắc lại URL để dễ nhìn
                Debug.WriteLine($"┃ Status: {(int)response.StatusCode} {response.StatusCode}");

                // [QUAN TRỌNG] Đọc nội dung Server trả về (kể cả lỗi)
                if (response.Content != null)
                {
                    // Load vào buffer để đọc không làm hỏng stream của Refit
                    await response.Content.LoadIntoBufferAsync();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"┃ Body: {responseBody}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"┃ ⚠️ ERROR REQUEST DETECTED");
                }
                Debug.WriteLine($"┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n┏━━━━━━━━━━━━━━ HTTP EXCEPTION ━━━━━━━━━━━━");
                Debug.WriteLine($"┃ Error: {ex.Message}");
                Debug.WriteLine($"┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                throw;
            }

            return response;
        }
    }
}