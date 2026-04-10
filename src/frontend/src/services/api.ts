// --- แก้ไข api.ts ใหม่ให้เป็นแบบนี้ ---

const getBaseUrl = () => {
  if (typeof window !== "undefined") {
    return "http://localhost:5177"; // เรียกจาก Browser
  }
  return "http://api-gateway:8080"; // เรียกจาก Next.js Server
};

const request = async (path: string, options: RequestInit = {}) => {
  // 🔥 ย้ายมาเรียกข้างในนี้เพื่อให้ได้ค่าที่ถูกต้องตาม context ณ ตอนนั้น
  const baseUrl = getBaseUrl();

  const res = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      ...options.headers,
    },
    cache: "no-store", // ป้องกัน Cache ค้าง
  });

  if (!res.ok) {
    // พยายามอ่าน Error Message จาก ApiResponse
    const errorData = await res.json().catch(() => ({}));
    throw new Error(errorData.message || `Error: ${res.status}`);
  }

  return res.json();
};

export const getConcerts = () => request("/api/Tickets/concerts");

export const login = (credentials: any) =>
  request("/api/Auth/login", {
    method: "POST",
    body: JSON.stringify(credentials),
  });

export const getConcertDetails = (id: string) => request(`/api/Tickets/${id}`);

// 🔥 แก้ไขทุกฟังก์ชันให้ใช้ตัวแปร request ตัวเดียวกันทั้งหมดเพื่อความเสถียร
export const getBookingDetails = (id: string, token: string) =>
  request(`/api/Tickets/booking/${id}`, {
    headers: { Authorization: `Bearer ${token}` },
  });

export const bookTicket = (
  concertId: string,
  seatNumber: string,
  token: string,
) =>
  request("/api/Tickets/book", {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: JSON.stringify({ concertId, seatNumber }),
  });

export const confirmPayment = (id: string, token: string) =>
  request(`/api/tickets/confirm-payment/${id}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

export const cancelBooking = (id: string, token: string) =>
  request(`/api/tickets/cancel/${id}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

export const getMyTickets = (token: string) =>
  request("/api/Tickets/my-bookings", {
    headers: { Authorization: `Bearer ${token}` },
  });

// ฟังก์ชันสำหรับ Admin สร้างคอนเสิร์ต (ส่งเป็น FormData เพื่อรองรับไฟล์รูป)
export const createConcert = async (formData: FormData, token: string) => {
  const res = await fetch(`http://localhost:5177/api/Admin/concerts`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      // ห้ามใส่ Content-Type: application/json นะครับ เพราะเราส่งเป็น FormData
    },
    body: formData,
  });

  if (!res.ok) {
    const errorData = await res.json();
    throw new Error(errorData.message || "สร้างคอนเสิร์ตล้มเหลว");
  }
  return res.json();
};

// 1. เพิ่มฟังก์ชัน delete ใน api.ts
export const deleteConcert = async (id: string, token: string) => {
  const res = await fetch(`http://localhost:5177/api/Admin/concerts/${id}`, {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  return res.json();
};