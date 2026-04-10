"use client";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import {
  confirmPayment,
  getBookingDetails,
  cancelBooking,
} from "@/services/api";
import Cookies from "js-cookie";
import { Timer, ArrowLeft, Loader2, Ticket as TicketIcon } from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

export default function CheckoutPage() {
  const { id } = useParams();
  const router = useRouter();
  const [booking, setBooking] = useState<any>(null);
  const [timeLeft, setTimeLeft] = useState(900);
  const [isProcessing, setIsProcessing] = useState(false);

  useEffect(() => {
    const token = Cookies.get("token");
    if (!token) {
      router.push("/login");
      return;
    }

    getBookingDetails(id as string, token)
      .then((res) => setBooking(res.data))
      .catch(() => router.push("/"));

    const timer = setInterval(() => setTimeLeft((prev) => prev - 1), 1000);
    return () => clearInterval(timer);
  }, [id]);

  const handlePayment = async () => {
    const token = Cookies.get("token");
    setIsProcessing(true);
    try {
      await confirmPayment(id as string, token!);
      toast.success("ชำระเงินสำเร็จ!");
      router.push("/my-tickets");
    } catch (err: any) {
      toast.error("ล้มเหลว: " + err.message);
    } finally {
      setIsProcessing(false);
    }
  };

  const onCancel = async () => {
    const token = Cookies.get("token");
    try {
      await cancelBooking(id as string, token!);
      toast.info("ยกเลิกการจองสำเร็จ");
      router.push("/");
    } catch (err) {
      toast.error("ยกเลิกไม่สำเร็จ");
    }
  };

  const formatTime = (s: number) =>
    `${Math.floor(s / 60)}:${(s % 60).toString().padStart(2, "0")}`;

  return (
    <div className="min-h-screen bg-black text-white flex flex-col items-center justify-center p-6">
      <div className="max-w-md w-full bg-zinc-900 border border-zinc-800 rounded-[2.5rem] p-8 shadow-2xl overflow-hidden relative">
        <div
          className="absolute top-0 left-0 h-1 bg-red-600 transition-all"
          style={{ width: `${(timeLeft / 900) * 100}%` }}
        ></div>

        <div className="text-center mb-8">
          <p className="text-zinc-500 text-[10px] font-bold tracking-[0.3em] uppercase mb-1">
            Time Remaining
          </p>
          <p className="text-5xl font-black tabular-nums">
            {formatTime(timeLeft)}
          </p>
        </div>

        <div className="bg-black/40 rounded-2xl p-6 border border-zinc-800 mb-8 space-y-4">
          <div className="flex gap-4">
            <div className="p-3 bg-blue-500/10 rounded-xl">
              <TicketIcon className="text-blue-500" />
            </div>
            <div className="text-left">
              <p className="text-[10px] text-zinc-500 uppercase font-bold">
                Event
              </p>
              <p className="font-bold text-sm">{booking?.concertName}</p>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4 text-left">
            <div>
              <p className="text-[10px] text-zinc-500 uppercase font-bold">
                Seat
              </p>
              <p className="text-2xl font-black text-white">
                {booking?.seatNumber || "..."}
              </p>
            </div>
            <div>
              <p className="text-[10px] text-zinc-500 uppercase font-bold">
                Price
              </p>
              <p className="text-2xl font-black text-green-500">
                ฿{booking?.price?.toLocaleString()}
              </p>
            </div>
          </div>
        </div>

        <Button
          onClick={handlePayment}
          disabled={isProcessing}
          className="w-full bg-white text-black hover:bg-blue-500 hover:text-white py-10 text-2xl font-black rounded-3xl transition-all shadow-xl cursor-pointer"
        >
          {isProcessing ? <Loader2 className="animate-spin" /> : "PAY NOW"}
        </Button>

        {/* 🔥 ใช้ AlertDialog แทน confirm() */}
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <button className="mt-6 text-zinc-500 hover:text-red-500 text-sm font-bold flex items-center gap-2 mx-auto cursor-pointer">
              <ArrowLeft className="w-4 h-4" /> ยกเลิกการจอง
            </button>
          </AlertDialogTrigger>
          <AlertDialogContent className="bg-zinc-900 border-zinc-800 text-white p-0 overflow-hidden max-w-[400px] rounded-[2.5rem] shadow-2xl">
            <div className="p-10 text-center">
              <AlertDialogHeader>
                <AlertDialogTitle className="text-3xl font-black uppercase italic text-center tracking-tighter">
                  ยืนยันการยกเลิก?
                </AlertDialogTitle>
                <AlertDialogDescription className="text-zinc-400 pt-4 text-center leading-relaxed">
                  ที่นั่งของคุณจะถูกคืนเข้าระบบเพื่อให้ผู้อื่นจองได้ทันที
                  หากเปลี่ยนใจคุณต้องเริ่มจองใหม่
                </AlertDialogDescription>
              </AlertDialogHeader>
            </div>

            {/* 🔥 Footer แบบคลีนๆ ไม่ต้องใส่ mx-0 mb-0 แล้วเพราะเราแก้ที่ไฟล์หลักแล้ว */}
            <AlertDialogFooter>
              {/* ใช้ flex-1 เพื่อให้ทั้งสองปุ่มยาวเท่ากันและเต็มพื้นที่ */}
              <AlertDialogCancel className="flex-1 bg-zinc-800 border-zinc-700 text-zinc-300 hover:bg-zinc-700 hover:text-white rounded-2xl py-6 font-bold cursor-pointer transition-all">
                กลับไปหน้าชำระเงิน
              </AlertDialogCancel>
              <AlertDialogAction
                onClick={onCancel}
                className="flex-1 bg-red-600 hover:bg-red-500 text-white font-bold rounded-2xl py-6 shadow-lg shadow-red-900/30 cursor-pointer transition-all active:scale-95"
              >
                ยืนยันยกเลิก
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </div>
  );
}
