using FluentValidation;
using HotelWebApplication.DTOs.PriceDTOs;
using HotelWebApplication.Enums;

namespace HotelWebApplication.Validators.PriceRuleValidators;

public class UpdatePriceRuleDtoValidator : AbstractValidator<UpdatePriceRuleDto>
{
    public UpdatePriceRuleDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Value must be greater than 0.");

        // If percentage - cannot be more than 100
        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100).WithMessage("Percent value cannot exceed 100.")
            .When(x => x.IsPercent);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");

        // For SpecialDate, StartDate must match EndDate
        RuleFor(x => x.EndDate)
            .Equal(x => x.StartDate)
            .WithMessage("For SpecialDate rules StartDate and EndDate must be the same.")
            .When(x => x.RuleType == RuleType.SpecialDate);

        // Maximum a year ahead
        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow.Date.AddYears(1))
            .WithMessage("EndDate cannot be more than 1 year in the future.");
    }
}