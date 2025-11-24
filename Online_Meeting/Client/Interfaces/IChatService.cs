using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Online_Meeting.Client.Models;
using Refit;

namespace Online_Meeting.Client.Interfaces
{
    public interface IChatService
    {
        // ------------------- SEND MESSAGE -------------------
        // POST: /message/groups/{groupId}/send
        [Post("/message/groups/{groupId}/send")]
        Task<ApiResponse<ChatMessage>> SendMessageAsync(
            Guid groupId,
            [Body] SendMessageRequest request);


        // ------------------- GET MESSAGES IN GROUP -------------------
        // GET: /message/groups/{groupId}
        [Get("/message/groups/{groupId}")]
        Task<GroupMessagesResponse> GetGroupMessagesAsync(
            Guid groupId,
            [Query] int page = 1,
            [Query] int pageSize = 50);


        // ------------------- GET SINGLE MESSAGE -------------------
        // GET: /message/{messageId}
        [Get("/message/{messageId}")]
        Task<ApiResponse<MessageResponse>> GetMessageByIdAsync(Guid messageId);


        // ------------------- UPDATE MESSAGE -------------------
        // PUT: /message/{messageId}
        [Put("/message/{messageId}")]
        Task<ApiResponse<MessageResponse>> UpdateMessageAsync(
            Guid messageId,
            [Body] UpdateMessageRequest request);


        // ------------------- DELETE MESSAGE -------------------
        // DELETE: /message/{messageId}
        [Delete("/message/{messageId}")]
        Task<ApiResponse<object>> DeleteMessageAsync(Guid messageId);


        // ------------------- SEARCH MESSAGE -------------------
        // GET: /message/groups/{groupId}/search?keyword=...
        [Get("/message/groups/{groupId}/search")]
        Task<ApiResponse<List<ChatMessage>>> SearchMessagesAsync(
            Guid groupId,
            [Query] string keyword);


        // ------------------- GET MESSAGE BY TYPE -------------------
        // GET: /message/groups/{groupId}/type/{typeMessage}
        [Get("/message/groups/{groupId}/type/{typeMessage}")]
        Task<ApiResponse<List<ChatMessage>>> GetMessagesByTypeAsync(
            Guid groupId,
            string typeMessage);
    }


    // ====================== DTO CLASSES ======================

    public class ChatResponse
    {
        public List<ChatMessage> Data { get; set; }
        public MessagePagination Pagination { get; set; }
    }

    public class MessageResponse
    {
        public ChatMessage Data { get; set; }
    }

    public class UpdateMessageRequest
    {
        public string Content { get; set; }
    }

    public class GroupMessagesResponse
    {
        public bool Success { get; set; }
        public List<ChatMessage> Data { get; set; }
        public MessagePagination Pagination { get; set; }
    }

    public class MessagePagination
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasMore { get; set; }
    }
}
