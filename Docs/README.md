# Blood Bank Management System

A web-based Blood Bank Management System built with **ASP.NET Core MVC**, **C#**, **Entity Framework Core**, **SQLite**, **ASP.NET Core Identity**, and plain HTML/CSS (no frontend frameworks or external libraries).

---

## 1. System Requirements

- **.NET SDK**: .NET 8.0 SDK or newer (e.g. .NET 10 SDK)
- **Database**: SQLite (embedded, no server setup or installation needed; file `bloodbank.db` is created automatically on startup)
- **OS**: Windows, macOS, or Linux

---

## 2. How to Run

1. Clone or open the repository directory:
   ```bash
   cd project.net
   ```

2. Build and restore packages:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Open your browser and navigate to the printed listening address (e.g., `http://localhost:5000` or `http://localhost:5175`).

> [!NOTE]
> Database creation, migrations, and seed data are applied automatically on startup (`Database.Migrate()` + `DbSeeder.SeedAsync`). No manual database installation is needed.

---

## 3. Seeded Accounts & Credentials

### Administrator Account
- **Email**: `admin@bloodbank.com`
- **Password**: `Admin@123`

> [!WARNING]
> The seeded admin account is provided for demonstration and initial evaluation. For production environments, this password must be changed immediately.

### Demo Accounts (enabled via `"BloodBank:SeedDemoData": true` in `appsettings.json`)
- **Donor 1 (Alice Smith, O+, eligible)**:
  - Email: `donor1@bloodbank.com`
  - Password: `Donor@123`
- **Donor 2 (Bob Jones, A+, ineligible - donated 20 days ago)**:
  - Email: `donor2@bloodbank.com`
  - Password: `Donor@123`
- **Donor 3 (Charlie Brown, B-, ineligible - underweight 47 kg)**:
  - Email: `donor3@bloodbank.com`
  - Password: `Donor@123`
- **Patient 1 (David Wilson, O+, St. Jude Hospital)**:
  - Email: `patient1@bloodbank.com`
  - Password: `Patient@123`
- **Patient 2 (Emma Davis, AB-, Metropolitan Medical Center)**:
  - Email: `patient2@bloodbank.com`
  - Password: `Patient@123`

---

## 4. Pages by Role

### 🌐 Guest (Unauthenticated)
- **Home** (`/`): Landing banner and live stock availability cards for all 8 blood groups (Available, Low, Out of Stock).
- **Search Blood** (`/BloodSearch`): Filter live inventory by blood group without exposing private donor data.
- **Register** (`/Account/Register`): Register as a Donor (with weight) or Patient (with hospital name).
- **Login** (`/Account/Login`): Secure login using ASP.NET Core Identity cookies.

### 🩸 Donor Portal (`[Authorize(Roles = "Donor")]`)
- **Donor Dashboard** (`/Donor/Dashboard`): Displays real-time donation eligibility status (age $\ge$ 18, weight $\ge$ 50kg, 90-day gap), next eligible date, total donations count, and appointment scheduling form.
- **My Donations** (`/Donor/MyDonations`): Table of personal donation history and ability to cancel pending scheduled appointments.
- **Profile** (`/Donor/Profile`): Update weight, blood group, contact phone, and city.

### 🏥 Patient Portal (`[Authorize(Roles = "Patient")]`)
- **Patient Dashboard** (`/Patient/Dashboard`): Counters for Pending, Approved, Issued, and Rejected requests, along with facility profile summary.
- **New Request** (`/Patient/NewRequest`): Submit a blood request (group, units, urgency, required-by date, reason) with real-time stock availability table for reference.
- **My Requests** (`/Patient/MyRequests`): View submitted requests, review administrative remarks, and cancel pending requests. (Enforces strict ownership check).
- **Profile** (`/Patient/Profile`): Edit hospital facility name, blood group, phone, and city.

### 🛡️ Admin Dashboard & Management (`[Authorize(Roles = "Admin")]`, Route: `/Admin/...`)
- **Admin Dashboard** (`/Admin/Dashboard`): Operational number cards (Total Donors, Total Patients, Available Units, Pending Requests, Urgent/Critical Requests, Donations Today), list of low-stock blood groups, and top 5 recent requests.
- **Stock Management** (`/Admin/Stock`): Full table of all 8 blood groups (units, status, minimum thresholds), threshold adjustment, and manual count correction with mandatory audit reason.
- **Donations** (`/Admin/Donations`): List with status filter, record walk-in donations (with eligibility validation), complete scheduled donations with units, reject with remarks, or cancel.
- **Requests Management** (`/Admin/Requests`): View all submitted blood requests (newest first); details view with live stock check, Approve, Issue (atomic stock deduction), and Reject (mandatory remarks).
- **Transaction Ledger** (`/Admin/Transactions`): Immutable audit ledger of all inventory transactions (`DonationIn`, `IssueOut`, `Adjustment`) showing exact timestamp and `BalanceAfter`.
- **Donor Directory** (`/Admin/Donors`): Search by name/email/phone, filter by blood group, view health profile/donation history, and activate/deactivate accounts.
- **Patient Directory** (`/Admin/Patients`): Search by name/hospital/phone, filter by blood group, view facility profile/request history, and activate/deactivate accounts.
- **Blood Groups** (`/Admin/BloodGroups`): List and edit blood group nomenclature.

---

## 5. How to Switch to Microsoft SQL Server Later

Because the application uses EF Core Code-First with all data access channeled through `ApplicationDbContext`, switching to Microsoft SQL Server requires only **one NuGet package, one line of code, and a connection string**:

1. **Install SQL Server EF Core Provider**:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   ```

2. **Update Connection String** in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=BloodBankDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   }
   ```

3. **Update `Program.cs`**:
   Replace:
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseSqlite(connectionString));
   ```
   With:
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseSqlServer(connectionString));
   ```

4. **Re-generate Migration** (optional if starting clean):
   ```bash
   dotnet ef migrations add InitialSqlServer
   dotnet ef database update
   ```
