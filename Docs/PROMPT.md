# Blood Bank Management System: Antigravity Build Prompt

ASP.NET Core MVC (.NET 8) · C# · Entity Framework Core · SQLite · Plain HTML and CSS

## 1. Role and Mission
You are a senior .NET developer. Build a simple but complete web-based Blood Bank Management System using ASP.NET Core MVC with C# and SQLite. Every feature below must really work and save to the database. Keep the code easy for a beginner to read: simple classes, clear names, short methods, and a comment wherever the logic is not obvious.
Work in small phases. After each phase run dotnet build, fix all errors, and make sure the app starts before moving on. Do not leave TODO comments in the code.

## 2. Project Overview
Problem: Managing blood donations and stock by hand causes wrong records, slow searches for a blood group, and messy request handling.
Solution: One web application where:
- Users register and log in as a Donor or a Patient.
- Donors keep a profile and see their donation history.
- Anyone can search which blood groups are available. Patients submit blood requests and track their status.
- Administrators manage donors, patients, blood groups, stock, donations, requests and transactions from an admin dashboard.
- Blood stock updates automatically when a donation is completed or blood is issued.

## 3. Technology Stack and Simplicity Rules
- Framework: ASP.NET Core MVC, .NET 8, C#
- ORM: Entity Framework Core 8, Code-First with migrations
- Database: SQLite (one file, bloodbank.db, no installation needed). Connection string in appsettings.json.
- Packages: Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.Design, Microsoft.AspNetCore.Identity.EntityFrameworkCore
- Login: ASP.NET Core Identity with three roles: Admin, Donor, Patient
- Frontend: Razor views with plain HTML and one hand-written CSS file (wwwroot/css/site.css)
- Do not use: Bootstrap, Chart.js or any chart, DataTables, jQuery plugins, JavaScript frameworks, AutoMapper, repository pattern, Areas, background services, email or SMS, unit-test projects. The dashboard shows only number cards and small tables.
- Keep it simple:
  - Controllers use ApplicationDbContext directly for normal create, read, update and list work.
  - Only one service class, StockService, holds the inventory logic, so stock is always changed in one place.
  - Use ViewModels only where a form differs from the entity. Use async/await.
  - The database is created and seeded automatically on startup (Database.Migrate()), so no manual database step is needed.
  - Because all data access goes through ApplicationDbContext, moving to SQL Server later means changing one NuGet package, one line in Program.cs and the connection string.

## 4. Project Structure
BloodBankSystem/
- Controllers/
  - HomeController, AccountController, BloodSearchController, DonorController, PatientController
  - Admin/ DashboardController, DonorsController, PatientsController, BloodGroupsController, StockController, DonationsController, RequestsController, TransactionsController
- Data/ ApplicationDbContext.cs, DbSeeder.cs
- Models/ entity classes and Enums.cs
- ViewModels/
- Services/ StockService.cs
- Views/ (Shared/_Layout.cshtml plus one folder per controller)
- wwwroot/css/site.css
- Docs/README.md

Admin controllers use the Admin/... route prefix and are restricted to the Admin role.

## 5. Users and Permissions
- Guest: View home page, search available blood, register and log in.
- Donor: View home, search blood, edit own profile, schedule donation, view own donation history, cancel pending donation.
- Patient: View home, search blood, edit own profile, submit blood request, view and cancel own pending request.
- Admin: All capabilities, manage donors, patients, groups, stock, donations, requests, transactions.
The registration form asks the user to choose Donor or Patient. Admin accounts are never created through public registration; the first admin is seeded.

## 6. Database Tables
- ApplicationUser (extends IdentityUser): FullName, Phone, City, DateOfBirth, Gender.
- BloodGroup: Id, Name (8 seeded groups: A+, A-, B+, B-, AB+, AB-, O+, O-).
- Donor: Id, UserId, BloodGroupId, WeightKg, LastDonationDate, IsActive. One per donor account.
- Patient: Id, UserId, BloodGroupId, HospitalName, IsActive. One per patient account.
- Donation: Id, DonorId, BloodGroupId, Units, DonationDate, Status, Remarks. Status: Scheduled, Completed, Rejected, Cancelled.
- BloodStock: Id, BloodGroupId (unique), AvailableUnits, MinimumLevel (default 10), LastUpdated. One row per blood group.
- BloodRequest: Id, PatientId, BloodGroupId, UnitsRequired, Urgency, RequiredByDate, Reason, Status, AdminRemarks, CreatedAt, ProcessedAt. Urgency: Normal, Urgent, Critical. Status: Pending, Approved, Rejected, Issued, Cancelled.
- BloodTransaction: Id, Type, BloodGroupId, Units, BalanceAfter, DonationId (nullable), BloodRequestId (nullable), TransactionDate, Remarks. Type: DonationIn, IssueOut, Adjustment. Never edited or deleted.

Use C# enums for all statuses. Stock is a simple count per blood group; there is no tracking of individual blood bags or expiry dates. Use required and max-length constraints, and restrict deletes. Donors and patients are deactivated (IsActive = false), never deleted.

## 7. Business Rules
1. Donor eligibility: age 18 or more, weight 50 kg or more, and at least 90 days since the last donation. Keep these three numbers in appsettings.json. If a donor is not eligible, show the reason and the next eligible date, and block scheduling.
2. Request flow: Pending, then Approved, then Issued. A request can also be Rejected (admin remarks required) or Cancelled (by the patient, only while Pending).
3. Approve only if available stock for that group is at least the units requested. Issue checks stock again before reducing it.
4. Stock never goes below zero.
5. Blood groups must match exactly (an A+ request is served from A+ stock).
6. Request queue order: Critical first, then Urgent, then Normal, oldest first within each.
7. A group is Low when AvailableUnits is at or below its MinimumLevel; show it in red. Show Out of stock when it is 0.
8. Transaction rows are never edited or deleted. A manual stock correction is saved as an Adjustment with a required reason.

## 8. Pages and Features
### 8.1 Public and Login
- Home: short intro and one card per blood group showing available units and status (Available, Low, Out of stock).
- Search Blood: pick a blood group and see its availability. Never show personal donor details.
- Register (with Donor or Patient choice and the fields for that role), Login, Logout.

### 8.2 Donor
- Dashboard: eligibility message, total donations, last donation date.
- Edit profile (blood group, weight, phone, city).
- Schedule a donation (date must not be in the past; blocked if not eligible) and cancel a scheduled one.
- Donation history table.

### 8.3 Patient
- Dashboard: counts of Pending, Approved, Issued and Rejected requests.
- Edit profile (blood group, hospital name, phone, city).
- Create blood request (group, units, urgency, required-by date, reason). Show current availability of the chosen group on the form page.
- My Requests table with status and admin remarks; cancel while Pending.

### 8.4 Admin
- Dashboard: number cards (total donors, total patients, total available units, pending requests, urgent and critical pending requests, donations today), a list of low-stock groups, and the 5 latest requests. No charts.
- Donors and Patients: list with search by name and filter by blood group; view details; add, edit, activate or deactivate.
- Blood Groups: list and edit names.
- Stock: table of all groups (available units, minimum level, status); edit the minimum level; manual adjustment with a reason.
- Donations: list with status filter; record a walk-in donation; for a scheduled donation choose Complete (enter units), Reject (reason) or Cancel.
- Requests: queue in priority order; details page with live stock for that group; buttons Approve, Reject, Issue.
- Transactions: list filtered by blood group, type and date range, newest first, with Previous and Next paging.

## 9. Inventory Update Logic
Put this in StockService and call it from the controllers. Each action must save everything together in one SaveChanges call or one database transaction, so stock and ledger can never disagree.
When the admin completes a donation:
1. Check the donation is Scheduled (or a new walk-in) and the donor is eligible.
2. Set the donation to Completed and save the units.
3. Add the units to AvailableUnits of that blood group and update LastUpdated.
4. Set the donor's LastDonationDate.
5. Add a DonationIn row to BloodTransaction with the new BalanceAfter.

When the admin issues a request:
1. Check the request is Approved and stock is at least the units requested; otherwise show a friendly error and change nothing.
2. Subtract the units from AvailableUnits.
3. Set the request to Issued and fill ProcessedAt.
4. Add an IssueOut row to BloodTransaction with the new BalanceAfter.

## 10. User Interface
- Write all styling by hand in site.css: no CSS framework. Use a clean medical look with red (#C62828) as the main color, white background, light gray cards, and a readable font.
- One shared layout with a top navigation bar. The menu changes by role (guest, donor, patient, admin).
- Number cards and blood group cards use CSS grid. Tables are plain HTML tables styled in CSS.
- Status labels are small colored text badges made with CSS classes (Pending yellow, Approved blue, Issued green, Rejected red, Cancelled gray; Critical dark red, Urgent orange).
- Success and error messages use TempData and show at the top of the page.
- Simple responsive layout using one media query for small screens.

## 11. Security and Validation
- Use the Identity default password rules. Protect pages with the Authorize attribute and the right role.
- Use form tag helpers so the anti-forgery token is added to every POST form.
- Validate with DataAnnotations on the models and ViewModels and show messages on every form (server-side validation is enough).
- A donor or patient can only open their own records: check the owner in the controller, not only the role.
- Show a simple friendly error page; do not show stack traces.

## 12. Seed Data
Seed on startup if the tables are empty:
- Roles Admin, Donor, Patient.
- Admin user admin@bloodbank.com with password Admin@123 (mention in the README that it must be changed).
- The 8 blood groups, each with a BloodStock row (MinimumLevel 10).
- A few demo records: 3 donors, 2 patients, some completed donations (with matching stock and transactions), and 2 requests including one Critical. Keep demo data behind a setting in appsettings.json so it can be switched off.

## 13. Phased Plan
1. Setup: project, packages, entities, ApplicationDbContext, SQLite connection, first migration, seeder.
2. Login and layout: Identity, registration with role choice, shared layout, role-based menu, site.css.
3. Admin master data: blood groups, donors, patients.
4. Donations, stock and transactions: StockService, donation completion, stock page, ledger.
5. Requests: patient request pages and admin approve, reject and issue.
6. Public pages and dashboards: home, blood search, donor and patient dashboards, admin dashboard, then final cleanup.

## 14. Deliverables
- Complete source code that builds and runs with dotnet run, including migrations.
- Docs/README.md covering: requirements (.NET 8 SDK), how to run, the admin login, the list of pages for each role, and the steps to switch to SQL Server later.

## 15. Acceptance Criteria
1. A new donor and a new patient can register and log in and see only their own menu and data.
2. An underweight donor, or one who donated less than 90 days ago, cannot schedule a donation and sees the reason.
3. Admin completes a 2-unit O+ donation: O+ stock goes up by 2, a DonationIn transaction is saved, and the donor's last donation date updates.
4. A patient requests 3 units of O+ as Urgent. It appears at the top of the admin queue. Admin approves and issues it: O+ stock goes down by 3 and an IssueOut transaction is saved.
5. A request for more units than in stock cannot be approved or issued, and stock never becomes negative.
6. Rejecting a request without remarks is blocked.
7. The Search Blood page shows the same numbers as the admin stock page.
8. A patient cannot open another patient's request by changing the URL, and a non-admin cannot open any /Admin page.
9. dotnet build shows no errors and no Bootstrap, Chart.js or other external UI library is used.
