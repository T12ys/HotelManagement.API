using AutoMapper;
using HotelWebApplication.DTOs.PriceDTOs;
using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Models;

namespace HotelWebApplication.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
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


        // PRICE RULE

        CreateMap<PriceRule, PriceRuleResponseDto>();


        CreateMap<CreatePriceRuleDto, PriceRule>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoomType, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdatePriceRuleDto, PriceRule>()
            .ForMember(dest => dest.RoomTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.RoomType, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // RESERVATION
        CreateMap<Reservation, ReservationResponseDto>()
            .ForMember(dest => dest.RoomNumber,
                opt => opt.MapFrom(src => src.Room != null ? src.Room.Number : src.RoomId.ToString()))
            .ForMember(dest => dest.NightsCount,
                opt => opt.MapFrom(src => (int)(src.EndDate - src.StartDate).TotalDays))
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.ReservationItems));

        // RESERVATION ITEM
        CreateMap<ReservationItem, ReservationItemResponseDto>()
            .ForMember(dest => dest.Total,
                opt => opt.MapFrom(src => src.Price * src.Quantity));
    }
}