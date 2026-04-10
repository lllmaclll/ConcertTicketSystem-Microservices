"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { createConcert } from "@/services/api";
import Cookies from "js-cookie";
import { PlusCircle, Image as ImageIcon, Loader2, ChevronLeft } from "lucide-react";
import Link from "next/link";

export default function AdminPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [preview, setPreview] = useState<string | null>(null); // 🔥 สำหรับโชว์รูป

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setLoading(true);

    const token = Cookies.get("token");
    const formData = new FormData(e.currentTarget); // ดึงค่าจากฟอร์มอัตโนมัติ

    try {
      await createConcert(formData, token!);
      toast.success("สร้างคอนเสิร์ตสำเร็จ!", {
        description: "ระบบสร้างโซนและที่นั่งให้เรียบร้อยแล้ว",
      });
      router.push("/"); // กลับหน้าแรกไปดูผลงาน
    } catch (err: any) {
      toast.error(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setPreview(URL.createObjectURL(file)); // สร้าง URL จำลองเพื่อโชว์รูป
    }
  };

  return (
    <div className="min-h-screen bg-black text-white p-8">
      <div className="max-w-2xl mx-auto">
        {/* 🔥 เพิ่มปุ่มย้อนกลับ */}
        <Link href="/" className="inline-flex items-center text-zinc-500 hover:text-white mb-8 transition-colors group cursor-pointer">
          <ChevronLeft className="mr-1 group-hover:-translate-x-1 transition-transform" /> Back to Events
        </Link>


        <h1 className="text-4xl font-black mb-8 uppercase italic text-blue-500">Admin Dashboard</h1>

        <form
          onSubmit={handleSubmit}
          className="space-y-6 bg-zinc-900 p-8 rounded-[2rem] border border-zinc-800 shadow-2xl"
        >
          <div className="grid gap-4">
            <div className="space-y-2">
              <Label>ชื่อคอนเสิร์ต</Label>
              <Input
                name="name"
                placeholder="เช่น LISA First Solo Concert"
                className="bg-zinc-800 border-zinc-700"
                required
              />
            </div>

            <div className="space-y-2">
              <Label>วันที่และเวลาจัดงาน</Label>
              <Input
                name="date"
                type="datetime-local"
                className="bg-zinc-800 border-zinc-700"
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>ราคา VIP (บาท)</Label>
                <Input
                  name="vipPrice"
                  type="number"
                  defaultValue="5000"
                  className="bg-zinc-800 border-zinc-700"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label>ราคา GA (บาท)</Label>
                <Input
                  name="gaPrice"
                  type="number"
                  defaultValue="2000"
                  className="bg-zinc-800 border-zinc-700"
                  required
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>จำนวนที่นั่ง VIP</Label>
                <Input
                  name="vipCapacity"
                  type="number"
                  defaultValue="40"
                  className="bg-zinc-800 border-zinc-700"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label>จำนวนที่นั่ง GA</Label>
                <Input
                  name="gaCapacity"
                  type="number"
                  defaultValue="60"
                  className="bg-zinc-800 border-zinc-700"
                  required
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label>รูปภาพโปสเตอร์</Label>
              <div className="flex items-center justify-center w-full">
                <label className="flex flex-col items-center justify-center w-full h-48 border-2 border-dashed border-zinc-700 rounded-2xl cursor-pointer hover:bg-zinc-800 overflow-hidden relative">
                  {preview ? (
                    <img src={preview} className="w-full h-full object-cover" />
                  ) : (
                    <div className="flex flex-col items-center">
                      <ImageIcon className="w-8 h-8 text-zinc-500 mb-2" />
                      <p className="text-sm text-zinc-500">
                        คลิกเพื่อเลือกไฟล์รูปภาพ
                      </p>
                    </div>
                  )}
                  <input
                    name="posterFile"
                    type="file"
                    className="hidden"
                    accept="image/*"
                    onChange={handleImageChange}
                    required
                  />
                </label>
              </div>
            </div>
          </div>

          <Button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-500 py-8 text-xl font-bold rounded-2xl cursor-pointer"
          >
            {loading ? (
              <Loader2 className="animate-spin mr-2" />
            ) : (
              <PlusCircle className="mr-2" />
            )}
            CREATE CONCERT
          </Button>
        </form>
      </div>
    </div>
  );
}
