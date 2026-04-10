"use client";

import Link from "next/link";
import Cookies from "js-cookie";
import { deleteConcert, getConcerts } from "@/services/api";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { Card, CardContent, CardFooter, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  CalendarDays,
  MapPin,
  Ticket,
  RefreshCcw,
  LogOut,
  User,
  Trash2,
  Edit3,
} from "lucide-react";
import { useEffect, useState } from "react";
import { jwtDecode } from "jwt-decode";
import { toast } from "sonner";
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
import { ConcertCardSkeleton } from "@/components/ConcertCardSkeleton";
import { Skeleton } from "@/components/ui/skeleton";

export default function HomePage() {
  const router = useRouter();
  const [concerts, setConcerts] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [isAdmin, setIsAdmin] = useState(false);

  useEffect(() => {
    const token = Cookies.get("token");
    setIsLoggedIn(!!token);

    if (token) {
      try {
        const decoded: any = jwtDecode(token);
        console.log("Decoded Token:", decoded); // 🔥 ดูใน Console ของ Browser ว่ามีค่า role ไหม
        // 🔥 เช็ค Role แบบ Case-insensitive และรองรับทั้ง 2 Key
        const rawRole =
          decoded.role ||
          decoded[
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
          ];
        if (rawRole === "Admin") {
          setIsAdmin(true);
        }
      } catch (e) {
        console.error("Token error");
      }
    }
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const response = await getConcerts();
      setConcerts(response.data || []);
      setError(false);
    } catch (err) {
      setError(true);
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = () => {
    Cookies.remove("token");
    setIsLoggedIn(false);
    setIsAdmin(false);
    window.location.reload();
  };

  const handleBooking = (concertId: string) => {
    if (isLoggedIn) {
      router.push(`/booking/${concertId}`);
    } else {
      router.push("/login");
    }
  };

  const handleDelete = async (id: string) => {
    const token = Cookies.get("token");
    if (!token) return;

    // ใช้การกดยืนยันแบบมาตรฐานก่อน
    if (
      window.confirm(
        "คุณต้องการลบคอนเสิร์ตนี้ใช่หรือไม่? ข้อมูลตั๋วทั้งหมดจะถูกลบถาวร",
      )
    ) {
      try {
        await deleteConcert(id, token);
        toast.success("ลบคอนเสิร์ตเรียบร้อยแล้ว");
        // เรียกโหลดข้อมูลใหม่
        const response = await getConcerts();
        setConcerts(response.data || []);
      } catch (err: any) {
        toast.error("ไม่สามารถลบได้: " + err.message);
      }
    }
  };

  if (loading)
    return (
      <div className="min-h-screen bg-black text-white">
        {/* Navbar จำลองคงไว้เพื่อให้ดูเหมือนเว็บโหลดแค่เนื้อหา */}
        <nav className="border-b border-white/5 py-5 px-8 flex justify-between items-center bg-black/60 backdrop-blur-xl">
          <h2 className="text-2xl font-black text-zinc-800">TICKET-HUB</h2>
        </nav>

        <main className="p-8 max-w-7xl mx-auto">
          <header className="mb-16">
            <Skeleton className="h-16 w-1/2 bg-zinc-900 mb-4" />
            <Skeleton className="h-6 w-1/3 bg-zinc-900" />
          </header>

          {/* 🔥 แสดง Skeleton 6 ใบระหว่างรอข้อมูล */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-10">
            {[...Array(6)].map((_, i) => (
              <ConcertCardSkeleton key={i} />
            ))}
          </div>
        </main>
      </div>
    );

  if (error)
    return (
      <div className="min-h-screen bg-black flex items-center justify-center p-8 text-center">
        <div className="max-w-md p-8 rounded-3xl border border-red-500/20 bg-red-500/5">
          <h2 className="text-2xl font-bold text-red-500 mb-4">
            ⚠️ CONNECTION ERROR
          </h2>
          <p className="text-zinc-400 mb-6">
            ไม่สามารถเชื่อมต่อระบบหลังบ้านได้ กรุณาตรวจสอบการรัน Docker
          </p>
          <Button
            onClick={loadData}
            className="w-full bg-red-600 hover:bg-red-500 cursor-pointer"
          >
            <RefreshCcw className="mr-2 h-4 w-4" /> RETRY
          </Button>
        </div>
      </div>
    );

  return (
    <div className="min-h-screen bg-black text-white selection:bg-blue-500/30">
      {/* Navbar */}
      <nav className="border-b border-white/5 py-5 px-8 flex justify-between items-center bg-black/60 backdrop-blur-xl sticky top-0 z-50">
        <Link href="/" className="group">
          <h2 className="text-2xl font-black bg-linear-to-r from-blue-400 to-indigo-500 bg-clip-text text-transparent group-hover:from-blue-300 group-hover:to-indigo-400 transition-all">
            TICKET-HUB
          </h2>
        </Link>

        <div className="flex items-center gap-6">
          {isLoggedIn ? (
            <div className="flex items-center gap-4">
              {isAdmin && (
                <Link
                  href="/admin"
                  className="text-sm font-bold text-zinc-400 hover:text-blue-400 transition-colors cursor-pointer mr-2"
                >
                  Dashboard
                </Link>
              )}
              <Link
                href="/my-tickets"
                className="text-sm font-bold text-zinc-400 hover:text-blue-400 transition-colors cursor-pointer mr-2"
              >
                ตั๋วของฉัน
              </Link>
              <div className="hidden md:flex items-center gap-2 text-zinc-400 bg-zinc-900/50 px-4 py-2 rounded-full border border-white/5">
                <User className="w-4 h-4" />
                <span className="text-sm font-medium">Tony (Member)</span>
              </div>
              <Button
                onClick={handleLogout}
                variant="ghost"
                className="cursor-pointer text-zinc-500 hover:text-red-500 transition-colors"
              >
                <LogOut className="w-4 h-4" />
              </Button>
            </div>
          ) : (
            <Link href="/login">
              <Button className="bg-white text-black hover:bg-blue-500 hover:text-white cursor-pointer rounded-full px-8 font-bold transition-all">
                Sign In
              </Button>
            </Link>
          )}
        </div>
      </nav>

      <main className="p-8 max-w-7xl mx-auto">
        <header className="mb-16 text-center md:text-left">
          <h1 className="text-6xl md:text-8xl font-black mb-4 italic tracking-tighter uppercase leading-none">
            Upcoming <br /> <span className="text-blue-500">Live</span> Events
          </h1>
          <p className="text-zinc-500 text-xl">
            ค้นหาคอนเสิร์ตที่คุณต้องการและสำรองที่นั่งได้ทันที
            พร้อมระบบล็อกที่นั่งแบบ Real-time
          </p>
        </header>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-10">
          {concerts.map((concert) => (
            <Card
              key={concert.id}
              className="bg-zinc-900 border-zinc-800 text-white overflow-hidden group flex flex-col h-full border-none shadow-[0_20px_50px_rgba(0,0,0,0.5)] hover:shadow-blue-900/10 transition-all duration-500"
            >
              <div className="relative aspect-[3/4] w-full overflow-hidden shrink-0 pt-0">
                <Image
                  src={concert.posterImageUrl}
                  alt={concert.name}
                  fill
                  className="object-cover transition-transform duration-700 group-hover:scale-110"
                  priority
                  unoptimized
                />
                <div className="absolute inset-0 bg-linear-to-t from-zinc-900 via-zinc-900/20 to-transparent opacity-80" />
              </div>

              <CardContent className="p-8 grow flex flex-col">
                <CardTitle className="text-3xl font-black mb-4 leading-tight tracking-tight group-hover:text-blue-400 transition-colors">
                  {concert.name}
                </CardTitle>
                <div className="space-y-4 text-zinc-400 mt-auto">
                  <div className="flex items-center gap-3">
                    <CalendarDays className="w-5 h-5 text-blue-400" />
                    <span className="text-sm font-semibold">
                      {new Date(concert.date).toLocaleDateString("th-TH", {
                        year: "numeric",
                        month: "long",
                        day: "numeric",
                      })}
                    </span>
                  </div>
                  <div className="flex items-center gap-3">
                    <MapPin className="w-5 h-5 text-purple-400" />
                    <span className="text-sm font-semibold uppercase tracking-wider">
                      Impact Arena / Challenger Hall
                    </span>
                  </div>
                </div>
              </CardContent>

              <CardFooter className="p-8 pt-0 mt-auto bg-transparent border-none flex flex-col gap-4">
                {/* 1. ปุ่มจองที่นั่ง (ทุกคนเห็น) */}
                <Button
                  onClick={() => handleBooking(concert.id)}
                  className="w-full bg-blue-600 text-white hover:bg-blue-400 cursor-pointer font-black py-8 text-xl rounded-2xl shadow-xl shadow-blue-900/20 active:scale-95 transition-all"
                >
                  <Ticket className="mr-3 w-6 h-6" /> จองที่นั่งเลย
                </Button>

                {/* 2. ปุ่มลบคอนเสิร์ต (เฉพาะ Admin เห็น) - ปรับสไตล์ให้เหมือนปุ่มจองแต่เป็นสีแดง */}
                {isAdmin && (
                  <>
                    {/* 2. 🔥 ปุ่มแก้ไขคอนเสิร์ต (เฉพาะ Admin) - สีเหลือง Amber */}
                    <Button
                      onClick={() => router.push(`/admin/edit/${concert.id}`)}
                      className="w-full bg-amber-600/10 text-amber-500 hover:bg-amber-600 hover:text-white border border-amber-500/20 cursor-pointer font-black py-8 text-xl rounded-2xl active:scale-95 transition-all"
                    >
                      <Edit3 className="mr-3 w-6 h-6" /> แก้ไขคอนเสิร์ต
                    </Button>

                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button
                          variant="destructive"
                          className="w-full bg-red-600/10 text-red-500 hover:bg-red-600 hover:text-white border border-red-500/20 cursor-pointer font-black py-8 text-xl rounded-2xl active:scale-95 transition-all "
                        >
                          <Trash2 className="mr-3 w-6 h-6" /> ลบคอนเสิร์ตนี้
                        </Button>
                      </AlertDialogTrigger>

                      <AlertDialogContent className="bg-zinc-900 border-zinc-800 text-white rounded-[2.5rem] p-0 overflow-hidden max-w-[400px] shadow-2xl">
                        <div className="p-10 text-center">
                          <AlertDialogHeader>
                            <AlertDialogTitle className="text-3xl font-black uppercase italic tracking-tighter text-center">
                              ยืนยันการลบ?
                            </AlertDialogTitle>
                            <AlertDialogDescription className="text-zinc-400 pt-4 text-center leading-relaxed">
                              การลบ "{concert.name}"
                              จะทำให้ข้อมูลที่นั่งและประวัติการจองทั้งหมดหายไปถาวร
                            </AlertDialogDescription>
                          </AlertDialogHeader>
                        </div>

                        <AlertDialogFooter className="bg-zinc-800/50 p-8 border-t border-zinc-800 gap-3">
                          <AlertDialogCancel className="flex-1 bg-zinc-800 border-zinc-700 text-zinc-300 hover:bg-zinc-700 hover:text-white rounded-2xl py-7 font-bold cursor-pointer transition-all">
                            ยกเลิก
                          </AlertDialogCancel>
                          <AlertDialogAction
                            onClick={() => handleDelete(concert.id)}
                            className="flex-1 bg-red-600 hover:bg-red-500 text-white font-bold rounded-2xl py-7 shadow-lg shadow-red-900/30 cursor-pointer transition-all active:scale-95"
                          >
                            ยืนยันลบทิ้ง
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </>
                )}
              </CardFooter>
            </Card>
          ))}
        </div>
      </main>
    </div>
  );
}
