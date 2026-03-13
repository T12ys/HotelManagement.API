using FluentValidation;
using HotelWebApplication.DTOs.ReservationDTOs;

namespace HotelWebApplication.Validators.ReservationValidators;

public class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
{
    public CreateReservationDtoValidator()
    {
        RuleFor(x => x.RoomTypeId)
            .GreaterThan(0).WithMessage("RoomId is required.");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.");

        RuleFor(x => x.CustomerPhone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(50).WithMessage("Phone must not exceed 50 characters.");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");

        RuleFor(x => x.GuestCount)
            .GreaterThan(0).WithMessage("Guest count must be at least 1.")
            .LessThanOrEqualTo(20).WithMessage("Guest count cannot exceed 20.");

        RuleForEach(x => x.Items).SetValidator(new ReservationItemDtoValidator());
    }
}

public class ReservationItemDtoValidator : AbstractValidator<ReservationItemDto>
{
    public ReservationItemDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(200).WithMessage("Item name must not exceed 200 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Item price cannot be negative.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Item quantity must be at least 1.");
    }
}