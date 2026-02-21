using AutoMapper;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Models;

namespace HotelWebApplication.Mappings;

public class RoomMappingProfile : Profile
{
    public RoomMappingProfile()
    {
        // ROOM

        CreateMap<Room, RoomResponseDto>();

        CreateMap<CreateRoomDto, Room>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoomType, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        CreateMap<UpdateRoomDto, Room>()
            .ForMember(dest => dest.RoomType, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());


        // ROOM TYPE

        CreateMap<RoomType, RoomTypeResponseDto>();

        CreateMap<CreateRoomTypeDto, RoomType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.Photos, opt => opt.Ignore())
            .ForMember(dest => dest.Rooms, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        CreateMap<UpdateRoomTypeDto, RoomType>()
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.Photos, opt => opt.Ignore())
            .ForMember(dest => dest.Rooms, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        // TAG

        CreateMap<Tag, TagResponseDto>();

        CreateMap<CreateTagDto, Tag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoomTypes, opt => opt.Ignore());


        // ROOM PHOTO

        CreateMap<RoomPhoto, RoomPhotoResponseDto>();
    }
}
