# 🏗️ System Architecture

ไฟล์นี้อธิบายโครงสร้างและการไหลของข้อมูลภายในระบบ Concert Ticket Booking

## 📊 System Overview Diagram

```mermaid
graph TD
    subgraph Client_Side [Frontend - Next.js]
        Web[Web Browser]
    end

    subgraph Entry_Point [API Gateway]
        YARP[YARP - C# API Gateway]
    end

    subgraph Backend_Services [Microservices]
        ServiceA[Service A - ASP.NET Core]
        ServiceB[Service B - Node.js TypeScript]
    end

    subgraph Infrastructure
        Postgres[(PostgreSQL)]
        Redis[(Redis - Distributed Lock)]
        RabbitMQ[RabbitMQ - Message Broker]
        Jaeger[Jaeger - Tracing]
    end

    %% Flow ของการทำงาน
    Web -->|HTTP Request| YARP
    YARP -->|Rate Limit & Route| ServiceA
    
    ServiceA <-->|Locking| Redis
    ServiceA <-->|Persistence| Postgres
    ServiceA -->|Event| RabbitMQ
    
    RabbitMQ -->|Consume Event| ServiceB
    ServiceB -->|SMTP| Mailtrap[Mailtrap - Email Service]
    ServiceB -->|gRPC Call| ServiceA
    
    %% Monitoring
    ServiceA & ServiceB -.->|Telemetry| Jaeger
```

## 💡 Key Architectural Decisions

1. **API Gateway (YARP):** ใช้เพื่อทำ Centralized Entry Point และจัดการ Cross-Cutting Concerns เช่น Rate Limiting และ Correlation ID
2. **Distributed Locking (Redis):** เพื่อจัดการสภาวะการแย่งชิงทรัพยากร (Race Condition) ในระบบที่มีผู้ใช้จำนวนมาก
3. **Event-Driven Architecture (RabbitMQ):** เพื่อทำ Decoupling ระหว่างระบบจองและระบบแจ้งเตือน ช่วยเพิ่ม Scalability และ Availability
4. **gRPC for Internal Comm:** เลือกใช้ gRPC เพราะต้องการประสิทธิภาพสูงสุดในการแลกเปลี่ยนข้อมูลระหว่าง Microservices
5. **Observability (Jaeger):** เพื่อช่วยในการ Debug และ Monitoring ระบบที่มีความซับซ้อนแบบ Distributed System