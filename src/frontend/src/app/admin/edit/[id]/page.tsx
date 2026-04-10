"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { concertSchema } from "@/lib/validations"; // มั่นใจว่าสร้างไฟล์นี้แล้วใน Step 3 ครั้งก่อน
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { getConcertDetails, updateConcert } from "@/services/api";
import Cookies from "js-cookie";
import { Save, ChevronLeft, Loader2, Image as ImageIcon } from "lucide-react";
import Link from "next/link";

export default function EditConcertPage() {
  const { id } = useParams();
  const router = useRouter();
  const [preview, setPreview] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  // 1. ตั้งค่า React Hook Form ร่วมกับ Zod
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(
      concertSchema.omit({ vipCapacity: true, gaCapacity: true }),
    ),
    // หมายเหตุ: ตอนแก้ไขเราจะไม่แก้จำนวนที่นั่งเพื่อป้องกันข้อมูลเดิมพัง
  });

  // 2. ดึงข้อมูลเดิมมาเติมในฟอร์ม
  useEffect(() => {
    const token = Cookies.get("token");
    if (!token) return router.push("/login");

    getConcertDetails(id as string)
      .then((res) => {
        const concert = res.data.concert;
        reset({
          name: concert.name,
          date: new Date(concert.date).toISOString().slice(0, 16), // Format ให้เข้ากับ input datetime-local
          vipPrice:
            res.data.zones.find((z: any) => z.name === "VIP")?.price || 0,
          gaPrice: res.data.zones.find((z: any) => z.name === "GA")?.price || 0,
        });
        setPreview(concert.posterImageUrl);
        setLoading(false);
      })
      .catch(() => {
        toast.error("ดึงข้อมูลล้มเหลว");
        router.push("/");
      });
  }, [id, reset, router]);

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) setPreview(URL.createObjectURL(file));
  };

  const onSubmit = async (values: any) => {
    setSubmitting(true);
    const token = Cookies.get("token");
    const formData = new FormData();

    formData.append("Id", id as string);
    formData.append("Name", values.name);
    formData.append("Date", values.date);
    formData.append("VipPrice", values.vipPrice.toString());
    formData.append("GaPrice", values.gaPrice.toString());

    const fileInput = document.querySelector(
      'input[type="file"]',
    ) as HTMLInputElement;
    if (fileInput?.files?.[0]) {
      formData.append("PosterFile", fileInput.files[0]);
    }

    try {
      await updateConcert(id as string, formData, token!);
      toast.success("แก้ไขข้อมูลสำเร็จ");
      router.push("/");
    } catch (err: any) {
      toast.error(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  if (loading)
    return (
      <div className="min-h-screen bg-black flex items-center justify-center text-white">
        <Loader2 className="animate-spin" />
      </div>
    );

  return (
    <div className="min-h-screen bg-black text-white p-8">
      <div className="max-w-2xl mx-auto">
        <Link
          href="/"
          className="inline-flex items-center text-zinc-500 hover:text-white mb-8 transition-colors group"
        >
          <ChevronLeft className="mr-1 group-hover:-translate-x-1 transition-transform" />{" "}
          กลับหน้าแรก
        </Link>

        <h1 className="text-4xl font-black mb-8 uppercase italic text-blue-500">
          Edit Concert
        </h1>

        <form
          onSubmit={handleSubmit(onSubmit)}
          className="space-y-6 bg-zinc-900 p-8 rounded-[2rem] border border-zinc-800 shadow-2xl"
        >
          <div className="grid gap-4">
            <div className="space-y-2">
              <Label>ชื่อคอนเสิร์ต</Label>
              <Input
                {...register("name")}
                className="bg-zinc-800 border-zinc-700"
              />
              {errors.name && (
                <p className="text-red-500 text-xs">
                  {errors.name.message as string}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label>วันที่แสดง</Label>
              <Input
                {...register("date")}
                type="datetime-local"
                className="bg-zinc-800 border-zinc-700"
              />
              {errors.date && (
                <p className="text-red-500 text-xs">
                  {errors.date.message as string}
                </p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>ราคา VIP</Label>
                <Input
                  {...register("vipPrice")}
                  type="number"
                  className="bg-zinc-800 border-zinc-700"
                />
                {errors.vipPrice && (
                  <p className="text-red-500 text-xs">
                    {errors.vipPrice.message as string}
                  </p>
                )}
              </div>
              <div className="space-y-2">
                <Label>ราคา GA</Label>
                <Input
                  {...register("gaPrice")}
                  type="number"
                  className="bg-zinc-800 border-zinc-700"
                />
                {errors.gaPrice && (
                  <p className="text-red-500 text-xs">
                    {errors.gaPrice.message as string}
                  </p>
                )}
              </div>
            </div>

            <div className="space-y-2">
              <Label>รูปภาพโปสเตอร์ (เว้นไว้ถ้าไม่ต้องการเปลี่ยน)</Label>
              <div className="flex items-center justify-center w-full">
                <label className="flex flex-col items-center justify-center w-full h-64 border-2 border-dashed border-zinc-700 rounded-2xl cursor-pointer hover:bg-zinc-800 overflow-hidden relative">
                  {preview ? (
                    <img src={preview} className="w-full h-full object-cover" />
                  ) : (
                    <ImageIcon />
                  )}
                  <input
                    type="file"
                    className="hidden"
                    accept="image/*"
                    onChange={handleImageChange}
                  />
                </label>
              </div>
            </div>
          </div>

          <Button
            type="submit"
            disabled={submitting}
            className="w-full bg-blue-600 hover:bg-blue-500 py-8 text-xl font-bold rounded-2xl cursor-pointer"
          >
            {submitting ? (
              <Loader2 className="animate-spin mr-2" />
            ) : (
              <Save className="mr-2" />
            )}
            SAVE CHANGES
          </Button>
        </form>
      </div>
    </div>
  );
}
