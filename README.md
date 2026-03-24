# Hotel Booking API — Backend

> **ASP.NET Web API 8.0** · **Entity Framework Core** · **SQL Server** · **JWT + Refresh Tokens** · **Cloudinary**

REST API for a hotel room booking system. Handles room management, dynamic pricing, reservations with atomic availability checks, mock payments, user roles, photo uploads, and full audit logging.

---

## Table of Contents

1. [Technology Stack](#1-technology-stack)
2. [Architecture & Design Patterns](#2-architecture--design-patterns)
3. [Hotel Business Standards](#3-hotel-business-standards)
4. [Project Structure](#4-project-structure)
5. [Getting Started](#5-getting-started)
6. [Configuration](#6-configuration)
7. [Demo Accounts](#7-demo-accounts)
8. [Authentication & Authorization](#8-authentication--authorization)
9. [API Endpoints](#9-api-endpoints)
10. [Data Models](#10-data-models)
11. [Key Business Logic](#11-key-business-logic)
12. [Error Handling](#12-error-handling)
13. [Security](#13-security)
14. [Pagination & Sorting](#14-pagination--sorting)
15. [Demo Seed Data](#15-demo-seed-data)
16. [Demo Scenario](#16-demo-scenario)

---

## 1. Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Web API 8.0 (C#) |
| ORM | Entity Framework Core 8 — Code-First migrations |
| Database | Microsoft SQL Server (LocalDB / full instance) |
| Authentication | JWT Bearer tokens + rotating Refresh Tokens (HttpOnly cookie) |
| Authorization | Role-Based Access Control — Admin / Moderator / Customer / Guest |
| Validation | FluentValidation with auto-validation pipeline |
| Object Mapping | AutoMapper 13 |
| File Storage | Cloudinary (prod) / Local `/wwwroot/uploads` (dev fallback) |
| Background Jobs | `IHostedService` — `ReservationCompletionJob` (runs hourly) |
| Documentation | Swagger / OpenAPI with XML comments and JWT security scheme |
| Password Hashing | BCrypt.Net-Next |

---

## 2. Architecture & Design Patterns

### 2.1 Layered Architecture

The project is organized into distinct horizontal layers with strict dependency direction:

```
Controllers  →  Services (via Interfaces)  →  DbContext  →  SQL Server
                     ↕
              DTOs / AutoMapper / Validators
```

- **Controllers** — handle HTTP concerns only (routing, status codes, reading claims). No business logic.
- **Services** — all business logic lives here. Each service receives `actorUserId` and `ip` for audit tracing.
- **DbContext** — data access only. No service calls inside models or DbContext.
- **DTOs** — all API input/output goes through DTOs; entities are never exposed directly.

### 2.2 Service Layer Pattern

All business logic is encapsulated in scoped services behind interfaces:

```
IAuthService        →  AuthService
IRoomTypeService    →  RoomTypeService
IRoomService        →  RoomService
ITagService         →  TagService
IPriceRuleService   →  PriceRuleService
IReservationService →  ReservationService
IUserService        →  UserService
IAuditLogService    →  AuditLogService
IFileStorageService →  CloudinaryFileStorageService / LocalFileStorageService
```

Controllers depend on interfaces — not concrete implementations — enabling substitution and testability.

### 2.3 Repository Pattern (via EF Core DbContext)

`HotelDbContext` acts as the unit-of-work and repository. All queries go through `DbSet<T>` properties. Services receive the context via constructor injection.

### 2.4 DTO Pattern

Every API request and response uses a dedicated DTO class. Entities never leave the service layer. AutoMapper handles all entity ↔ DTO conversions via a single centralized `MappingProfile`.

### 2.5 Strategy Pattern — Price Calculation

`PriceRuleService.CalculatePriceAsync()` applies a dynamic set of price rules (strategies) to each night of a stay. Rules are loaded by date range and room type, then applied in sequence — each rule contributes a delta (surcharge or discount, fixed or percentage). Adding new pricing strategies requires only a new `PriceRule` record, not code changes.

### 2.6 Middleware Pipeline Pattern

Cross-cutting concerns are implemented as ASP.NET middleware:

- **`GlobalExceptionMiddleware`** — catches all unhandled exceptions and maps them to RFC 7807 `ProblemDetails` responses with correct HTTP status codes.
- **JWT Bearer middleware** — validates access tokens on every request.
- **CORS middleware** — restricts origins to the configured frontend URL.

### 2.7 Options / Extension Method Pattern

`Program.cs` is kept clean by delegating all registrations to focused extension methods:

```csharp
// Program.cs — clean entry point
builder.Services
    .AddControllersWithValidation()
    .AddDatabase(builder.Configuration)
    .AddApplicationServices()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorizationPolicies()
    .AddFrontendCors()
    .AddSwaggerWithJwt();
```

Each extension method in `ServiceCollectionExtensions` and `WebApplicationExtensions` handles one concern.

### 2.8 Factory / Seeder Pattern

`DatabaseSeeder` is a static factory that produces a consistent, reproducible initial state on every startup. It is **idempotent** — skips entities that already exist. This ensures a fresh clone can be demoed immediately without manual setup.

### 2.9 Background Service Pattern

`ReservationCompletionJob` extends `BackgroundService` (IHostedService). It creates its own DI scope per tick (to safely use scoped `DbContext`) and runs on a `PeriodicTimer` with a one-hour interval. This follows the recommended pattern for long-running background work in ASP.NET Core.

### 2.10 Optimistic Concurrency

`Reservation` has a `ConcurrencyToken` (`byte[]` mapped as `rowversion`/`timestamp`). EF Core uses this to detect concurrent updates — if two users modify the same reservation simultaneously, the second write is rejected.

### 2.11 Policy-Based Authorization

Rather than scattering `[Authorize(Roles = "...")]` across controllers, named policies are declared once in `AddAuthorizationPolicies()`. Controllers reference policies by name (e.g., `[Authorize(Policy = "RoomTypeWrite")]`), making permission changes a single-location edit.

---

## 3. Hotel Business Standards

The following rules reflect standard hotel industry practices and are enforced at the service layer.

### 3.1 Reservation Hold (15-Minute Block)

When a guest initiates a reservation, the selected room is immediately blocked for **15 minutes** (`HeldUntil = UtcNow + 15 min`) with status `Pending`. This prevents another guest from booking the same room while the first guest completes payment — standard practice in online hotel booking systems (similar to OTAs like Booking.com).

If payment is not completed within 15 minutes, the hold expires and the room becomes available again.

### 3.2 Cancellation Policy (7-Day Rule)

Guests may cancel their own confirmed reservation only if the check-in date is **at least 7 days away**. Last-minute cancellations require staff (Admin / Moderator) intervention. This mirrors the standard flexible cancellation policy used by most independent hotels.

### 3.3 Price Floor (Minimum 30% of Base Price)

No combination of discounts can reduce the nightly rate below **30% of the room's base price**. This protects revenue integrity and prevents misconfigured discount rules from making rooms effectively free.

```
finalDailyPrice = Max(calculatedPrice, basePrice * 0.30)
```

### 3.4 Dynamic Pricing Calendar

Two types of price rules are supported:

- **SeasonalRange** — applies a surcharge or discount across a date range (e.g., summer season +25%, New Year +40%).
- **SpecialDate** — applies to a single specific date (e.g., a local event).

Rules can be **global** (apply to all room types) or **room-type-specific**. Multiple rules can overlap on the same date — all are applied cumulatively, subject to the price floor.

### 3.5 Auto-Completion of Stays

Reservations with status `Confirmed` whose `EndDate` has passed are automatically transitioned to `Completed` by the background job. This keeps the booking calendar accurate and enables correct reporting without manual intervention.

### 3.6 Double-Booking Prevention

Atomic availability check uses a database transaction. The service finds the first available room of the requested type with no overlapping `Pending` or `Confirmed` reservations. If none is found, `409 Conflict` is returned. EF Core optimistic concurrency (`ConcurrencyToken`) provides an additional safety net for high-concurrency scenarios.

### 3.7 Full Audit Trail

Every write operation across all domains (reservations, rooms, prices, users) is recorded in `AuditLog` with:
- Actor user ID (null for guest/system actions)
- Client IP address
- Old and new values serialized as JSON
- UTC timestamp

This satisfies standard hotel operational requirements for traceability and dispute resolution.

### 3.8 Multilingual Tags

Room amenity tags support multiple language translations stored as a JSON dictionary (`{ "en": "Sea View", "ru": "Вид на море", "az": "Dəniz mənzərəsi" }`). Supported languages are configured via `SupportedLanguages` in `appsettings.json`.

---

## 4. Project Structure

```
HotelWebApplication/
├── BackgroundJobs/
│   └── ReservationCompletionJob.cs     # Hourly job: auto-completes past stays
├── Common/
│   ├── Extensions/
│   │   └── QueryableExtensions.cs      # Dynamic sorting via reflection
│   └── Pagination/
│       ├── PagedRequest.cs
│       ├── PagedResult.cs
│       └── RoomTypeFilterRequest.cs
├── Controllers/                        # 9 controllers — HTTP layer only
│   ├── AuditLogsController.cs
│   ├── AuthController.cs
│   ├── PaymentsController.cs
│   ├── PriceRulesController.cs
│   ├── ReservationsController.cs
│   ├── RoomsController.cs
│   ├── RoomTypesController.cs
│   ├── TagsController.cs
│   └── UserController.cs
├── Data/
│   ├── HotelDbContext.cs               # EF Core DbContext + Fluent API config
│   ├── DatabaseSeeder.cs               # Full idempotent seed on startup
│   └── RoleSeeder.cs                   # Lightweight admin-only seed
├── DTOs/                               # Request / response objects (no entities exposed)
│   ├── AuditLogDTOs/
│   ├── AuthDTOs/
│   ├── PriceDTOs/
│   ├── ReservationDTOs/
│   ├── RoomDTOs/
│   └── UserDTOs/
├── Enums/
│   ├── ReservationStatus.cs            # Pending / Confirmed / Cancelled / Completed
│   ├── RuleType.cs                     # SeasonalRange / SpecialDate
│   └── UserRole.cs                     # Admin / Moderator / Customer
├── Extensions/
│   ├── ServiceCollectionExtensions.cs  # DI registrations grouped by concern
│   └── WebApplicationExtensions.cs     # Middleware pipeline setup
├── Mappings/
│   └── MappingProfile.cs               # All AutoMapper entity ↔ DTO mappings
├── Middlewares/
│   └── GlobalExceptionMiddleware.cs    # RFC 7807 ProblemDetails for all exceptions
├── Migrations/                         # EF Core migrations — auto-applied on startup
├── Models/                             # 10 domain entities
│   ├── AuditLog.cs
│   ├── PriceRule.cs
│   ├── RefreshToken.cs
│   ├── Reservation.cs
│   ├── ReservationItem.cs
│   ├── Room.cs
│   ├── RoomPhoto.cs
│   ├── RoomType.cs
│   ├── Tag.cs
│   └── User.cs
├── Services/                           # Business logic layer
│   ├── Interfaces/                     # 9 service interfaces
│   ├── AuditLogService.cs
│   ├── AuthService.cs
│   ├── CloudinaryFileStorageService.cs
│   ├── LocalFileStorageService.cs      # Dev fallback (not registered by default)
│   ├── PriceRuleService.cs
│   ├── ReservationService.cs
│   ├── RoomService.cs
│   ├── RoomTypeService.cs
│   ├── TagService.cs
│   └── UserService.cs
├── Validators/                         # FluentValidation — grouped by domain
│   ├── AuthValidators/
│   ├── PriceRuleValidators/
│   ├── ReservationValidators/
│   └── RoomValidators/
├── appsettings.json
├── Program.cs                          # Clean entry point — delegates to extensions
└── HotelWebApplication.csproj
```

---

## 5. Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is sufficient for development)
- A Cloudinary account (or use the local file storage fallback)

### Run

```bash
# 1. Restore dependencies
dotnet restore

# 2. Set configuration (see section 6)
# Edit appsettings.json or use dotnet user-secrets

# 3. Start the application
dotnet run
```

On first startup the application automatically:

1. Applies all pending EF Core migrations
2. Seeds the database with demo data (rooms, tags, price rules, reservations)
3. Creates three demo user accounts
4. Opens Swagger UI in the browser at `http://localhost:5207/swagger`

---

## 6. Configuration

All settings live in `appsettings.json`. For local development, override sensitive values with `dotnet user-secrets`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HotelDb;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "<min-32-char-secret>",
    "Issuer": "HotelApi",
    "Audience": "HotelFrontend",
    "AccessTokenMinutes": "15",
    "RefreshTokenDays": "7"
  },
  "Cloudinary": {
    "CloudName": "<your-cloud-name>",
    "ApiKey": "<your-api-key>",
    "ApiSecret": "<your-api-secret>"
  },
  "Seed": {
    "AdminEmail": "admin@hotel.local",
    "AdminPassword": "Admin123!"
  },
  "SupportedLanguages": ["en", "ru", "az"]
}
```

| Key | Description |
|---|---|
| `Jwt:Key` | Secret used to sign JWT tokens — keep this private |
| `Jwt:AccessTokenMinutes` | Access token lifetime (default: 15 min) |
| `Jwt:RefreshTokenDays` | Refresh token lifetime (default: 7 days) |
| `Seed:AdminEmail` | Email for the auto-created admin account |
| `SupportedLanguages` | Languages accepted in tag translations |

---

## 7. Demo Accounts

Seeded automatically on first startup.

| Email | Password | Role |
|---|---|---|
| `admin@hotel.local` | `Admin123!` | Admin |
| `moderator@hotel.local` | `Moderator123!` | Moderator |
| `guest@hotel.local` | `Guest123!` | Customer |

---

## 8. Authentication & Authorization

### Token Flow

```
POST /api/auth/login
  → { accessToken, expiresAt, userId, role, ... }
  → Set-Cookie: refreshToken=<token>; HttpOnly; SameSite=Lax

Authorization: Bearer <accessToken>   ← include in all protected requests

POST /api/auth/refresh                ← when access token expires
  → old refresh token revoked (rotation)
  → new access token + new refresh cookie

POST /api/auth/logout
  → refresh token revoked, cookie cleared
```

### Role Matrix

| Permission | Admin | Moderator | Customer | Guest |
|---|---|---|---|---|
| Room Type CRUD | ✅ Full | ❌ | ❌ | ❌ |
| Room CRUD + Availability | ✅ Full | ✅ Create / Edit | ❌ | ❌ |
| Photo Management | ✅ | ✅ | ❌ | ❌ |
| Price Rule CRUD | ✅ Full | ✅ Create / Edit | ❌ | ❌ |
| View Reservations | ✅ | ✅ | Own only | ❌ |
| Edit / Cancel Reservations | ✅ | ✅ | Own (7-day rule) | ❌ |
| Create Reservation | ✅ | ✅ | ✅ | ✅ |
| User List | ✅ | ✅ | ❌ | ❌ |
| Change User Role | ✅ | ❌ | ❌ | ❌ |
| Audit Log | ✅ | ❌ | ❌ | ❌ |

### Auth Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Login — returns access token, sets refresh cookie |
| `POST` | `/api/auth/register` | Register a new Customer account |
| `POST` | `/api/auth/refresh` | Refresh access token using HttpOnly cookie |
| `POST` | `/api/auth/logout` | Revoke refresh token and clear cookie |

---

## 9. API Endpoints

### Room Types — `/api/room-types`

| Method | Route | Access | Description |
|---|---|---|---|
| `GET` | `/api/room-types` | Public | List with filters: code, price range, capacity, tags, availability dates |
| `GET` | `/api/room-types/{id}` | Public | Details including photos and tags |
| `GET` | `/api/room-types/{id}/rooms` | Public | Physical rooms belonging to this type |
| `POST` | `/api/room-types` | Admin | Create with optional photos (`multipart/form-data`) |
| `PUT` | `/api/room-types/{id}` | Admin | Update details and tags |
| `DELETE` | `/api/room-types/{id}` | Admin | Delete and remove photos from Cloudinary |
| `POST` | `/api/room-types/{id}/photos` | Admin / Mod | Upload additional photos |
| `DELETE` | `/api/room-types/photos/{photoId}` | Admin / Mod | Delete a single photo |

### Rooms — `/api/rooms`

| Method | Route | Access | Description |
|---|---|---|---|
| `GET` | `/api/rooms` | Public | List all physical room units |
| `GET` | `/api/rooms/{id}` | Public | Get room by ID |
| `POST` | `/api/rooms` | Admin / Mod | Create room (number, floor, room type) |
| `PUT` | `/api/rooms/{id}` | Admin / Mod | Update room details |
| `PATCH` | `/api/rooms/{id}/availability` | Admin / Mod | Toggle availability without deleting |
| `DELETE` | `/api/rooms/{id}` | Admin | Permanently delete |

### Reservations — `/api/reservations`

| Method | Route | Access | Description |
|---|---|---|---|
| `POST` | `/api/reservations` | Public | Create with atomic availability check → `Pending` + `HeldUntil` |
| `GET` | `/api/reservations/{id}` | Public | Get by GUID (confirmation page) |
| `GET` | `/api/reservations` | Admin / Mod | List with filters (room type, status, dates, email) |
| `PUT` | `/api/reservations/{id}` | Admin / Mod | Update dates / status / notes |
| `DELETE` | `/api/reservations/{id}` | Admin / Mod | Cancel reservation |

### Payments — `/api/payments`

| Method | Route | Access | Description |
|---|---|---|---|
| `POST` | `/api/payments/mock` | Public | `simulateSuccess: true` → `Pending` → `Confirmed`. Returns `HOLD_EXPIRED` (422) if 15 min elapsed. |

### Price Rules — `/api/price-rules`

| Method | Route | Access | Description |
|---|---|---|---|
| `GET` | `/api/price-rules` | Public | Rules for a room type (paginated) |
| `GET` | `/api/price-rules/{id}` | Public | Single rule |
| `GET` | `/api/price-rules/all` | Public | All rules with optional room type filter |
| `GET` | `/api/price-rules/period` | Public | Rules active within a date range (price calendar) |
| `GET` | `/api/price-rules/calculate` | Public | Total price with per-day breakdown |
| `GET` | `/api/price-rules/discounted` | Public | Room types with active discount ≥ 15% (homepage promos) |
| `POST` | `/api/price-rules` | Admin / Mod | Create seasonal or special-date rule |
| `PUT` | `/api/price-rules/{id}` | Admin / Mod | Update rule |
| `DELETE` | `/api/price-rules/{id}` | Admin | Delete rule |

### Tags — `/api/tags`

| Method | Route | Access | Description |
|---|---|---|---|
| `GET` | `/api/tags` | Public | List all tags with translations |
| `GET` | `/api/tags/{id}` | Public | Single tag |
| `POST` | `/api/tags` | Admin / Mod | Create (English translation required, slug auto-generated) |
| `PUT` | `/api/tags/{id}` | Admin / Mod | Update translations |
| `DELETE` | `/api/tags/{id}` | Admin | Delete and disassociate from room types |

### Users — `/api/user`

| Method | Route | Access | Description |
|---|---|---|---|
| `GET` | `/api/user/profile` | Authenticated | Get own profile |
| `PUT` | `/api/user/profile` | Authenticated | Update email, display name, phone |
| `POST` | `/api/user/change-password` | Authenticated | Change password (current password required) |
| `GET` | `/api/user/all` | Admin / Mod | List all users |
| `PUT` | `/api/user/{userId}/role` | Admin | Change user role (Admin role cannot be assigned) |
| `GET` | `/api/user/reservations` | Authenticated | Own reservation history |
| `POST` | `/api/user/reservations/{id}/cancel` | Authenticated | Self-cancel (≥ 7 days before check-in) |

### Audit Log — `/api/admin/audit-logs`

| Method | Route | Access | Description |
|---|---|---|---|
| `GET` | `/api/admin/audit-logs` | Admin | Paginated log with multi-select filters |
| `GET` | `/api/admin/audit-logs/{entityType}/{entityId}` | Admin | Full change history for a specific entity |
| `GET` | `/api/admin/audit-logs/action-types` | Admin | Distinct action types for filter dropdowns |

---

## 10. Data Models

### Core Entities

| Entity | Key Fields |
|---|---|
| `User` | `Id` (GUID), `Email`, `DisplayName`, `PhoneNumber`, `PasswordHash`, `Salt`, `SecurityStamp`, `Role`, `CreatedAt`, `LastLoginAt`, `IsActive` |
| `RoomType` | `Id`, `Code`, `Name`, `Description`, `Capacity`, `MaxOccupancyAdults`, `MaxOccupancyChildren`, `BasePrice`, `IsActive`, Tags (M2M), Photos (1-M), Rooms (1-M) |
| `Room` | `Id`, `Number`, `RoomTypeId`, `Floor`, `IsAvailable`, `CreatedAt` |
| `RoomPhoto` | `Id`, `RoomTypeId`, `Url`, `SortOrder`, `AltText` |
| `Tag` | `Id`, `Slug`, `Translations` (JSON dict — en/ru/az) |
| `PriceRule` | `Id`, `Name`, `RuleType`, `RoomTypeId` (nullable), `StartDate`, `EndDate`, `IsIncrease`, `IsPercent`, `Value`, `IsActive` |
| `Reservation` | `Id` (GUID), `RoomId`, `UserId` (nullable), `CustomerName/Email/Phone`, `StartDate`, `EndDate`, `TotalPrice`, `Status`, `HeldUntil`, `PaidAt`, `ConcurrencyToken`, `Source`, `Notes` |
| `ReservationItem` | `Id`, `ReservationId`, `Name`, `Price`, `Quantity` |
| `AuditLog` | `Id`, `ActorUserId`, `ActionType`, `EntityType`, `EntityId`, `OldValue` (JSON), `NewValue` (JSON), `Timestamp`, `IP` |
| `RefreshToken` | `Id`, `UserId`, `Token` (SHA-256 hash), `ExpiresAt`, `RevokedAt`, `CreatedAt` |

### Reservation Status Flow

```
POST /api/reservations
        │
        ▼
    [Pending]  ←── HeldUntil = Now + 15 min
        │
        ├── POST /api/payments/mock (simulateSuccess: true)
        │         ▼
        │    [Confirmed]  ←── PaidAt set
        │         │
        │         ├── EndDate passed (background job)
        │         │         ▼
        │         │    [Completed]
        │         │
        │         └── Admin / Mod cancellation
        │                   ▼
        │              [Cancelled]
        │
        ├── HoldUntil expired / payment failed
        │         ▼
        │    [Cancelled]
        │
        └── User self-cancel (≥ 7 days before check-in)
                  ▼
             [Cancelled]
```

---

## 11. Key Business Logic

### Atomic Availability Check

```
1. Open DB transaction
2. Find first available room of requested type
   WHERE no overlapping Pending/Confirmed reservations exist
3. If no room found → throw CONFLICT → return 409
4. Insert Reservation (Pending, HeldUntil = Now + 15 min)
5. Commit transaction
```

### Price Calculation

For each night of the stay:
1. Load all active rules where `RoomTypeId = requested` OR `RoomTypeId = NULL` (global), and dates overlap.
2. For each rule, compute delta: `IsPercent ? basePrice * value / 100 : value`. Negate if `IsIncrease = false`.
3. Sum all deltas: `finalPrice = basePrice + Σ(deltas)`.
4. Apply price floor: `finalPrice = Max(finalPrice, basePrice * 0.30)`.

### Background Job

`ReservationCompletionJob` runs on startup and every hour. For each `Confirmed` reservation where `EndDate <= today`, sets status to `Completed` and writes an `AutoCompleted` audit log entry.

---

## 12. Error Handling

`GlobalExceptionMiddleware` catches all exceptions and returns RFC 7807 `ProblemDetails` JSON.

| Status | Trigger |
|---|---|
| `400` | `ArgumentException`, `InvalidOperationException`, `ValidationException` (includes `errors` field) |
| `401` | `UnauthorizedAccessException` |
| `404` | `KeyNotFoundException` |
| `409` | Double-booking conflict (returned directly from controllers) |
| `422` | FluentValidation errors or `HOLD_EXPIRED` / `PAYMENT_FAILED` business codes |
| `500` | All other unhandled exceptions |

Business error codes returned in the response body:

| Code | Meaning |
|---|---|
| `CONFLICT` | Room not available for the requested dates |
| `HOLD_EXPIRED` | The 15-minute payment window elapsed |
| `PAYMENT_FAILED` | Mock payment simulation returned failure |

---

## 13. Security

- **Passwords** — hashed with BCrypt (never stored in plain text or logs)
- **Refresh tokens** — stored as SHA-256 hashes; the raw token is never persisted
- **Token rotation** — refresh token is revoked on every use; a new one is issued
- **HttpOnly cookie** — refresh token is stored in an HttpOnly, SameSite=Lax cookie to prevent XSS theft
- **Secure flag** — enabled automatically in production environment
- **CORS** — restricted to `http://localhost:5173` (React dev server); update for production
- **Audit log** — every write operation recorded with actor ID and client IP
- **Admin role protection** — the Admin role cannot be assigned via the API; only via the seeder or direct DB access
- **Sensitive data** — passwords, tokens, and secrets are never logged

---

## 14. Pagination & Sorting

All list endpoints accept `PagedRequest` query parameters:

| Parameter | Default | Description |
|---|---|---|
| `page` | `1` | Page number (1-based) |
| `pageSize` | `10` | Items per page |
| `sortBy` | — | e.g. `basePrice:asc,name:desc` |
| `search` | — | Free-text search (fields depend on endpoint) |

All list responses return:

```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10
}
```

---

## 15. Demo Seed Data

`DatabaseSeeder.SeedAsync()` is idempotent — runs on every startup, skips existing records.

**Room Types (5)**

| Code | Name | Base Price |
|---|---|---|
| `STANDARD` | Standard Room | $80 / night |
| `DELUXE` | Deluxe Room | $140 / night |
| `SUITE` | Junior Suite | $220 / night |
| `FAMILY` | Family Room | $180 / night |
| `PRESIDENTIAL` | Presidential Suite | $500 / night |

**Physical Rooms (16)** — distributed across floors 1–10 (101–602, P01).

**Tags (10)** — Wi-Fi, Sea View, Balcony, Jacuzzi, Air Conditioning, Breakfast Included, King Bed, City View, Pool Access, Pet Friendly — all with EN / RU / AZ translations.

**Price Rules (4)**

| Name | Type | Value |
|---|---|---|
| Summer Season Surcharge | Global seasonal | +25% (Jun–Aug) |
| New Year Holiday Surcharge | Global seasonal | +40% (Dec 28 – Jan 3) |
| Presidential Early Bird Discount | PRESIDENTIAL type | -15% (30–120 days ahead) |
| Standard Midweek Discount | STANDARD type | -$10 flat |

**Sample Reservations (6)** — one in each status: Confirmed (active), Confirmed (future, with add-on items), Pending (held), Cancelled, Completed.

---

## 16. Demo Scenario

End-to-end walk-through for the course defence:

```
1. Login
   POST /api/auth/login
   { "email": "admin@hotel.local", "password": "Admin123!" }

2. Browse room types
   GET /api/room-types?isActive=true

3. Check availability
   GET /api/room-types?checkIn=2026-05-01&checkOut=2026-05-05

4. Calculate price
   GET /api/price-rules/calculate?roomTypeId=1&startDate=2026-05-01&endDate=2026-05-05

5. Create reservation (no auth required)
   POST /api/reservations
   { "roomTypeId": 1, "customerName": "...", "customerEmail": "...",
     "customerPhone": "...", "startDate": "2026-05-01", "endDate": "2026-05-05",
     "guestCount": 2 }
   → response: { id, status: "Pending", heldUntil }

6. Confirm payment
   POST /api/payments/mock
   { "reservationId": "<id from step 5>", "simulateSuccess": true }
   → response: { status: "Confirmed", paidAt }

7. Test double-booking protection
   POST /api/reservations  (same roomTypeId, overlapping dates)
   → 409 Conflict

8. Test hold expiry
   POST /api/reservations, wait > 15 min (or set HeldUntil manually in DB)
   POST /api/payments/mock
   → 422 HOLD_EXPIRED

9. Moderate a reservation
   PUT /api/reservations/{id}
   { "status": "Cancelled" }

10. View audit log
    GET /api/admin/audit-logs?entityType=Reservation
```

---

*Hotel Booking API — Backend README | ASP.NET Web API 8.0*
