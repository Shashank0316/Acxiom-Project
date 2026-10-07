# AcxiomCRM

AcxiomCRM is a comprehensive Customer Relationship Management (CRM) application built using ASP.NET Core MVC and Entity Framework Core. It is designed to manage the full lifecycle of customer acquisition, from Lead generation to Opportunity management, with robust security and auditing built-in.

## Features
- **Dashboard & Reporting:** Interactive Chart.js visualizations for pipeline health and lead status.
- **Role-Based Access Control:** Distinct views and access permissions for Admins, Managers, and Sales Executives.
- **Lead-to-Customer Conversion:** Transaction-safe workflow to convert qualified leads into customers and opportunities.
- **Secure REST API:** Complete programmatic access to CRM entities secured by JWT/Cookie authentication and utilizing DTOs.
- **Audit Logging:** Tamper-evident logging of all record modifications.

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later

### Running the Application Locally
1. Clone the repository and navigate to the project root.
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build the solution:
   ```bash
   dotnet build
   ```
4. The application uses a local SQLite database by default (`app.db`). The database will be automatically created and seeded with default roles and users upon the first run.
5. Run the application:
   ```bash
   dotnet run --project AcxiomCRM
   ```
6. Open a web browser and navigate to `https://localhost:5001` (or the port specified in the terminal output).

### Default Users
- **Admin:** admin@acxiomcrm.com / Admin@123
- **Manager:** manager@acxiomcrm.com / Manager@123
- **Sales Executive:** sales@acxiomcrm.com / Sales@123

## Architecture
- **Framework:** ASP.NET Core 8 MVC
- **Database Access:** Entity Framework Core (SQLite default)
- **Security:** ASP.NET Core Identity (Role-based)
- **Frontend:** HTML5, Bootstrap 5, Chart.js

## Testing
To run the automated integration and unit tests:
```bash
dotnet test
```
The test suite ensures transactional integrity during conversions and validates all Role-Based Authorization paths.
