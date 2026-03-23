using FluentValidation;
using HotelWebApplication.DTOs.RoomDTOs;

namespace HotelWebApplication.Validators.RoomValidators;

public class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagDtoValidator()
    {
        RuleFor(x => x.Translations)
            .NotNull()
            .NotEmpty()
            .WithMessage("Translations are required.");

        RuleFor(x => x.Translations)
            .Must(t => t.ContainsKey("en") && !string.IsNullOrWhiteSpace(t["en"]))
            .WithMessage("English translation is required.");
    }
}