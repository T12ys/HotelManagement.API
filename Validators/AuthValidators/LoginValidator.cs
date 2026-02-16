using FluentValidation;
using HotelWebApplication.DTOs.AuthDTOs;

namespace HotelWebApplication.Validators.AuthValidators;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
