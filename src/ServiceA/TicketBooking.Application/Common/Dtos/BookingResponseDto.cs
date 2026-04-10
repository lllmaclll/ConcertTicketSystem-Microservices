namespace TicketBooking.Application.Common.Dtos;

public class BookingResponseDto
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }        // 🔥 เพิ่ม: เพื่อบอกว่าใครจอง
    public string Username { get; set; } = string.Empty; // 🔥 เพิ่ม: เพื่อใช้แสดงชื่อในหน้าสรุป
    public string ConcertName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Price { get; set; } // 🔥 เพิ่มบรรทัดนี้
    public string Status { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime ExpiresAt { get; set; } // บอกหน้าบ้านด้วยว่าต้องจ่ายก่อนกี่โมง
}