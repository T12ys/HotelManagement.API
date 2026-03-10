using FluentValidation;
using HotelWebApplication.DTOs.PriceDTOs;
using HotelWebApplication.Enums;

namespace HotelWebApplication.Validators.PriceRuleValidators;

public class CreatePriceRuleDtoValidator : AbstractValidator<CreatePriceRuleDto>
{
    public CreatePriceRuleDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Value must be greater than 0.");

        // Если процент — не может быть больше 100
        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100).WithMessage("Percent value cannot exceed 100.")
            .When(x => x.IsPercent);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");

        // Для SpecialDate StartDate должен совпадать с EndDate
        RuleFor(x => x.EndDate)
            .Equal(x => x.StartDate)
            .WithMessage("For SpecialDate rules StartDate and EndDate must be the same.")
            .When(x => x.RuleType == RuleType.SpecialDate);

        // Дата не может быть в прошлом
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("StartDate cannot be in the past.");

        // Максимум год вперёд
        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow.Date.AddYears(1))
            .WithMessage("EndDate cannot be more than 1 year in the future.");
    }
}