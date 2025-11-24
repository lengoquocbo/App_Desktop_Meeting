
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Online_Meeting.Client.Dtos.ChatDto.CreateGroupChatRequest;
using Online_Meeting.Client.Models;

namespace Online_Meeting.Client.Interfaces
{
    /// <summary>
    /// Group API Interface
    /// Token tự động được gắn vào header, không cần truyền parameter nữa
    /// </summary>
    public interface IGroupService

    {
        /// <summary>
        /// Tạo group mới
        /// </summary>
        
            [Post("/groupchat")]
            [Headers("Content-Type: application/json")]
            Task<ApiResponse<CreateGroupResponse>> CreateGroup([Body] CreateGroupRequest request);



        /// <summary>
        /// lấy danh sách group của user
        /// </summary>
        /// <returns></returns>
        [Get("/groupchat/my-groups")]
        [Headers("Content-Type: application/json")]
        Task<ApiResponse<List<ChatGroup>>> GetMyGroupsAsync();

        /// <summary>
        /// Join group chat
        /// </summary>

        /// <summary>
        /// Join group chat - chỉ cần groupId
        /// </summary>
        [Post("/groupchat/{groupId}/join")]
        [Headers("Content-Type: application/json")]
        Task<ApiResponse<object>> JoinGroupAsync(Guid groupId);
        /// <summary>
        /// Lấy danh sách groups
        /// </summary>
        //[Get("/api/groups")]
        //Task<ApiResponse<List<GroupInfo>>> GetGroups();

        ///// <summary>
        ///// Lấy chi tiết group
        ///// </summary>
        //[Get("/api/groups/{groupId}")]
        //Task<ApiResponse<GroupDetail>> GetGroupDetail(string groupId);

        ///// <summary>
        ///// Xóa group
        ///// </summary>
        //[Delete("/api/groups/{groupId}")]
        //Task<ApiResponse<object>> DeleteGroup(string groupId);

        ///// <summary>
        ///// Cập nhật group
        ///// </summary>
        //[Put("/api/groups/{groupId}")]
        //Task<ApiResponse<GroupInfo>> UpdateGroup(string groupId, [Body] UpdateGroupRequest request);

        ///// <summary>
        ///// Thêm member vào group
        ///// </summary>
        //[Post("/api/groups/{groupId}/members")]
        //Task<ApiResponse<object>> AddMember(string groupId, [Body] AddMemberRequest request);
    }
}