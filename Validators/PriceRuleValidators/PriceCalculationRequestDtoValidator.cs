using FluentValidation;
using HotelWebApplication.DTOs.PriceDTOs;

namespace HotelWebApplication.Validators.PriceRuleValidators;

public class PriceCalculationRequestDtoValidator : AbstractValidator<PriceCalculationRequestDto>
{
    public PriceCalculationRequestDtoValidator()
    {
        RuleFor(x => x.RoomTypeId)
            .GreaterThan(0).WithMessage("RoomTypeId is required.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be greater than StartDate.");

        // Min 1 night
        RuleFor(x => x)
            .Must(x => (x.EndDate.Date - x.StartDate.Date).TotalDays >= 1)
            .WithMessage("Minimum stay is 1 night.");

        // Maximum a year ahead
        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow.Date.AddYears(1))
            .WithMessage("EndDate cannot be more than 1 year in the future.");
    }
}