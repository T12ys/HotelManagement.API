using FluentValidation;
using HotelWebApplication.DTOs.RoomDTOs;
using System.Text.RegularExpressions;

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

        RuleFor(x => x.Translations)
            .Must(t => !t.ContainsKey("en") || Regex.IsMatch(t["en"].Trim(), @"^[a-zA-Z0-9\s\-]+$"))
            .WithMessage("English translation must contain only Latin letters, digits, spaces and hyphens.");
    }
}