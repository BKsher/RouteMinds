# RouteMinds 🚛

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Docker](https://img.shields.io/badge/Docker-Compose-blue)
![Architecture](https://img.shields.io/badge/Architecture-Microservices-green)
![Status](https://img.shields.io/badge/Status-Prototype-yellow)

**RouteMinds** is a distributed microservices platform designed to solve the Vehicle Routing Problem (VRP) using an event-driven architecture. It demonstrates modern cloud-native patterns including asynchronous messaging, caching, and clean architecture.

## 🏗 Architecture

The system consists of two decoupled services:

1.  **RouteMinds.API**: A RESTful entry point that accepts orders and handles data persistence.
2.  **RouteMinds.Worker**: A background service that listens for events, performs geospatial routing algorithms, and caches results.

### Tech Stack
*   **Core:** .NET 8, C# 12
*   **Data:** Entity Framework Core (PostgreSQL), Redis (Caching)
*   **Messaging:** RabbitMQ (via MassTransit)
*   **Infrastructure:** Docker Compose

## 🚀 Getting Started

### Prerequisites
*   [Docker Desktop](https://www.docker.com/products/docker-desktop/)
*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation
1.  **Clone the repository**
    ```bash
    git clone https://github.com/YOUR_USERNAME/RouteMinds.git
    cd RouteMinds
    ```

2.  **Start Infrastructure**
    Run the following command to spin up PostgreSQL, RabbitMQ, and Redis:
    ```bash
    docker-compose up -d
    ```

3.  **Apply Migrations**
    Initialize the database schema:
    ```bash
    dotnet ef database update --project RouteMinds.Infrastructure --startup-project RouteMinds.API
    ```

4.  **Run the Services**
    You can run via Visual Studio (Multiple Startup Projects) or CLI:
    ```bash
    dotnet run --project RouteMinds.API
    dotnet run --project RouteMinds.Worker
    ```

### Usage
1.  Open Swagger: `http://localhost:5000/swagger` (Check your actual port).
2.  **POST** `/api/orders`: Creates an order and queues it for processing.
3.  **GET** `/api/orders/{id}/route`: Retrieves the calculated route from Redis.

## 🧪 Key Patterns Implemented
*   **Clean Architecture:** Domain / Infrastructure / Presentation separation.
*   **CQRS-Lite:** Separation of Write (SQL) and Read (Redis) operations.
*   **Event-Driven:** Asynchronous communication via MassTransit.
*   **Repository Pattern:** Abstraction over EF Core.

---
*Created by Volodymyr Tochonyi*
