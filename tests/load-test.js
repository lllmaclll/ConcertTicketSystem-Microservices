import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 50,        // ลดเหลือ 50 คนเพื่อความเสถียรบนเครื่องเครื่องเดียว
    duration: '30s', 
};

const BASE_URL = 'http://localhost:5177';

// 🔥 ส่วน setup: Login แค่ครั้งเดียวเพื่อเอา Token ของ Tony มาใช้งานร่วมกัน
// วิธีนี้จะตัดภาระ CPU เรื่อง BCrypt ออกไป 99%
export function setup() {
    const loginPayload = JSON.stringify({ username: 'tony', password: '123' });
    const res = http.post(`${BASE_URL}/api/Auth/login`, loginPayload, {
        headers: { 'Content-Type': 'application/json' },
    });
    return { token: res.json().data.token };
}

export default function (data) {
    const bookParams = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${data.token}`,
        },
    };

    // สุ่มที่นั่ง A1 ถึง J10
    const rows = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];
    const seatNumber = `${rows[Math.floor(Math.random() * rows.length)]}${Math.floor(Math.random() * 10) + 1}`;

    const res = http.post(`${BASE_URL}/api/Tickets/book`, JSON.stringify({
        concertId: '22222222-2222-2222-2222-222222222222',
        seatNumber: seatNumber,
    }), bookParams);

    check(res, {
        'status is 200 or 400': (r) => r.status === 200 || r.status === 400,
    });

    sleep(0.1); // พักนิดหน่อยเพื่อให้ระบบหายใจทัน
}