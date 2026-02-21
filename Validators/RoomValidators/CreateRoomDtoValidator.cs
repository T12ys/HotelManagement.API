using FluentValidation;
using HotelWebApplication.DTOs.RoomDTOs;

namespace HotelWebApplication.Validators.RoomValidators;

public class CreateRoomDtoValidator : AbstractValidator<CreateRoomDto>
{
    public CreateRoomDtoValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.RoomTypeId)
            .GreaterThan(0);

        RuleFor(x => x.Floor)
            .GreaterThanOrEqualTo(0);
    }
}
