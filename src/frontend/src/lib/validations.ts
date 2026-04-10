import * as z from "zod";

// กฎสำหรับฟอร์มสร้าง/แก้ไขคอนเสิร์ต
export const concertSchema = z.object({
  name: z.string().min(5, "ชื่อคอนเสิร์ตต้องมีอย่างน้อย 5 ตัวอักษร"),
  date: z.string().min(1, "กรุณาระบุวันที่จัดงาน"),
  vipPrice: z.coerce.number().min(100, "ราคา VIP ต้องไม่ต่ำกว่า 100 บาท"),
  gaPrice: z.coerce.number().min(100, "ราคา GA ต้องไม่ต่ำกว่า 100 บาท"),
  vipCapacity: z.coerce.number().min(1, "ต้องมีที่นั่ง VIP อย่างน้อย 1 ที่"),
  gaCapacity: z.coerce.number().min(1, "ต้องมีที่นั่ง GA อย่างน้อย 1 ที่"),
  // 🔥 เพิ่มบรรทัดนี้ เพื่อบอกว่ารองรับไฟล์รูปภาพ (เป็นไฟล์อะไรก็ได้)
  posterFile: z.any().optional(), 
});

// กฎสำหรับ Login
export const loginSchema = z.object({
  username: z.string().min(3, "Username ต้องมีอย่างน้อย 3 ตัวอักษร"),
  password: z.string().min(3, "Password ต้องมีอย่างน้อย 3 ตัวอักษร"),
});