# NextBus - Ride-Sharing, Transit Booking & Digital Wallet Platform

[![.NET 8 CI](https://github.com/Aitem2012/Next-Bus/actions/workflows/next-bus.yml/badge.svg)](https://github.com/Aitem2012/Next-Bus/actions/workflows/next-bus.yml)
[![Target Framework](https://img.shields.io/badge/.NET-8.0%20(LTS)-512BD4)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture%20%2B%20CQRS-blue)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)

**NextBus** is an enterprise-grade ride-sharing and transit booking platform built with **.NET 8**, **C# 12**, and **Clean Architecture**. It enables drivers to offer rides (with up to 2 intermediate stops, available seats, and seat fares) and passengers to discover matching routes and book seats. Safety is enforced through dual-role KYC (passenger BVN/NIN and driver license verification), with cashless transactions handled via digital wallets, Paystack integration, and platform commission deduction.

---

## Architecture Overview

The solution adheres strictly to Clean Architecture separation of concerns:

```
NextBus/
├── NextBus.Domain/           # Enterprise entities, enums, domain rules (zero external dependencies)
├── NextBus.Application/      # CQRS commands/queries (MediatR), validation (FluentValidation), DTOs, AutoMapper
├── NextBus.Persistence/      # EF Core 8, AppDbContext, SQLite (with WAL mode), PostgreSQL support, Migrations
├── NextBus.Infrastructure/   # External services: JWT auth, Email (ZeptoMail), OTP generator, Paystack API
├── NextBus.API/              # RESTful API controllers, Swagger UI with JWT Bearer, global middleware
└── NextBus.Tests/            # Comprehensive xUnit integration and unit test suite
```

---

## Key Features

### 1. Dual-Role User & Safety KYC
* **Unified Account**: Any registered user can act as both a passenger and a driver.
* **Passenger KYC**: BVN and NIN identity verification required before requesting to join rides.
* **Driver Evidence of Driving**: Driver's License number, expiry date, and document verification required before offering rides.

### 2. Ride Offering & Intermediate Stops
* Drivers specify:
  * Origin (starting location) and Destination
  * Up to **two intermediate stops** (`Stop1`, `Stop2`)
  * Available seats and price per seat
  * Vehicle selection (plate number, make, model, capacity)
* Passengers can search for rides matching their origin, destination, or either stop.

### 3. Escrow Booking & Platform Commission
* When a passenger requests a seat, the fare is reserved from their wallet into escrow.
* Drivers can accept or reject requests:
  * **Accepted**: Seat count is decremented.
  * **Rejected / Cancelled**: Reserved fare is automatically refunded back to the passenger's wallet.
* **Ride Completion & Commission**:
  * Upon driver completing the ride (`POST /api/rides/{id}/complete`), the platform automatically deducts a configurable commission percentage (e.g. 10%) from each passenger booking.
  * Driver's wallet is credited with the net earnings.
  * Transparent audit transactions are recorded for both driver earnings and platform commission.

### 4. Email Sending & One-Time Passwords (OTP)
* **Registration**: User registers with `EmailConfirmed = false` and receives a 6-digit verification code.
* **Verification**: `POST /api/auth/verify-email-otp` verifies the code and activates the account with JWT sign-in.
* **Password Reset**: `POST /api/auth/request-password-reset-otp` and `POST /api/auth/reset-password-otp` allow secure self-service password recovery.
* **Withdrawal Authorization**: `POST /api/wallets/withdraw/request-otp` emails an authorization code before any funds can leave the user's wallet.

### 5. Paystack Cashless Funding & Withdrawals
* **Wallet Top-Up**:
  * `POST /api/wallets/paystack/initialize`: Initiates transaction with Paystack and returns checkout URL.
  * `POST /api/wallets/paystack/verify`: Verifies transaction status and idempotently credits the user's wallet.
  * `POST /api/wallets/paystack/webhook`: Listens for asynchronous `charge.success` events with HMAC-SHA512 signature verification.
* **Withdrawals**:
  * Step 1: Resolves and validates the destination bank account (Account Number + Bank Code).
  * Step 2: User confirms with the OTP received in email.
  * Step 3: NextBus creates a Paystack transfer recipient, initiates transfer, and debits the user wallet.

### 6. Database Durability Across Restarts
* **Canonical Path Resolution**: SQLite connection strings resolve relative paths to a fixed absolute project path, ensuring the database is never recreated in arbitrary working directories.
* **Write-Ahead Logging (WAL)**: `PRAGMA journal_mode=WAL;` and `PRAGMA busy_timeout=5000;` guarantee concurrency and crash safety.
* **EF Core Migrations**: `app.Services.MigrateDatabaseAsync()` applies schema updates safely without dropping existing tables or data.
* **PostgreSQL Ready**: Transparently switches to PostgreSQL when a `Host=` connection string is supplied.

---

## Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=NextBus.db"
  },
  "Jwt": {
    "Key": "NextBus_Super_Secret_Key_For_Authentication_2026_Secure_JWT_Key_Longer_Than_32_Chars",
    "Issuer": "NextBusAPI",
    "Audience": "NextBusClients"
  },
  "Paystack": {
    "SecretKey": "sk_test_...",
    "PublicKey": "pk_test_...",
    "CallbackUrl": "https://nextbus.classicalaitem.com/checkout/complete"
  },
  "ZeptoMail": {
    "FromName": "NextBus Transit",
    "FromAddress": "hello@classicalaitem.com",
    "SmtpHost": "smtp.zeptomail.com",
    "SmtpPort": 587,
    "SmtpUser": "emailapikey",
    "SmtpPassword": "..."
  },
  "PlatformSettings": {
    "CommissionPercentage": 10.0
  }
}
```

---

## Getting Started

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* SQLite (or PostgreSQL)

### Run the Application
```bash
# 1. Restore dependencies
dotnet restore NextBus.sln

# 2. Run test suite
dotnet test NextBus.sln

# 3. Launch API
dotnet run --project NextBus.API
```

Visit Swagger UI at: `http://localhost:5000/`

---

## API Endpoints Summary

### Authentication (`/api/auth`)
* `POST /api/auth/register` - Register user & send email verification OTP
* `POST /api/auth/verify-email-otp` - Verify 6-digit registration OTP
* `POST /api/auth/resend-email-otp` - Resend verification code
* `POST /api/auth/request-password-reset-otp` - Request password reset OTP
* `POST /api/auth/reset-password-otp` - Reset password with OTP
* `POST /api/auth/login` - Authenticate with email & password
* `GET  /api/auth/me` - Get profile of authenticated user
* `POST /api/auth/kyc` - Submit passenger KYC (BVN, NIN)
* `POST /api/auth/driver-license` - Submit driver's license for verification

### Vehicles (`/api/vehicles`)
* `POST /api/vehicles` - Register vehicle (make, model, plate number, seats)
* `GET  /api/vehicles/my-vehicles` - List authenticated driver's vehicles

### Rides (`/api/rides`)
* `POST /api/rides` - Offer a ride (origin, destination, max 2 stops, seats, fare)
* `GET  /api/rides/search` - Search rides matching route & intermediate stops
* `GET  /api/rides/{id}` - Get ride details
* `GET  /api/rides/my-offered-rides` - List rides offered by current driver
* `POST /api/rides/{id}/start` - Start scheduled ride (driver only)
* `POST /api/rides/{id}/complete` - Complete ride & credit driver (net of commission)
* `POST /api/rides/{id}/cancel` - Cancel ride & auto-refund passengers

### Bookings (`/api/bookings`)
* `POST /api/bookings` - Request to join a ride (escrows wallet balance)
* `POST /api/bookings/{id}/respond` - Driver accept or reject request
* `POST /api/bookings/{id}/cancel` - Passenger cancel request & get refund
* `GET  /api/bookings/my-bookings` - List passenger's bookings
* `GET  /api/bookings/rides/{rideId}` - List booking requests for a ride

### Wallets & Payments (`/api/wallets`)
* `GET  /api/wallets/my-wallet` - View balance, history & transactions
* `POST /api/wallets/paystack/initialize` - Initialize Paystack top-up
* `POST /api/wallets/paystack/verify` - Verify payment & credit wallet
* `POST /api/wallets/paystack/webhook` - Paystack webhook listener
* `POST /api/wallets/withdraw/request-otp` - Step 1: Verify destination account & send OTP
* `POST /api/wallets/withdraw/confirm` - Step 2: Confirm OTP & execute transfer
