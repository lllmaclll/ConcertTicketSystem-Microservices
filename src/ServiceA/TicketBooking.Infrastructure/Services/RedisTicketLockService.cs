using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using TicketBooking.Application.Interfaces;

namespace TicketBooking.Infrastructure.Services
{
    public class RedisTicketLockService : ITicketLockService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisTicketLockService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<bool> AcquireLockAsync(Guid concertId, string seatNumber, TimeSpan expiration)
        {
            var db = _redis.GetDatabase();
            string lockKey = $"ticket_lock:{concertId}:{seatNumber}";

            // พระเอกของเราอยู่ตรงนี้: StringSetAsync พร้อม When.NotExists
            // มันจะยอมสร้าง Key นี้และคืนค่า true ก็ต่อเมื่อ "ยังไม่มี Key นี้อยู่เท่านั้น" (ระดับ Millisecond)
            return await db.StringSetAsync(lockKey, "locked", expiration, When.NotExists);
        }
    }
}