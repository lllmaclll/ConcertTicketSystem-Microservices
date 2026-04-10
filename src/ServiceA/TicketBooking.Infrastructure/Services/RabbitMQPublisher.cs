using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TicketBooking.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace TicketBooking.Infrastructure.Services
{
    public class RabbitMQPublisher : IMessagePublisher
    {
        // เพิ่ม IHttpContextAccessor เพื่อดึง Header
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RabbitMQPublisher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // ฟังก์ชันช่วยดึง Correlation ID จาก Request ปัจจุบัน
        private string GetCorrelationId()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].ToString() 
                ?? Guid.NewGuid().ToString();
        }

        public async Task PublishTicketBookedEvent(Guid userId, string seatNumber, Guid concertId, Guid ticketId)
        {
            // ดึง Correlation ID จาก Header ที่ Gateway ส่งมา
            var correlationId = GetCorrelationId(); // 🔥 ดึงรหัสติดตาม

            // 1. เชื่อมต่อ RabbitMQ
            var factory = new ConnectionFactory { HostName = "rabbitmq_broker" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // 2. ประกาศสร้างคิวชื่อ "email_queue" (ถ้ายังไม่มีให้สร้าง)
            await channel.QueueDeclareAsync(queue: "email_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

            // 3. ปั้นข้อมูลที่จะส่งเป็น JSON
            // 🔥 ต้องใส่ ConcertId ลงไปในก้อนข้อมูลนี้ด้วย!
            var message = new
            {
                CorrelationId = correlationId, // 🔥 ส่งรหัสติดตามไปด้วย
                UserId = userId,
                SeatNumber = seatNumber,
                ConcertId = concertId,
                BookingId = ticketId,
                Status = "Booked"
            };

            var messageString = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageString);

            // 4. โยนข้อความลงคิว!
            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "email_queue", body: body);
        }

        public async Task PublishPaymentConfirmedEvent(Guid userId, string seatNumber, string concertName, Guid ticketId)
        {
            var correlationId = GetCorrelationId(); // 🔥 ดึงรหัสติดตาม
            
            var factory = new ConnectionFactory { HostName = "rabbitmq_broker" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // ประกาศคิวใหม่สำหรับ E-Ticket
            await channel.QueueDeclareAsync(queue: "payment_confirmed_queue", durable: true, exclusive: false, autoDelete: false);

            var message = new
            {
                CorrelationId = correlationId, // 🔥 ส่งไปด้วย
                UserId = userId,
                SeatNumber = seatNumber,
                ConcertName = concertName,
                TicketId = ticketId,
                Status = "Paid"
            };

            var body = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "payment_confirmed_queue", body: body);
        }
    }
}