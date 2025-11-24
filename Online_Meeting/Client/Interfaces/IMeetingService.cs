using Online_Meeting.Client.Dtos.MeetingDto;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Interfaces
{
    public interface IMeetingService
    {

        //==================ROOMS==================

        [Get("/api/meetings/active-rooms")]
        Task<ApiResponse<List<MeetingRoom>>> GetActivecRooms();

        [Get("/api/meetings/my-rooms")]
        Task<ApiResponse<List<MeetingRoom>>> GetMyRooms();

        [Get("/api/meetings/rooms/{roomId}")]
        Task<ApiResponse<MeetingRoomDetail>> GetRoomById(Guid roomId);

        [Get("/api/meetings/rooms/key/{roomKey}")]
        Task<ApiResponse<MeetingRoomDetail>> GetRoomByKey(string RoomKey);

        [Post("/api/meetings/rooms")]
        Task<ApiResponse<CreateMeetingRoomResponse>> CreateRoom([Body] CreateRoomRequest request);

        [Put("/api/meetings/rooms/{roomId}")]
        Task<ApiResponse<MeetingRoom>> UpdateRoom(Guid roomId, [Body] UpdateRoomRequest request);

        [Delete("/api/meetings/rooms/{roomId}")]
        Task<ApiResponse<object>> DeleteRoom(Guid roomId);


        //==================ROOM PARTICIPANTS==================

        [Post("/api/meetings/rooms/{roomId}/join")]
        Task<ApiResponse<JoinMeetingResponse>>  JoinRoom(Guid roomId);

        [Post("/api/meetings/join/{roomKey}")]
        Task<ApiResponse<JoinMeetingResponse>> JoinRoomByKey(string roomKey);

        [Post("/api/meetings/rooms/{roomId}/leave")]
        Task<ApiResponse<object>> LeaveRoom(Guid roomId);

        [Get("/api/meetings/rooms/{roomId}/participants")]
        Task<ApiResponse<ParticipantsResponse>> GetParticipants(Guid roomId);

        [Post("/api/meetings/rooms/{roomId}/participants/{participantUserId}/kick")]
        Task<ApiResponse<object>> KickParticipant(Guid roomId, Guid participantUserId);

        [Get("/api/meetings/my-history")]
        Task<ApiResponse<List<MeetingHistory>>> GetMyMeetingHistory();

    }
}
