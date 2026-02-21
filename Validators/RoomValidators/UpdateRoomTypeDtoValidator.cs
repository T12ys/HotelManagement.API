using FluentValidation;
using HotelWebApplication.DTOs.RoomDTOs;

namespace HotelWebApplication.Validators.RoomValidators;

public class UpdateRoomTypeDtoValidator : AbstractValidator<UpdateRoomTypeDto>
{
    public UpdateRoomTypeDtoValidator()
    {
        Include(new CreateRoomTypeDtoValidator());

        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
