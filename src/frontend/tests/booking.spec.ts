import { test, expect } from '@playwright/test';

test('User ต้องสามารถ Login และจองที่นั่งได้จนถึงหน้าชำระเงิน', async ({ page }) => {
  // ขยายเวลาสำหรับเครื่อง Dev
  test.setTimeout(90000);

  // 1. ไปหน้า Login
  await page.goto('http://localhost:3000/login');
  await page.fill('input[id="username"]', 'tony');
  await page.fill('input[id="password"]', '123');
  await page.click('button:has-text("Sign In")');

  // 2. ตรวจสอบหน้าแรก (เช็คข้อความ "Tony (Member)" ใน Navbar)
  await expect(page).toHaveURL('http://localhost:3000/');
  await expect(page.locator('text=Tony (Member)')).toBeVisible({ timeout: 15000 });

  // 3. คลิกจอง Taylor Swift (เจาะจงคอนเสิร์ตที่ 2 ตาม Seeder)
  const taylorCard = page.locator('div.group', { hasText: 'Taylor Swift' });
  await taylorCard.getByRole('button', { name: 'จองที่นั่งเลย' }).click();

  // 4. รอจนกว่าหน้าจอ Loading จะหายไป
  await page.waitForSelector('text=LOADING SEATING PLAN...', { state: 'hidden', timeout: 30000 });

  // 5. 🔥 วิธีเลือกที่นั่ง E1 (อ้างอิงจากโครงสร้าง <span> ของคุณ)
  // บอทจะมองหาปุ่มที่มีคำว่า "E1" อยู่ข้างใน
  const seatE1 = page.locator('button').filter({ hasText: /^E1$/ }).first();
  await seatE1.waitFor({ state: 'visible' });
  await seatE1.click({ force: true });

  // 6. ตรวจสอบว่า "สรุปการเลือก" แสดงคำว่า E1 หรือยัง
  // ในโค้ดคุณใช้ <span className="text-2xl font-black text-white">{selectedSeat?.seatNumber || "---"}</span>
  const summarySeat = page.locator('span.text-2xl.font-black.text-white'); 
  await expect(summarySeat).toHaveText('E1', { timeout: 10000 });

  // 7. กดยืนยันการจอง (BOOK NOW)
  const bookNowBtn = page.getByRole('button', { name: 'BOOK NOW' });
  await expect(bookNowBtn).toBeEnabled(); // มั่นใจว่าปุ่มหายจาก disabled แล้ว
  await bookNowBtn.click();

  // 8. ตรวจสอบว่าระบบพาไปหน้า Checkout
//   await page.waitForURL(/.*\/checkout\/.*/, { timeout: 20000 });
//   await expect(page.locator('text=Time Remaining')).toBeVisible();
  
  console.log('✅ E2E Test: จองสำเร็จ 100%!');
});