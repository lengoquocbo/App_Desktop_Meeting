using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/ChatMessage.cs
namespace Online_Meeting.Client.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } // Từ User navigation
        public string UserAvatar { get; set; }
        public string TypeMessage { get; set; } // text, image, file
        public string Content { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime SendAt { get; set; }

        // UI properties
        public bool IsMyMessage { get; set; }
        public bool IsSending { get; set; } // Optimistic UI
        public bool IsError { get; set; }
        public bool IsEdited { get; set; }
    }
    public class SendMessageRequest
    {
        public string Content { get; set; }
        public string TypeMessage { get; set; } // "TEXT", "IMAGE", "FILE"
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
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