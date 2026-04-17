# TaskManagement.API

Task Management mini-project built with ASP.NET Core Web API, EF Core, and PostgreSQL.

## Features
- JWT authentication (`register`, `login`)
- Role-based authorization (`User`, `Admin`, `Manager`)
- Task CRUD
- Task business flow (`Pending -> InProgress -> Completed -> Approved/Rejected`)
- Approval logs tracking
- Standard API response wrapper
- Centralized error handling middleware
- Request logging middleware

## Tech Stack
- .NET 10 Web API
- Entity Framework Core
- PostgreSQL
- Swashbuckle (Swagger)
- BCrypt password hashing

## Seed Accounts
Automatically seeded at startup:
- `user` / `User@123!` (role: `User`)
- `admin` / `Admin@123!` (role: `Admin`)
- `manager` / `Manager@123!` (role: `Manager`)

## Run
```bash
dotnet restore
dotnet build
dotnet run --launch-profile https
```

API base URL:
- `https://localhost:7187`

Swagger:
- `https://localhost:7187/swagger`

## Main Endpoints
### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`

### Tasks (Auth required)
- `GET /api/tasks`
- `GET /api/tasks/{id}`
- `POST /api/tasks`
- `PUT /api/tasks/{id}`
- `DELETE /api/tasks/{id}`
- `PATCH /api/tasks/{id}/start`
- `PATCH /api/tasks/{id}/complete`
- `PATCH /api/tasks/{id}/approve` (Admin/Manager)
- `PATCH /api/tasks/{id}/reject` (Admin/Manager)
- `GET /api/tasks/{id}/approvals`

## Notes
- Migration is auto-applied on startup.
- Use `TaskManagement.API.http` for quick local request testing.
