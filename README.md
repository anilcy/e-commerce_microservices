# E-Commerce Microservices

A production-style distributed backend built with .NET 8, demonstrating microservices architecture, CQRS/MediatR, async messaging via RabbitMQ, and API Gateway routing with YARP.

## Architecture

```
Client
  │
  ▼
Gateway (YARP) :5000          ← Single entry point
  ├── /api/auth/*    → Identity Service :5001
  ├── /api/products/* → Catalog Service  :5002
  └── /api/orders/*  → Order Service    :5003

RabbitMQ                      ← Async inter-service messaging
  ├── OrderPlacedEvent        Order → Catalog (reserve stock)
  ├── StockReservedEvent      Catalog → Order (confirm/fail)
  ├── OrderCancelledEvent     Order → Catalog (restore stock)
  └── UserRegisteredEvent     Identity → (extensible)

Databases (one per service)
  ├── identity_db   (PostgreSQL)
  ├── catalog_db    (PostgreSQL)
  └── order_db      (PostgreSQL)
```

## Key Patterns Implemented

### CQRS + MediatR
Every service separates reads from writes using MediatR:
- **Commands** — mutate state: `RegisterCommand`, `PlaceOrderCommand`, `CancelOrderCommand`, `CreateProductCommand`
- **Queries** — read state: `GetUserByIdQuery`, `GetProductsQuery`, `GetOrdersByUserQuery`
- Controllers are thin — they only call `_mediator.Send(command)` and return the result

### Async Messaging (Event-Driven)
Services never call each other directly over HTTP. Instead they publish integration events:
1. User places order → `OrderPlacedEvent` published
2. Catalog Service consumes it → reserves stock → publishes `StockReservedEvent`
3. Order Service consumes it → confirms or fails the order

This means services are **fully decoupled** — Catalog can be down and Orders still get accepted.

### API Gateway (YARP)
All traffic enters through port 5000. YARP routes based on URL path prefix. Services are not exposed directly in production.

### Database per Service
Each service owns its database. No shared tables. The Order Service stores a **snapshot** of the product name at order time — so if a product is renamed later, old orders are unaffected.

## Running Locally

```bash
# Start everything
docker-compose up --build

# Services available at:
# Gateway (use this one) → http://localhost:5000
# Identity Swagger       → http://localhost:5001/swagger
# Catalog Swagger        → http://localhost:5002/swagger
# Order Swagger          → http://localhost:5003/swagger
# RabbitMQ Management    → http://localhost:15672  (guest/guest)
```

## API Flow Example

```bash
# 1. Register
POST http://localhost:5000/api/auth/register
{ "email": "user@example.com", "username": "john", "password": "secret123" }

# 2. Login → get JWT
POST http://localhost:5000/api/auth/login
{ "email": "user@example.com", "password": "secret123" }

# 3. Browse products
GET http://localhost:5000/api/products

# 4. Place an order (with Bearer token)
POST http://localhost:5000/api/orders
Authorization: Bearer <token>
{
  "shippingAddress": "Warsaw, Poland",
  "items": [
    { "productId": "...", "productName": "Laptop", "quantity": 1, "unitPrice": 2999.99 }
  ]
}

# 5. Order starts as "Pending", becomes "Confirmed" once Catalog reserves stock
GET http://localhost:5000/api/orders/{id}
```

## Running Migrations (first time)

```bash
# Identity
dotnet ef migrations add InitialCreate -p src/Services/Identity/Identity.API

# Catalog
dotnet ef migrations add InitialCreate -p src/Services/Catalog/Catalog.API

# Order
dotnet ef migrations add InitialCreate -p src/Services/Order/Order.API
```

## Tech Stack

| Technology | Purpose |
|---|---|
| .NET 8 / ASP.NET Core | All services |
| MediatR 12 | CQRS implementation |
| FluentValidation | Command/Query validation |
| Entity Framework Core 8 | ORM |
| PostgreSQL | Database (one per service) |
| MassTransit + RabbitMQ | Async messaging |
| YARP | API Gateway / reverse proxy |
| Docker Compose | Local orchestration |
| JWT Bearer | Authentication (shared secret) |

## Project Structure

```
src/
  Services/
    Identity/Identity.API/
      Controllers/        ← Thin controllers
      Features/
        Auth/Commands/    ← RegisterCommand, LoginCommand
        Users/            ← GetUserByIdQuery
      Domain/Entities/    ← User
      Infrastructure/     ← DbContext
    Catalog/Catalog.API/
      Features/
        Products/Commands/ ← CreateProductCommand
        Products/Queries/  ← GetProductsQuery, GetProductByIdQuery
      Infrastructure/
        Consumers/         ← OrderPlacedConsumer (handles RabbitMQ events)
    Order/Order.API/
      Features/
        Orders/Commands/   ← PlaceOrderCommand, CancelOrderCommand
        Orders/Queries/    ← GetOrdersByUserQuery, GetOrderByIdQuery
      Infrastructure/
        Consumers/         ← StockReservedConsumer
  Gateway/Gateway.API/    ← YARP config only, no business logic
  Shared/Shared.Messaging/ ← Integration event contracts (shared NuGet-style)
docker/                   ← Dockerfiles per service
docker-compose.yml
```
