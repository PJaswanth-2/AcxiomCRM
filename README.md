# AcxiomCRM - Enterprise Customer Relationship Management System

AcxiomCRM is a complete, production-grade, web-based Customer Relationship Management (CRM) application built strictly using **ASP.NET Core MVC**, **Entity Framework Core**, **ASP.NET Core Identity**, **Bootstrap 5**, and **Chart.js**.

---

## 🚀 Quick Start Instructions

### Prerequisites
- [.NET 8+ / 9 / 11 SDK](https://dotnet.microsoft.com/) installed.

### Steps to Run
1. Open a terminal in the project directory:
   ```bash
   cd AcxiomCRM
   ```
2. Restore NuGet dependencies and build:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Run the application:
   ```bash
   dotnet run
   ```
4. Open your web browser and navigate to:
   - **Web UI:** `http://localhost:5062`
   - **Swagger REST API Docs:** `http://localhost:5062/swagger`

> **Note on Database:** The database and all seed data (Roles, Demo Users, Customers, Leads, Opportunities, Follow-Ups, Activities, Audit Logs) are automatically provisioned and populated on first run!

---

## 🔑 Demo Login Credentials

The application comes pre-seeded with 3 primary demo accounts representing each authorization level:

| Role | Email Address | Password | Access Level |
|---|---|---|---|
| **Admin** | `admin@acxiomcrm.com` | `Admin@123456` | Full CRM administration, user/role management, audit logs, all records & reports. |
| **Manager** | `manager@acxiomcrm.com` | `Manager@123456` | Team pipeline monitoring, management reports, team customers/leads. |
| **Sales Executive** | `sales@acxiomcrm.com` | `Sales@123456` | Assigned customers, leads, opportunities, follow-ups, personal dashboard. |

---

## 🏗️ Clean Layered Architecture

AcxiomCRM strictly adheres to clean layered architecture separation:

```
AcxiomCRM/
│
├── Controllers/         # Thin MVC Controllers coordinating HTTP requests & views
├── Api/                 # Secure REST API Controllers exposing JSON endpoints
├── Data/                # EF Core ApplicationDbContext & DbInitializer seed logic
├── Models/              # Domain Entities (Customer, Lead, Opportunity, FollowUp, Activity, AuditLog, ApplicationUser)
├── ViewModels/          # Strictly-typed ViewModels for forms and UI views
├── DTOs/                # Data Transfer Objects for API contracts (no EF entity leakage)
├── Services/            # Business Logic Layer (Interfaces & Implementations)
├── Authorization/       # User scope & role authorization helpers
├── Middleware/          # Global Exception Handling & Security Middleware
├── Views/               # Razor Views grouped by module (Bootstrap 5 corporate UI)
├── wwwroot/             # Static web assets (CSS, JS, images)
├── appsettings.json     # System configuration & DB connection strings
├── Program.cs           # Dependency Injection & Pipeline setup
└── README.md            # Comprehensive project documentation
```

---

## 🛠️ Technology Stack

- **Framework:** ASP.NET Core MVC (C#)
- **Security & Authentication:** ASP.NET Core Identity (PBKDF2 Password Hashing, Account Lockout after 5 attempts, Secure Cookies, CSRF Anti-Forgery)
- **Data Access:** Entity Framework Core (SQLite / SQL Server support)
- **REST API:** ASP.NET Core ApiControllers + Swashbuckle Swagger OpenAPI
- **Frontend UI:** Razor Views + Bootstrap 5 + Bootstrap Icons
- **Analytics & Charts:** Chart.js 4.x
- **Validation:** Dual Client-side (jQuery Unobtrusive) + Server-side DataAnnotations & Business Logic Validation

---

## 🛡️ Business Validation Rules

The application strictly enforces mandatory business rules both client-side and server-side:

1. **Customer Rules:**
   - Email is mandatory, must be valid format, and **must be unique across all clients**.
   - Phone is mandatory, valid phone format, and **must be unique**.
   - Customer Name is mandatory.

2. **Lead Rules:**
   - Lead Name, Email, Phone are mandatory.
   - Expected Deal Value must be $\ge 0$.
   - **Lead Conversion:** Qualified leads convert to a Customer Account + Opportunity, update lead status to `Converted`, and log event in AuditLog.

3. **Opportunity Rules:**
   - Amount **must be greater than 0** for active deals (Error: `"Opportunity Amount must be greater than 0."`).
   - Probability **must be 0 to 100** (Error: `"Probability must be between 0 and 100."`).
   - Expected Close Date **cannot be in the past** for active deals (Error: `"Expected Close Date cannot be in the past."`).
   - **Weighted Pipeline Calculation:** $\text{Weighted Amount} = \text{Amount} \times \frac{\text{Probability}}{100}$.

4. **Follow-Up Rules:**
   - Follow-up date **cannot be earlier than today** for new/planned items (Error: `"Follow-up date cannot be earlier than today."`).

---

## 🔌 REST API Endpoints Specification

All protected API endpoints require authentication and enforce role scoping.

| Method | Endpoint | Description | Scope |
|---|---|---|---|
| `POST` | `/api/auth/login` | Authenticate API user | Public / Rate-limited |
| `POST` | `/api/auth/logout` | Terminate session | Authenticated |
| `GET` | `/api/customers` | Get paginated & filtered customers list | Authorized |
| `GET` | `/api/customers/{id}` | Get customer details by ID | Authorized |
| `POST` | `/api/customers` | Create new customer | Authorized |
| `PUT` | `/api/customers/{id}` | Update customer | Authorized |
| `DELETE` | `/api/customers/{id}` | Deactivate customer | Authorized |
| `GET` | `/api/leads` | Get leads list | Authorized |
| `POST` | `/api/leads` | Create new lead | Authorized |
| `GET` | `/api/opportunities` | Get opportunities list | Authorized |
| `POST` | `/api/opportunities` | Create opportunity | Authorized |
| `GET` | `/api/followups` | Get follow-ups list | Authorized |
| `POST` | `/api/followups` | Schedule follow-up | Authorized |
| `GET` | `/api/reports/pipeline` | Get stage-wise pipeline report data | Authorized |

---

## 📊 Dashboard & Reports

### Role-Aware Dashboard
- **Admin:** Sees global statistics across all users and departments.
- **Manager:** Sees team pipeline and performance metrics.
- **Sales Executive:** Sees assigned customers, leads, opportunities, and upcoming follow-ups.

### Dynamic Chart.js Analytics
1. **Lead Status Breakdown Chart** (New, Contacted, Qualified, Lost, Converted)
2. **Opportunity Pipeline Bar Chart** (Qualification, Proposal, Negotiation, Won, Lost)
3. **Monthly Closed Won Sales Line Chart** (Monthly won outcome totals)

### Module Reports & Export
- Customer Report, Lead Report, Opportunity Report, Follow-Up Report, Pipeline Report, Conversion Rate Report, User Activity Report, Audit Report.
- **CSV Export:** Available on major report modules.

---

## 🔒 Security & Audit Implementation

- **No Plaintext Passwords:** Identity PBKDF2 adaptive password hashing enabled.
- **Account Lockout:** Automatically locks user out for 15 minutes after 5 failed login attempts and logs an `Account Lockout` audit event.
- **Append-Only Audit Logs:** Captures Logins, Logouts, Lockouts, User Creation, Role Modifications, Customer/Lead/Opportunity CRUD, Lead Conversions, and Opportunity Stage changes with IP address and timestamp.
- **CSRF Protection:** State-changing POST forms use `[ValidateAntiForgeryToken]`.
- **SQL Injection Prevention:** Parametrized EF Core queries.
- **Global Error Handling:** Friendly error pages without exposing SQL errors, stack traces, or internal secrets.

---

## ✅ Acceptance Checklist

- [x] Unauthenticated users redirected to Login.
- [x] Login with `admin@acxiomcrm.com` / `Admin@123456` redirects to Dashboard.
- [x] Account Lockout triggers after 5 failed login attempts.
- [x] Customer email/phone uniqueness enforced.
- [x] Opportunity amount $\le 0$, probability $>100$, close date in past rejected with proper messages.
- [x] Follow-up date in past rejected with proper message.
- [x] SalesExecutive scope restricted to assigned data.
- [x] Lead conversion creates Customer + Opportunity + Audit entry.
- [x] REST APIs functional and documented via Swagger.
- [x] Audit logs record all sensitive actions.
