FROM node:20-alpine
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .

# 1. คอมไพล์ TypeScript
RUN npx tsc

# 2. ก๊อปปี้โฟลเดอร์ Protos เข้าไปใน dist (จุดที่ต้องเพิ่ม 🔥)
RUN mkdir -p dist/Protos && cp Protos/concert.proto dist/Protos/

CMD ["node", "dist/index.js"]