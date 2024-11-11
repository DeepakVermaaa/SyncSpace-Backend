using AutoMapper;
using SyncSpaceBackend.Models;
using SyncSpaceBackend.DTO;

namespace SyncSpaceBackend.Configurations
{
    public class AutoMapperConfig : Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<ChatRoom, ChatRoomDto>();
            CreateMap<ChatRoomDto, ChatRoom>();

            CreateMap<ProjectGroup, ProjectGroupDto>();
            CreateMap<ProjectGroupDto, ProjectGroup>();

            CreateMap<ChatMessage, ChatMessageDto>();
            CreateMap<ChatMessageDto, ChatMessage>();
        }
    }
}