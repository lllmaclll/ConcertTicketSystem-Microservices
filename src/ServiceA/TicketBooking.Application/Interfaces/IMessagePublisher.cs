using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TicketBooking.Application.Interfaces
{
    public interface IMessagePublisher
    {
        // คำสั่งให้ส่งข้อความเข้าคิว
        // เพิ่ม Task เข้าไปข้างหน้าเพื่อให้เป็น Async
        Task PublishTicketBookedEvent(Guid userId, string seatNumber, Guid concertId, Guid ticketId);
        // 🔥 เพิ่มอันนี้: แจ้งว่าจ่ายเงินแล้วนะ
        Task PublishPaymentConfirmedEvent(Guid userId, string seatNumber, string concertName, Guid ticketId);
    }
}