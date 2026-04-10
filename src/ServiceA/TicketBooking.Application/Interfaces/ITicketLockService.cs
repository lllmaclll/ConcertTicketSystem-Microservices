using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TicketBooking.Application.Interfaces
{
    public interface ITicketLockService
    {
        // คืนค่า true ถ้าล็อกสำเร็จ, false ถ้ามีคนล็อกไปแล้ว
        Task<bool> AcquireLockAsync(Guid concertId, string seatNumber, TimeSpan expiration);
        // 🔥 เพิ่มคำสั่งปลดล็อก
        Task ReleaseLockAsync(Guid concertId, string seatNumber);
    }
}