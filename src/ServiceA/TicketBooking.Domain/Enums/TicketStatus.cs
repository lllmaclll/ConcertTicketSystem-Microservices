namespace TicketBooking.Domain.Enums
{
    public enum TicketStatus
    {
        Available = 0, // ว่าง
        Locked = 1,    // กำลังจอง (ล็อกโดย Redis)
        Booked = 2     // จ่ายเงินสำเร็จ
    }
}