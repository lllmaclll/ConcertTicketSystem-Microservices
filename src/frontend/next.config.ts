import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  output: 'standalone', // 🔥 เพิ่มบรรทัดนี้เพื่อให้รันใน Docker ได้
  images: {
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '5177', // พอร์ตของ API Gateway
        pathname: '/posters/**',
      },
    ],
  },
};

export default nextConfig;
