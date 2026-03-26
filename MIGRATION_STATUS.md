# COEPD .NET MVC4 Migration (Started)

## Implemented in this starter
- ASP.NET MVC4-style project scaffold (`.NET Framework 4.8` target).
- SQL Server + EF6 context/models for `leads` and `staff`.
- URL parity routes for key endpoints:
  - `/`, `/privacy`, `/health`
  - `/staff`, `/admin`, `/dashboard`, `/admin/dashboard`
  - `/api/leads`, `/lead`, `/contact`, `/enquiry`
  - `/api/login`, `/api/admin/login`, `/api/staff/login`, `/logout`, `/auth/me`
- Existing HTML templates copied to `Content/templates`.
- Existing static assets copied to `Content/static`.

## Pending for exact parity
- Full chatbot engine + session persistence (`/chat`) port.
- Admin APIs parity (`lead-growth`, `source-breakdown`, CSV export, staff CRUD endpoints).
- CSRF middleware parity and cookie strategy parity.
- Razor conversion of templates (if you want dynamic server rendering over static HTML serving).

## Setup
1. Open `Coepd.Web.csproj` in Visual Studio (with .NET Framework 4.8 workloads).
2. Restore NuGet packages.
3. Run `database/001_init.sql` in SQL Server (SSMS).
4. Update `Web.config` connection string `CoepdDb`.
5. Run on IIS Express.
