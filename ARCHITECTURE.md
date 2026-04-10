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