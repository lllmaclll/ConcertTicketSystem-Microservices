using FluentValidation;
using MediatR;
using TicketBooking.Application.Common.Exceptions;

namespace TicketBooking.Application.Common.Behaviors;

// คลาสนี้จะทำหน้าที่เป็น "ด่านตรวจ" สำหรับทุก Request ที่วิ่งผ่าน MediatR
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            
            // สั่งรัน Validator ทุกตัวที่เกี่ยวข้องกับ Request นี้
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                // ถ้าเจอจุดผิด แม้แต่จุดเดียว ให้โยน BadRequestException ทันที
                // ข้อความ Error จะถูกดึงมาจากที่เราตั้งไว้ใน Validator
                var errorMessages = string.Join(", ", failures.Select(f => f.ErrorMessage));
                throw new BadRequestException(errorMessages);
            }
        }

        // ถ้าผ่านด่านตรวจ ให้ส่งงานต่อไปยัง Handler จริงๆ
        return await next();
    }
}