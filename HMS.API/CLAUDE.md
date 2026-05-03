# CLAUDE.md — Hotel Management System (HMS)

## Project Overview
A full-stack Hotel Management System built for UWE Bristol Advanced Software Development module (UFCF8S-30-2). Group project built by up to 3 students.

## Team
- **Iyaadh** — Backend (ASP.NET Core API, Auth, Database, Emails, QR, PDF)
- **Member 2** — Frontend (Angular, UI/UX, GSAP Animations, FullCalendar)
- **Member 3 (maybe)** — Database seeding, Testing, Reports export

---

## Tech Stack

### Backend
- ASP.NET Core 10 Web API
- Entity Framework Core (ORM)
- PostgreSQL (Neon)
- ASP.NET Core Identity (Auth)
- JWT Bearer Tokens
- QRCoder (QR code generation)
- Resend .NET SDK (emails)
- SignalR (real-time occupancy)

### Frontend
- Angular 17+
- Tailwind CSS
- GSAP (animations)
- FullCalendar (availability views)
- Chart.js (analytics/reports)
- ngx-scanner (QR code scanning)

### Infrastructure
- GitHub Org: hms-project
- API hosted on: Railway (auto deploy from main)
- Frontend hosted on: Vercel (auto deploy from main)
- Database hosted on: Neon PostgreSQL

---

## Repository Structure

### hms-api (ASP.NET Core)
```
hms-api/
├── Controllers/
│   ├── AuthController.cs
│   ├── HotelController.cs
│   ├── RoomController.cs
│   ├── BookingController.cs
│   ├── PaymentController.cs
│   ├── CheckInController.cs
│   ├── CheckOutController.cs
│   ├── ReportController.cs
│   ├── UserController.cs
│   ├── AuditLogController.cs
│   └── AncillaryServiceController.cs
├── Models/
│   ├── User.cs
│   ├── Hotel.cs
│   ├── Room.cs
│   ├── Booking.cs
│   ├── Payment.cs
│   ├── AncillaryService.cs
│   ├── BookingService.cs
│   ├── AuditLog.cs
│   └── Waitlist.cs
├── DTOs/
│   ├── Auth/
│   ├── Booking/
│   ├── Room/
│   ├── Payment/
│   └── Report/
├── Services/
│   ├── AuthService.cs
│   ├── BookingService.cs
│   ├── PaymentService.cs
│   ├── EmailService.cs
│   ├── QRCodeService.cs
│   ├── PDFService.cs
│   ├── ReportService.cs
│   └── AuditLogService.cs
├── Middleware/
│   ├── AuditLoggingMiddleware.cs
│   └── SecurityHeadersMiddleware.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Seeders/
│       ├── HotelSeeder.cs
│       ├── RoomSeeder.cs
│       └── UserSeeder.cs
├── Migrations/
├── Hubs/
│   └── OccupancyHub.cs (SignalR)
└── Program.cs
```

### hms-frontend (Angular)
```
hms-frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── services/
│   │   │   │   ├── auth.service.ts
│   │   │   │   ├── booking.service.ts
│   │   │   │   ├── room.service.ts
│   │   │   │   ├── payment.service.ts
│   │   │   │   └── report.service.ts
│   │   │   ├── guards/
│   │   │   │   ├── auth.guard.ts
│   │   │   │   ├── guest.guard.ts
│   │   │   │   ├── staff.guard.ts
│   │   │   │   ├── manager.guard.ts
│   │   │   │   └── admin.guard.ts
│   │   │   └── interceptors/
│   │   │       ├── jwt.interceptor.ts
│   │   │       └── error.interceptor.ts
│   │   ├── shared/
│   │   │   └── components/
│   │   │       ├── navbar/
│   │   │       ├── footer/
│   │   │       ├── loading/
│   │   │       └── toast/
│   │   ├── pages/
│   │   │   ├── welcome/ (door opening animation)
│   │   │   ├── home/ (video hero)
│   │   │   ├── auth/
│   │   │   │   ├── login/
│   │   │   │   └── register/
│   │   │   ├── guest/
│   │   │   │   ├── search/
│   │   │   │   ├── room-detail/
│   │   │   │   ├── booking/
│   │   │   │   ├── my-bookings/
│   │   │   │   └── profile/
│   │   │   ├── staff/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── check-in/
│   │   │   │   ├── check-out/
│   │   │   │   └── room-management/
│   │   │   ├── manager/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── reports/
│   │   │   │   └── settings/
│   │   │   └── admin/
│   │   │       ├── dashboard/
│   │   │       ├── users/
│   │   │       └── hotels/
│   │   └── app.routes.ts
│   ├── environments/
│   │   ├── environment.ts (dev)
│   │   └── environment.prod.ts (prod)
│   └── styles.css
```

---

## Database Schema

### Users
```
id, email, passwordHash, firstName, lastName,
role (Guest/FrontDesk/Manager/Admin),
phoneNumber, isActive, failedLoginAttempts,
lockoutEnd, lastPasswordChange, createdAt
```

### Hotels
```
id, name, location, address, description,
imageUrl, isActive, createdAt
```

### Rooms
```
id, hotelId, roomNumber, type (Standard/Deluxe/Family/Penthouse),
capacity, priceOffPeak, pricePeak, status (Available/Occupied/Cleaning/OutOfService),
description, imageUrls, floor, createdAt
```

### Bookings
```
id, guestId, hotelId, referenceNumber (HMS-YYYY-XXXXX),
checkInDate, checkOutDate, totalPrice, status (Pending/Confirmed/CheckedIn/CheckedOut/Cancelled),
cancellationFee, qrCodeUrl, createdAt, updatedAt
```

### BookingRooms (junction)
```
id, bookingId, roomId
```

### Payments
```
id, bookingId, amount, method (Mock),
status (Pending/Completed/Refunded),
transactionReference, processedAt
```

### AncillaryServices
```
id, name, price, description
```

### BookingAncillaryServices (junction)
```
id, bookingId, serviceId, quantity, totalPrice
```

### AuditLogs
```
id, userId, action, entityType, entityId,
ipAddress, details, createdAt
```

### Waitlist
```
id, guestId, hotelId, roomType,
checkInDate, checkOutDate, status, createdAt
```

---

## User Roles & Permissions

| Role | Access |
|---|---|
| Guest | Search, Book, Cancel, Profile, Invoice |
| FrontDesk | CheckIn, CheckOut, RoomStatus, Payments |
| Manager | Reports, RoomRates, StaffAccounts |
| Admin | AllUsers, HotelConfig, SystemSettings |

---

## Room Types & Pricing

| Type | Capacity | Off-Peak (GBP) | Peak (GBP) |
|---|---|---|---|
| Standard Double | 2 | 120 | 180 |
| Deluxe King | 2 | 180 | 250 |
| Family Suite | 4 | 240 | 320 |
| Penthouse | 4 | 500 | 750 |

Peak season: June, July, August, December

---

## Cancellation Policy

| Notice | Fee |
|---|---|
| 14+ days | Free |
| 3–14 days | 50% of first night |
| < 72 hours | 100% of first night |
| No-show | 100% of entire booking |

---

## Ancillary Services

| Service | Price (GBP) |
|---|---|
| Airport Transfer (one-way) | 50 |
| Full English Breakfast (per person/day) | 20 |
| Spa Access (per person/day) | 35 |
| Late Check-out (until 2PM) | 40 |

---

## API Base URLs

- Development API: `http://localhost:5000`
- Production API: `https://hms-api.up.railway.app`
- Development Frontend: `http://localhost:4200`
- Production Frontend: `https://hms.vercel.app`

---

## Key API Endpoints

### Auth
```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
POST /api/auth/refresh-token
POST /api/auth/change-password
```

### Hotels
```
GET    /api/hotels
POST   /api/hotels (Admin)
PUT    /api/hotels/{id} (Admin)
DELETE /api/hotels/{id} (Admin)
```

### Rooms
```
GET    /api/rooms?hotelId=&type=&checkIn=&checkOut=&capacity=
GET    /api/rooms/{id}
POST   /api/rooms (Admin/Manager)
PUT    /api/rooms/{id} (Admin/Manager/FrontDesk)
GET    /api/rooms/availability?hotelId=&checkIn=&checkOut=
```

### Bookings
```
GET    /api/bookings (own bookings for Guest, all for Staff+)
GET    /api/bookings/{id}
POST   /api/bookings
PUT    /api/bookings/{id}
DELETE /api/bookings/{id} (cancel)
GET    /api/bookings/{id}/qr
GET    /api/bookings/{id}/invoice
```

### Check-in / Check-out
```
POST   /api/checkin/{bookingId}
POST   /api/checkout/{bookingId}
POST   /api/checkin/scan (QR scan)
```

### Payments
```
POST   /api/payments/process
GET    /api/payments/{bookingId}
```

### Reports (Manager+)
```
GET    /api/reports/occupancy?period=daily|monthly|yearly
GET    /api/reports/revenue?period=
GET    /api/reports/demographics
GET    /api/reports/export?type=pdf|excel
```

### Users (Admin)
```
GET    /api/users
POST   /api/users
PUT    /api/users/{id}
DELETE /api/users/{id}
```

### Audit Logs (Admin/Manager)
```
GET    /api/auditlogs?page=&limit=
```

---

## Security Requirements

- JWT expiry: 15 minutes (access token), 7 days (refresh token)
- Account lockout: 5 failed attempts → 15 minute lockout
- Auto logout: 15 minutes inactivity (frontend timer)
- Password policy: min 8 chars, upper, lower, number, special char
- Force password change: every 6 months for Admin/Manager
- HSTS enabled in production
- Secure + HttpOnly cookies for refresh token
- Audit log: all logins, bookings, payments, check-in/out
- Encrypt sensitive data at rest (EF Core value converters)

---

## Email Templates (Resend)

- `booking-confirmation` — includes QR code image, booking details
- `booking-cancellation` — includes cancellation fee details
- `checkin-confirmation` — welcome message
- `invoice` — itemized bill PDF attached
- `password-change-required` — for Admin/Manager 6 month policy

---

## Coding Conventions

### Backend (C#)
- PascalCase for classes, methods, properties
- camelCase for local variables
- Every controller method must be async
- Use DTOs for all request/response — never expose models directly
- Every student ID + name in file header comment
- Use repository pattern for data access

### Frontend (Angular/TypeScript)
- camelCase for variables and methods
- PascalCase for components and services
- Every component has its own folder
- Use Angular signals where possible
- Every student ID + name in file header comment

---

## Branch Strategy

```
main → production (auto deploys)
dev → staging (test before merging to main)
feature/task-name → individual features
```

### Git Commit Format
```
feat: add room search endpoint
fix: booking cancellation fee calculation
style: update navbar spacing
docs: update CLAUDE.md
test: add booking unit tests
```

---

## Environment Variables

### Backend (.env)
```
DATABASE_URL=
JWT_SECRET=
JWT_EXPIRY_MINUTES=15
REFRESH_TOKEN_EXPIRY_DAYS=7
RESEND_API_KEY=
FRONTEND_URL=
```

### Frontend (environment.ts)
```
apiUrl: ''
signalRUrl: ''
```

---

## File Header Comment (Required on all files)
```csharp
// Student ID: WP1234567
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)
```

---

## Current Status

### ✅ Done
- Nothing yet

### 🔄 In Progress
- Project setup

### 📋 TODO
- See full task breakdown in project board
