# HRMS — Proof of Concept

A small Human Resource Management System API built with ASP.NET Core 8, showing
authentication, role-based access control, CRUD, and Swagger documentation on
top of a local PostgreSQL database.

## 1. Architecture

Clean Architecture, 4 projects, each only depending on the one(s) inside it:

```
HRMS.API            -> Controllers, Program.cs, Swagger, JWT wiring, middleware
   depends on
HRMS.Infrastructure  -> EF Core DbContext, Identity, Dapper repo, JWT token service
   depends on
HRMS.Application     -> DTOs, service interfaces + implementations, FluentValidation
   depends on
HRMS.Domain          -> Entities (ApplicationUser, Organization, Employee), constants
```

Why this split: Domain and Application have no dependency on ASP.NET Core or EF
Core internals, so business rules (who can edit which employee, etc.) live in
one place and are easy to unit test. Infrastructure is the only project that
knows about PostgreSQL/Npgsql/Dapper — swap the database later without
touching Application or API.

### Folder map (where to find / change things)

| Want to change...                          | Go to |
|---------------------------------------------|-------|
| A field on Employee/Organization/User        | `HRMS.Domain/Entities/*.cs` |
| Request/response shape for an endpoint        | `HRMS.Application/DTOs/**` |
| Business/authorization rule (who can do what) | `HRMS.Application/Services/*.cs` |
| Input validation rules                        | `HRMS.Application/Validators/*.cs` |
| Database column mapping / constraints         | `HRMS.Infrastructure/Persistence/Configurations/*.cs` |
| JWT claims / expiry                           | `HRMS.Infrastructure/Identity/TokenService.cs`, `appsettings.json` |
| A new endpoint                                | `HRMS.API/Controllers/*.cs` |
| Swagger / auth pipeline setup                 | `HRMS.API/Program.cs` |

## 2. Technologies used

- **ASP.NET Core 8 Web API**
- **ASP.NET Core Identity** — registration, password hashing, lockout
- **JWT Bearer authentication** — `System.IdentityModel.Tokens.Jwt`
- **PostgreSQL** (local) via `Npgsql.EntityFrameworkCore.PostgreSQL`
- **EF Core 8** — Identity storage + all writes (Create/Update/Delete), migrations
- **Dapper** — read-only employee queries (`EmployeeRepository`), shown alongside
  EF Core per the "Dapper or EF Core, or both" requirement
- **FluentValidation** — request DTO validation
- **Swashbuckle (Swagger / OpenAPI)** — API documentation + a Bearer-token login box

## 3. Domain flow

1. **Register** (`POST /api/auth/register`) — creates a user with no
   organization and no role yet.
2. **Login** (`POST /api/auth/login`) — returns a JWT. At this point the token
   has no `organizationId` claim and no role claim.
3. **Create Organization** (`POST /api/organizations`) — the logged-in user
   supplies `Name` + `Address`. They are attached to the new organization and
   **automatically promoted to Admin**. The response includes a **new JWT** —
   use that one from here on, since the old token's claims are now stale.
4. **Admin adds HR users** (`POST /api/admin/hr-users`) — creates a user under
   the Admin's organization with the `HR` role.
5. **Admin removes HR users** (`DELETE /api/admin/hr-users/{id}`) — soft-removes
   (deactivates + locks out) the HR account rather than hard-deleting it, since
   employees the HR user created still reference their id as `CreatedByUserId`.
6. **HR logs in and manages employees** (`POST/PUT/DELETE /api/employees`) —
   an HR user can create employees, and can only edit/delete the ones **they
   personally created**.
7. **Viewing employees** (`GET /api/employees`, `GET /api/employees/{id}`) —
   Admin sees every employee in the organization (including ones other HR
   users created); HR sees only the employees they created themselves. This
   branching lives in `EmployeeService.GetAllAsync()` / `GetByIdAsync()`.

## 4. Local setup

### Prerequisites
- .NET 8 SDK
- PostgreSQL running locally (e.g. `localhost:5432`)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef` (skip if already installed)

### Steps

```bash
# 1. From the solution root, restore packages
cd HRMS
dotnet restore

# 2. Create the database (adjust user/password/db name to match appsettings.json,
#    or just update appsettings.Development.json to match your local Postgres)
#    e.g. using psql:
psql -U postgres -c "CREATE DATABASE hrms_db_dev;"

# 3. Add the initial migration (run from the solution root)
dotnet ef migrations add InitialCreate \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API

# 4. Apply it (Program.cs also calls Database.MigrateAsync() on startup,
#    so this step is optional but useful to sanity-check independently)
dotnet ef database update \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API

# 5. Run the API
dotnet run --project src/HRMS.API
```

The console will print the listening URL (see `Properties/launchSettings.json`
— `http://localhost:5080` by default). Swagger UI opens automatically at
`/swagger`.

Update the connection string in `src/HRMS.API/appsettings.Development.json`
(or `appsettings.json`) to match your local PostgreSQL credentials before step 3.

## 5. Testing the flow through Swagger

Swagger UI is at `http://localhost:5080/swagger`. There's an **Authorize**
button (padlock icon, top right) — paste a raw JWT there (no `Bearer ` prefix,
Swagger adds it) to authenticate subsequent calls.

Recommended order to exercise the whole flow:

1. `POST /api/auth/register` — create your first user (this will become Admin).
2. `POST /api/auth/login` — copy the `token` from the response.
3. Click **Authorize** in Swagger, paste the token.
4. `POST /api/organizations` — create your org. Copy the **new** `token` from
   this response and re-Authorize with it (roles changed).
5. `POST /api/admin/hr-users` — create an HR user (note the email/password you use).
6. Open a second Swagger session (or just re-Authorize) and
   `POST /api/auth/login` as the HR user to get their token; Authorize with it.
7. `POST /api/employees` as HR — create a couple of employees.
8. `GET /api/employees` as HR — confirms you only see your own.
9. Re-Authorize as Admin and `GET /api/employees` — confirms Admin sees all
   employees in the org, including the ones HR created.
10. Try `PUT`/`DELETE /api/employees/{id}` as Admin — the API returns 403,
    since only the creating HR user may modify their own records.
11. `DELETE /api/admin/hr-users/{id}` as Admin — deactivates the HR account;
    subsequent login attempts for that HR user will fail.

## 6. Notes / things intentionally kept simple for a POC

- Passwords: Identity's default hasher (PBKDF2) — no changes needed.
- No refresh tokens — access token expiry is 60 minutes (`JwtSettings:ExpiryMinutes`).
- No email confirmation flow.
- CORS is wide open (`AllowAnyOrigin`) for local testing only.
- HR user removal is a soft-delete (deactivate + lockout), not a hard delete,
  to keep employee history intact.
