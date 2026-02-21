using FluentValidation;
using HotelWebApplication.DTOs.RoomDTOs;
namespace HotelWebApplication.Validators.RoomValidators;

public class CreateRoomTypeDtoValidator : AbstractValidator<CreateRoomTypeDto>
{
    public CreateRoomTypeDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.Capacity)
            .GreaterThan(0);

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0);

        RuleForEach(x => x.TagIds)
            .GreaterThan(0);
    }
}
