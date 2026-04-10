FROM node:20-alpine
WORKDIR /app

# ก๊อปปี้แค่ไฟล์ config ก่อนเพื่อให้ Docker cache layer ได้
COPY package*.json ./

# ติดตั้ง dependencies
RUN npm install

# ก๊อปปี้ไฟล์ที่เหลือ (ต้องมี .dockerignore เพื่อไม่ให้เอา node_modules จากเครื่องเราเข้าไป)
COPY . .

# สั่ง build
RUN npm run build

EXPOSE 3000

# รันด้วยโหมดปกติก่อนเพื่อให้เห็นผลเร็วและเสถียรกว่า standalone ในช่วงพัฒนา
CMD ["npm", "start"]