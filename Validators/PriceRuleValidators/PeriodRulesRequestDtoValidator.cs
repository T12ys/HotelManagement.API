using FluentValidation;
using HotelWebApplication.DTOs.PriceDTOs;

namespace HotelWebApplication.Validators.PriceRuleValidators;

public class PeriodRulesRequestDtoValidator : AbstractValidator<PeriodRulesRequestDto>
{
    public PeriodRulesRequestDtoValidator()
    {
        RuleFor(x => x.RoomTypeId)
            .GreaterThan(0).WithMessage("RoomTypeId is required.");

        // To can be default, in which case the backend will substitute the year ahead.
        // If the frontend passed To, we check that it is not earlier than From.
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.To != default)
            .WithMessage("To must be greater than or equal to From.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("PageSize must be between 1 and 50.");
    }
}