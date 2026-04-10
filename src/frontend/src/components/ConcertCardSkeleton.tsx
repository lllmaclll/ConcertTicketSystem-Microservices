import { Skeleton } from "@/components/ui/skeleton"
import { Card, CardContent, CardFooter } from "@/components/ui/card"

export function ConcertCardSkeleton() {
  return (
    <Card className="bg-zinc-900 border-zinc-800 overflow-hidden flex flex-col h-full border-none shadow-2xl">
      {/* ส่วนรูปภาพจำลอง */}
      <Skeleton className="relative aspect-[3/4] w-full bg-zinc-800" />
      
      <CardContent className="p-8 grow space-y-4">
        {/* ชื่อคอนเสิร์ตจำลอง */}
        <Skeleton className="h-8 w-3/4 bg-zinc-800" />
        
        <div className="space-y-3 mt-auto">
          {/* วันที่จำลอง */}
          <div className="flex items-center gap-3">
            <Skeleton className="h-8 w-8 rounded-lg bg-zinc-800" />
            <Skeleton className="h-4 w-1/2 bg-zinc-800" />
          </div>
          {/* สถานที่จำลอง */}
          <div className="flex items-center gap-3">
            <Skeleton className="h-8 w-8 rounded-lg bg-zinc-800" />
            <Skeleton className="h-4 w-2/3 bg-zinc-800" />
          </div>
        </div>
      </CardContent>

      <CardFooter className="p-8 pt-0 mt-auto">
        {/* ปุ่มกดจำลอง */}
        <Skeleton className="h-16 w-full rounded-2xl bg-zinc-800" />
      </CardFooter>
    </Card>
  )
}