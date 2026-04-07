using FluentValidation;

namespace TicketBooking.Api.Controllers;

public class RegisterValidator : AbstractValidator<UserDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username ห้ามว่าง")
            .MinimumLength(3).WithMessage("Username ต้องมีอย่างน้อย 3 ตัวอักษร");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password ห้ามว่าง")
            .MinimumLength(6).WithMessage("Password ต้องมีอย่างน้อย 6 ตัวอักษร");
    }
}