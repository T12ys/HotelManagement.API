using FluentValidation;
using HotelWebApplication.DTOs.RoomDTOs;

namespace HotelWebApplication.Validators.RoomValidators;

public class UpdateRoomDtoValidator : AbstractValidator<UpdateRoomDto>
{
    public UpdateRoomDtoValidator()
    {
        Include(new CreateRoomDtoValidator());

        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
