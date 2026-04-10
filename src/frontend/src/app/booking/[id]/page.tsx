"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Cookies from "js-cookie";
import { getConcertDetails, bookTicket } from "@/services/api";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Loader2,
  Armchair,
  CheckCircle2,
  ChevronLeft,
  Ticket as TicketIcon,
} from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { toast } from "sonner";
import { Skeleton } from "@/components/ui/skeleton";

export default function BookingPage() {
  const { id } = useParams();
  const router = useRouter();
  const [data, setData] = useState<any>(null);
  const [activeZone, setActiveZone] = useState<string | null>(null); // 🔥 เก็บโซนที่กำลังเลือก
  const [selectedSeat, setSelectedSeat] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    const token = Cookies.get("token");
    if (!token) {
      toast.error("กรุณาเข้าสู่ระบบก่อนเลือกที่นั่ง");
      router.push("/login");
      return;
    }
    loadDetails();
  }, []);

  const loadDetails = async () => {
    try {
      const res = await getConcertDetails(id as string);
      setData(res.data);
      // ตั้งค่าโซนแรกเป็นค่าเริ่มต้น
      if (res.data.zones.length > 0) {
        setActiveZone(res.data.zones[0].id);
      }
    } catch (err) {
      toast.error("โหลดข้อมูลที่นั่งล้มเหลว");
    } finally {
      setLoading(false);
    }
  };

  const handleBook = async () => {
    if (!selectedSeat) return;
    const token = Cookies.get("token");
    setSubmitting(true);

    try {
      const result = await bookTicket(
        id as string,
        selectedSeat.seatNumber,
        token!,
      );

      // ✅ เช็คก่อนว่ามีข้อมูล bookingId กลับมาจริงไหม
      if (result.success && result.data?.bookingId) {
        toast.success("ล็อกที่นั่งสำเร็จ!");
        router.push(`/checkout/${result.data.bookingId}`);
      } else {
        throw new Error("ระบบไม่ได้ส่งรหัสการจองกลับมา");
      }
    } catch (err: any) {
      toast.error("จองไม่สำเร็จ", { description: err.message });
      loadDetails();
    } finally {
      setSubmitting(false);
    }
  };

  if (loading)
    return (
      <div className="min-h-screen bg-black p-8">
        <div className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-12 gap-10">
          {/* ฝั่งซ้ายจำลอง */}
          <div className="lg:col-span-3 space-y-6">
            <Skeleton className="aspect-[3/4] rounded-[2rem] bg-zinc-900" />
            <Skeleton className="h-10 w-full bg-zinc-900" />
          </div>
          {/* ฝั่งขวาจำลอง (ผังที่นั่ง) */}
          <div className="lg:col-span-6">
            <Skeleton className="h-[500px] w-full rounded-[3rem] bg-zinc-900" />
          </div>
          {/* ฝั่งขวาสรุปจำลอง */}
          <div className="lg:col-span-3">
            <Skeleton className="h-64 w-full rounded-3xl bg-zinc-900" />
          </div>
        </div>
      </div>
    );

  return (
    <div className="min-h-screen bg-black text-white p-4 md:p-8">
      <div className="max-w-7xl mx-auto">
        <Link
          href="/"
          className="inline-flex items-center text-zinc-500 hover:text-white mb-8 transition-colors group"
        >
          <ChevronLeft className="w-5 h-5 mr-1 group-hover:-translate-x-1 transition-transform" />
          Back to Events
        </Link>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
          {/* ส่วนข้อมูลคอนเสิร์ต */}
          <div className="lg:col-span-3 space-y-6">
            <div className="relative aspect-[3/4] rounded-3xl overflow-hidden border border-zinc-800 shadow-2xl">
              <Image
                src={data.concert.posterImageUrl}
                alt=""
                fill
                className="object-cover"
                unoptimized
              />
            </div>
            <div className="space-y-4">
              <h1 className="text-3xl font-black uppercase leading-none tracking-tighter">
                {data.concert.name}
              </h1>
              <div className="flex flex-col gap-2">
                <Badge className="bg-blue-600 text-white justify-center py-2">
                  LIVE SESSION
                </Badge>
                <Badge
                  variant="outline"
                  className="text-zinc-500 border-zinc-800 justify-center py-2"
                >
                  15 MINS TIMEOUT
                </Badge>
              </div>
            </div>
          </div>

          {/* ส่วนผังที่นั่ง (Grid มาตรฐาน) */}
          <div className="lg:col-span-6 space-y-8">
            <div className="bg-zinc-900/50 rounded-[2.5rem] p-8 border border-zinc-800">
              {/* ตัวเลือกโซน */}
              <div className="flex gap-2 mb-10 overflow-x-auto pb-2 scrollbar-hide">
                {data.zones.map((zone: any) => (
                  <Button
                    key={zone.id}
                    variant={activeZone === zone.id ? "default" : "secondary"}
                    onClick={() => {
                      setActiveZone(zone.id);
                      setSelectedSeat(null);
                    }}
                    className={`rounded-full px-6 cursor-pointer transition-all border-none ${
                      activeZone === zone.id
                        ? "bg-blue-600 text-white hover:bg-blue-700"
                        : "bg-zinc-800 text-zinc-400 hover:bg-zinc-700 hover:text-zinc-200" // 🔥 สีเข้มขึ้น
                    }`}
                  >
                    {zone.name} (฿{zone.price.toLocaleString()})
                  </Button>
                ))}
              </div>

              {/* เวทีแสดง */}
              <div className="mb-12">
                <div className="w-full h-3 bg-zinc-800 rounded-full mb-2 shadow-[0_0_20px_rgba(59,130,246,0.3)]"></div>
                <p className="text-center text-[10px] text-zinc-600 font-bold uppercase tracking-[0.4em]">
                  Main Stage
                </p>
              </div>

              {/* ตารางเก้าอี้ */}
              <div className="grid grid-cols-5 sm:grid-cols-10 gap-3">
                {data.seats
                  .filter((s: any) => s.zoneId === activeZone)
                  .map((seat: any) => {
                    const isPending = seat.status === 1;
                    const isPaid = seat.status === 2;
                    const isAvailable = seat.status === 0;
                    const isSelected = selectedSeat?.id === seat.id;

                    let bgColor =
                      "bg-zinc-800 hover:bg-zinc-700 hover:scale-110 cursor-pointer";
                    if (isPending)
                      bgColor = "bg-amber-500/50 cursor-not-allowed";
                    if (isPaid) bgColor = "bg-rose-600/40 cursor-not-allowed";
                    if (isSelected)
                      bgColor =
                        "bg-blue-600 ring-4 ring-blue-400/50 scale-110 z-10";

                    return (
                      <button
                        key={seat.id}
                        disabled={!isAvailable}
                        onClick={() => setSelectedSeat(seat)}
                        className={`aspect-square rounded-lg flex flex-col items-center justify-center transition-all ${bgColor}`}
                      >
                        <Armchair
                          className={`w-5 h-5 ${isSelected ? "text-white" : "text-zinc-500"}`}
                        />
                        <span className="text-[7px] font-bold mt-0.5">
                          {seat.seatNumber}
                        </span>
                      </button>
                    );
                  })}
              </div>

              {/* คำอธิบายสี */}
              <div className="mt-12 flex justify-center gap-6 text-[10px] font-bold uppercase text-zinc-500 tracking-wider">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 bg-zinc-800 rounded-sm"></div>{" "}
                  Available
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 bg-amber-500/50 rounded-sm"></div>{" "}
                  Pending
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 bg-rose-600/40 rounded-sm"></div> Sold
                  Out
                </div>
              </div>
            </div>
          </div>

          {/* สรุปข้อมูลด้านขวา */}
          <div className="lg:col-span-3">
            <div className="bg-zinc-900 border border-zinc-800 rounded-3xl p-6 sticky top-10 space-y-6">
              <div className="flex items-center gap-2 mb-4 text-blue-400">
                <TicketIcon className="w-5 h-5" />
                <h3 className="font-bold">Selection Summary</h3>
              </div>

              <div className="space-y-4">
                <div className="flex justify-between border-b border-zinc-800 pb-4">
                  <span className="text-zinc-500 text-sm">Selected Seat</span>
                  <span className="text-2xl font-black text-white">
                    {selectedSeat?.seatNumber || "---"}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-zinc-500 text-sm">Price</span>
                  <span className="text-xl font-bold">
                    ฿{selectedSeat?.price.toLocaleString() || "0"}
                  </span>
                </div>
              </div>

              <Button
                disabled={!selectedSeat || submitting}
                onClick={handleBook}
                className="w-full bg-white text-black hover:bg-blue-500 hover:text-white py-8 text-lg font-black rounded-2xl transition-all shadow-xl active:scale-95 cursor-pointer"
              >
                {submitting ? <Loader2 className="animate-spin" /> : "BOOK NOW"}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
