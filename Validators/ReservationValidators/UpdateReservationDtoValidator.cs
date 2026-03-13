using FluentValidation;
using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.Enums;

namespace HotelWebApplication.Validators.ReservationValidators;

public class UpdateReservationDtoValidator : AbstractValidator<UpdateReservationDto>
{
    public UpdateReservationDtoValidator()
    {
        // Если переданы даты — проверяем что EndDate > StartDate
        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate!.Value)
                .GreaterThan(x => x.StartDate!.Value)
                .WithMessage("End date must be after start date.");
        });

        // Нельзя вручную выставить статус Confirmed через PUT — это делает /payments/mock
        RuleFor(x => x.Status)
            .NotEqual(ReservationStatus.Confirmed)
            .When(x => x.Status.HasValue)
            .WithMessage("Cannot manually set status to Confirmed. Use the payment endpoint.");
    }
}