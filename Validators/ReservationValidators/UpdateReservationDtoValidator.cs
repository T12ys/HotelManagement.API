using FluentValidation;
using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.Enums;

namespace HotelWebApplication.Validators.ReservationValidators;

public class UpdateReservationDtoValidator : AbstractValidator<UpdateReservationDto>
{
    public UpdateReservationDtoValidator()
    {
        // If dates are passed, check that EndDate > StartDate
        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate!.Value)
                .GreaterThan(x => x.StartDate!.Value)
                .WithMessage("End date must be after start date.");
        });

        // You can't manually set the Confirmed status via PUT - /payments/mock does that.
        RuleFor(x => x.Status)
            .NotEqual(ReservationStatus.Confirmed)
            .When(x => x.Status.HasValue)
            .WithMessage("Cannot manually set status to Confirmed. Use the payment endpoint.");
    }
}