using FluentValidation;
using HotelWebApplication.DTOs.RoomDTOs;

namespace HotelWebApplication.Validators.RoomValidators;

public class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100);
    }
}