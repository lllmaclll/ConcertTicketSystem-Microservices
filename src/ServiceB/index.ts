import './tracing'; // 🔥 ต้องอยู่บรรทัดแรกสุดของไฟล์!
import amqp, { Message } from "amqplib";
import * as grpc from "@grpc/grpc-js";
import * as protoLoader from "@grpc/proto-loader";
import path from "path";
import nodemailer from "nodemailer";

// ตั้งค่า Mail Transporter (อยู่นอกสุดได้)
const transporter = nodemailer.createTransport({
  host: "sandbox.smtp.mailtrap.io",
  port: 587, // 🔥 เปลี่ยนจาก 2525 เป็น 587
  auth: {
    user: "3dd8e819a8656a",
    pass: "a5ed1eb9ca5e1f",
  },
});

// 1. โหลดไฟล์ Proto สัญญาของเรา
const PROTO_PATH = path.join(__dirname, "Protos", "concert.proto");
const packageDefinition = protoLoader.loadSync(PROTO_PATH, {
  keepCase: true,
  longs: String,
  enums: String,
  defaults: true,
  oneofs: true,
});

// ใส่ : any เพื่อให้ TypeScript ไม่ต้องตรวจเช็คโครงสร้างที่โหลดมาแบบ Dynamic
const grpcObject: any = grpc.loadPackageDefinition(packageDefinition);
const concertProto = grpcObject.concert;

// 2. สร้าง gRPC Client ชี้ไปที่ C# (สมมติ C# รันอยู่ที่ Port 5019 ถ้าของคุณเป็นเลขอื่น ให้เปลี่ยนตรงนี้นะครับ!)
const grpcClient = new concertProto.ConcertInfo(
  "service-a:5019",
  grpc.credentials.createInsecure(),
);

async function connectToRabbitMQ() {
  while (true) {
    // วนลูปจนกว่าจะต่อติด
    try {
      const connection = await amqp.connect(
        "amqp://guest:guest@rabbitmq_broker:5672",
      );
      console.log("[*] Service B เชื่อมต่อ RabbitMQ สำเร็จ!");
      return connection;
    } catch (error) {
      console.log("[!] RabbitMQ ยังไม่พร้อม... กำลังรอ 5 วินาที");
      await new Promise((res) => setTimeout(res, 5000));
    }
  }
}

async function startWorker() {
  try {
    const connection = await connectToRabbitMQ();
    const channel = await connection.createChannel();

    // --- 📨 Consumer 1: สำหรับส่งเมลยืนยันการจอง (Reservation) ---
    const queue = "email_queue";
    await channel.assertQueue(queue, { durable: true });
    console.log(`[*] Service B พร้อมทำงาน! รอรับงานและพร้อมคุย gRPC...`);

    channel.consume(queue, (msg) => {
      if (msg !== null) {
        const data = JSON.parse(msg.content.toString());

        // 3. ทันทีที่ได้งานจาก RabbitMQ ให้โทร gRPC ไปถาม C# ทันที! (โทร gRPC ไปถามข้อมูลคอนเสิร์ต)
        grpcClient.GetConcertDetail(
          { concertId: "11111111-2222-3333-4444-555555555555" },
          async (err: any, response: any) => {
            if (err) {
              console.error("gRPC Error:", err);
              return;
            }

            // --- LOG แบบละเอียดที่คุณต้องการ ---
            console.log(`-------------------------------------------------`);
            console.log(`[x] 📥 ได้รับงานใหม่จาก RabbitMQ`);
            console.log(`    - 👤 UserId (จาก Token): ${data.UserId}`);
            console.log(`    - 🪑 ที่นั่ง: ${data.SeatNumber}`);
            console.log(`    - 🎫 Concert ID: ${data.ConcertId}`);
            console.log(`[+] 📞 ข้อมูลที่ดึงเพิ่มผ่าน gRPC:`);
            console.log(`    - 🎵 ชื่อคอนเสิร์ต: ${response.concertName}`);
            console.log(`    - 📅 วันที่แสดง: ${response.concertDate}`);

            // 🔥 ย้ายมาอยู่ในนี้ เพราะเราต้องใช้ 'response' จาก gRPC
            // 2. ตรวจสอบส่วนการส่งเมลใน channel.consume
            try {
              await transporter.sendMail({
                from: '"Concert Ticket System" <no-reply@ticket.com>',
                to: "customer@example.com", // ในโปรเจกต์จริงเราจะดึง email จาก DB
                subject: "ยืนยันการจองตั๋วสำเร็จ!",
                html: `
                  <div style="font-family: sans-serif; border: 1px solid #ddd; padding: 20px;">
                      <h1 style="color: #1a73e8;">จองตั๋วสำเร็จแล้ว!</h1>
                      <p>ยินดีด้วยคุณได้รับตั๋วสำหรับงานคอนเสิร์ตแล้ว</p>
                      <hr>
                      <p><b>คอนเสิร์ต:</b> ${response.concertName}</p>
                      <p><b>ที่นั่ง:</b> ${data.SeatNumber}</p>
                      <p><b>วันที่แสดง:</b> ${response.concertDate}</p>
                      <p><b>รหัสการจอง:</b> ${data.BookingId || "N/A"}</p>
                      <hr>
                      <p style="color: red;">*กรุณาชำระเงินภายใน 15 นาที เพื่อรักษาที่นั่งของคุณ</p>
                  </div>
              `,
              });
              console.log(`[v] 📧 ส่งอีเมลเข้า Mailtrap สำเร็จ!`);
            } catch (mailErr) {
              console.error("❌ Mail Error:", mailErr);
            }

            channel.ack(msg);
          },
        );
      }
    });

    // --- 🎫 Consumer 2: สำหรับส่งตั๋วจริง (E-Ticket) เมื่อจ่ายเงินแล้ว [เพิ่มตรงนี้! 🔥] ---
    const paidQueue = 'payment_confirmed_queue';
    await channel.assertQueue(paidQueue, { durable: true });

    channel.consume(paidQueue, async (msg) => {
        if (msg !== null) {
            const data = JSON.parse(msg.content.toString());
            console.log(`-------------------------------------------------`);
            console.log(`[VIP] 🎫 กำลังส่ง E-Ticket ให้ลูกค้า: ${data.UserId}`);

            try {
                await transporter.sendMail({
                    from: '"Concert Ticket" <tickets@ticket.com>',
                    to: "customer@example.com",
                    subject: "🎉 ตั๋วเข้างานของคุณมาแล้ว! (E-Ticket)",
                    html: `
                        <div style="background: #f4f4f4; padding: 20px; font-family: sans-serif;">
                            <div style="background: white; padding: 30px; border-radius: 10px; border-top: 10px solid #28a745;">
                                <h1 style="color: #28a745;">ชำระเงินสำเร็จ!</h1>
                                <h2>ชื่องาน: ${data.ConcertName}</h2>
                                <div style="background: #eee; padding: 20px; text-align: center; font-size: 24px; border: 2px dashed #ccc;">
                                    <b>ที่นั่ง: ${data.SeatNumber}</b><br>
                                    <small style="font-size: 14px;">Ticket ID: ${data.TicketId}</small>
                                </div>
                                <p>กรุณาแสดงอีเมลฉบับนี้ที่หน้างานเพื่อเข้าชม</p>
                            </div>
                        </div>
                    `
                });
                console.log(`[v] ✅ ส่ง E-Ticket เข้า Mailtrap สำเร็จ!`);
            } catch (mailErr) { console.error("Mail Error:", mailErr); }
            channel.ack(msg);
        }
    });
  } catch (error) {
    console.error(error);
  }
}

startWorker();
