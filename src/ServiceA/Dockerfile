# 1. ใช้ SDK ในการ Build โค้ด
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# ก๊อปปี้ไฟล์โปรเจกต์ทั้งหมด
COPY . .
RUN dotnet restore
RUN dotnet publish TicketBooking.Api/TicketBooking.Api.csproj -c Release -o /out

# 2. ใช้ Runtime ขนาดเล็กในการรัน
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .

# เปิดพอร์ต 5018 (API) และ 5019 (gRPC)
EXPOSE 5018
EXPOSE 5019

ENTRYPOINT ["dotnet", "TicketBooking.Api.dll"]