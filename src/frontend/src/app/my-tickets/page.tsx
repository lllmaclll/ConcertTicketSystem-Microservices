"use client"
import { useEffect, useState } from "react";
import { getMyTickets } from "@/services/api";
import Cookies from "js-cookie";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Ticket, QrCode, Calendar, MapPin, ChevronRight } from "lucide-react";
import Link from "next/link";

export default function MyTicketsPage() {
  const [tickets, setTickets] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = Cookies.get("token");
    if (token) {
      getMyTickets(token).then(res => {
        setTickets(res.data || []);
        setLoading(false);
      });
    }
  }, []);

  if (loading) return (
    <div className="min-h-screen bg-black flex items-center justify-center">
      <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-blue-500"></div>
    </div>
  );

  return (
    <div className="min-h-screen bg-zinc-950 text-white p-6 md:p-12">
      <div className="max-w-4xl mx-auto">
        <header className="flex justify-between items-end mb-12">
          <div>
            <h1 className="text-5xl font-black italic uppercase tracking-tighter text-transparent bg-clip-text bg-linear-to-r from-blue-400 to-indigo-600">
              My Tickets
            </h1>
            <p className="text-zinc-500 mt-2">ประวัติการสั่งซื้อและบัตรเข้าชมของคุณ</p>
          </div>
          <Link href="/" className="text-zinc-400 hover:text-white text-sm font-bold flex items-center transition-colors">
            Back to Home <ChevronRight className="w-4 h-4" />
          </Link>
        </header>
        
        <div className="grid gap-8">
          {tickets.length === 0 ? (
            <div className="text-center py-24 border-2 border-dashed border-zinc-900 rounded-[3rem] bg-zinc-900/20">
              <Ticket className="w-16 h-16 text-zinc-800 mx-auto mb-4" />
              <p className="text-zinc-500 font-bold">คุณยังไม่มีประวัติการจองตั๋วในขณะนี้</p>
            </div>
          ) : (
            tickets.map((t) => (
              <div key={t.bookingId} className="relative group">
                {/* แถบสีสถานะข้างบัตร */}
                <div className={`absolute left-0 top-0 bottom-0 w-2 rounded-l-2xl ${t.status === "Booked" ? "bg-green-500" : "bg-yellow-500 shadow-[0_0_15px_rgba(234,179,8,0.3)]"}`} />
                
                <Card className="bg-zinc-900 border-zinc-800 overflow-hidden rounded-2xl transition-all hover:border-zinc-600">
                  <CardContent className="p-0 flex flex-col md:flex-row items-stretch">
                    
                    {/* ข้อมูลตั๋ว */}
                    <div className="p-8 flex-grow space-y-6">
                      <div className="space-y-1">
                        <div className="flex items-center gap-3 mb-2">
                           <Badge variant={t.status === "Booked" ? "default" : "outline"} className={t.status === "Booked" ? "bg-green-600 text-white" : "text-yellow-500 border-yellow-500"}>
                             {t.status === "Booked" ? "CONFIRMED" : "WAITING FOR PAYMENT"}
                           </Badge>
                        </div>
                        <h2 className="text-3xl font-black text-white tracking-tight">{t.concertName}</h2>
                      </div>

                      <div className="grid grid-cols-2 gap-4">
                        <div className="flex items-center gap-3 text-zinc-400">
                          <div className="p-2 bg-zinc-800 rounded-lg"><Ticket className="w-4 h-4 text-blue-400" /></div>
                          <div>
                            <p className="text-[10px] uppercase font-bold text-zinc-600">Seat Number</p>
                            <p className="font-bold text-white">{t.seatNumber}</p>
                          </div>
                        </div>
                        <div className="flex items-center gap-3 text-zinc-400">
                          <div className="p-2 bg-zinc-800 rounded-lg"><Calendar className="w-4 h-4 text-purple-400" /></div>
                          <div>
                            <p className="text-[10px] uppercase font-bold text-zinc-600">Booking Date</p>
                            <p className="font-bold text-white">{new Date(t.reservedAt).toLocaleDateString()}</p>
                          </div>
                        </div>
                      </div>
                    </div>
                    
                    {/* QR Code Section */}
                    <div className="bg-white px-10 flex flex-col items-center justify-center border-l-4 border-dashed border-zinc-900 min-w-[200px]">
                      <QrCode className="w-24 h-24 text-black mb-3" />
                      <div className="text-center">
                        <p className="text-[9px] font-black text-black leading-none uppercase tracking-widest">Digital Ticket</p>
                        <p className="text-[8px] font-mono text-zinc-500 mt-1">{t.bookingId.substring(0,13)}</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}