namespace Online_Meeting.Client.Dtos.ChatDto
{
    public class CreateGroupChatRequest
    {
        public class CreateGroupRequest
        {
            public string GroupName { get; set; }
        }

        public class CreateGroupResponse
        {
            public string GroupId { get; set; }
            public string GroupName { get; set; }
        }

        
    }
}