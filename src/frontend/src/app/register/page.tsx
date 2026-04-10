"use client"
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { registerUser } from "@/services/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { toast } from "sonner";
import { UserPlus, Loader2 } from "lucide-react";
import Link from "next/link";

const registerSchema = z.object({
  username: z.string().min(3, "Username ต้องมีอย่างน้อย 3 ตัวอักษร"),
  password: z.string().min(6, "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร"),
  email: z.string().email("รูปแบบอีเมลไม่ถูกต้อง"),
});

export default function RegisterPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const { register, handleSubmit, formState: { errors } } = useForm({ resolver: zodResolver(registerSchema) });

  const onSubmit = async (values: any) => {
    setLoading(true);
    try {
      await registerUser(values);
      toast.success("สมัครสมาชิกสำเร็จ!", { description: "กรุณาเข้าสู่ระบบเพื่อจองตั๋ว" });
      router.push("/login");
    } catch (err: any) {
      toast.error("สมัครสมาชิกไม่สำเร็จ", { description: err.message });
    } finally { setLoading(false); }
  };

  return (
    <div className="min-h-screen bg-black flex items-center justify-center p-4">
      <Card className="w-full max-w-md bg-zinc-900 border-zinc-800 text-white">
        <CardHeader className="text-center">
          <CardTitle className="text-3xl font-bold italic">JOIN TICKET-HUB</CardTitle>
          <CardDescription className="text-zinc-400">สร้างบัญชีใหม่เพื่อเริ่มจองคอนเสิร์ต</CardDescription>
        </CardHeader>
        <form onSubmit={handleSubmit(onSubmit)}>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label>Username</Label>
              <Input {...register("username")} className="bg-zinc-800 border-zinc-700" placeholder="tony_star" />
              {errors.username && <p className="text-red-500 text-xs">{errors.username.message as string}</p>}
            </div>
            <div className="space-y-2">
              <Label>Email</Label>
              <Input {...register("email")} type="email" className="bg-zinc-800 border-zinc-700" placeholder="tony@test.com" />
              {errors.email && <p className="text-red-500 text-xs">{errors.email.message as string}</p>}
            </div>
            <div className="space-y-2">
              <Label>Password</Label>
              <Input {...register("password")} type="password" className="bg-zinc-800 border-zinc-700" />
              {errors.password && <p className="text-red-500 text-xs">{errors.password.message as string}</p>}
            </div>
          </CardContent>
          <CardFooter className="flex flex-col gap-4">
            <Button type="submit" disabled={loading} className="w-full bg-blue-600 hover:bg-blue-500 py-6 text-lg font-bold rounded-xl cursor-pointer">
              {loading ? <Loader2 className="animate-spin mr-2" /> : <UserPlus className="mr-2" />} สมัครสมาชิก
            </Button>
            <p className="text-sm text-zinc-500">มีบัญชีอยู่แล้ว? <Link href="/login" className="text-blue-400 hover:underline">เข้าสู่ระบบ</Link></p>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}