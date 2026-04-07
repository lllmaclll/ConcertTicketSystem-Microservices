using FluentValidation;

namespace TicketBooking.Application.Commands;

public class BookTicketCommandValidator : AbstractValidator<BookTicketCommand>
{
    public BookTicketCommandValidator()
    {
        RuleFor(x => x.ConcertId)
            .NotEmpty().WithMessage("กรุณาระบุรหัสคอนเสิร์ต (Concert ID ห้ามว่าง)");

        RuleFor(x => x.SeatNumber)
            .NotEmpty().WithMessage("กรุณาระบุเลขที่นั่ง")
            .Matches(@"^[A-V0-9-]+$").WithMessage("รูปแบบเลขที่นั่งไม่ถูกต้อง (ตัวอย่าง: VIP-1, A-10)");
    }
}