# CCCInventory — Development Plan

**Project:** Custom Cake & Cookie (CCC) Bakery Order Management System
**Last Updated:** 2026-02-21
**Status:** Active Development — Phase 4.5 Complete, Phase 8/7 Next

---

## Overview

This system is a digital replacement for paper order forms used in a bakery. The long-term goal is a fully deployed, remotely accessible web application running on a shop PC, with an Angular frontend and a .NET backend. The MVP must be usable in-shop before anything else matters.

---

## Current State

| Layer | Technology | Version | Notes |
|---|---|---|---|
| Backend | ASP.NET Core | 10.0 | Functional |
| ORM | Entity Framework Core | 10.0.0 | Migrations tracked |
| Database | SQLite | Local file (`orders.db`) | Zero-install, single-machine |
| Frontend | Angular | 19.x | Standalone components |
| UI | Bootstrap + ng-bootstrap | 5.3.3 / 18.0 | Working |
| Auth | None | — | Not yet implemented |

**What works:** Basic order CRUD, order search, form-based entry, soft deletes, multi-product support (cakes, cupcakes, cookies, pupcakes).

**What is missing or broken:**
- No authentication
- No calendar view
- No bake sheet
- No attachment handling (field exists on model, not wired up)
- No Google Calendar or Google Maps integration
- Hardcoded localhost API URL in frontend service
- Single-machine deployment only

---

## Phase 0 — Tech Refresh & Project Cleanup ✅ Completed

*Do this before any new features to avoid building on a stale foundation.*

**Completed 2026-02-21.** .NET 8→9, Angular 17→19 (standalone), ng-bootstrap 16→18, esbuild application builder, fixed EF Core `.Include()` for navigation properties, fixed `GetNewOrderNumber()` crash on empty table, added global error handling middleware in `Program.cs`, migrated all components to standalone, removed legacy NgModule/SSR/oidc-client, fixed production `environment.prod.ts` (was missing `apiUrl`).

### 0.1 — Upgrade Dependencies

**Backend**
- Upgrade to **.NET 9** (latest LTS-adjacent stable; .NET 10 ships Nov 2025 — consider targeting 10 if timeline allows)
- Upgrade all NuGet packages to latest compatible versions
- Remove the `OrderDatabase` SSDT project — it is redundant with EF Core migrations and adds friction

**Frontend**
- Upgrade to **Angular 19** (current stable as of early 2026)
- Upgrade `ng-bootstrap` to match
- Upgrade all `npm` packages; run `npm audit fix`
- Standardize on standalone components throughout (AllOrdersComponent is already standalone; EditOrderComponent is not — make it consistent)

### 0.2 — Project Reorganization

Current structure has the Angular app nested inside the .NET project. This is the default ASP.NET SPA template structure and is acceptable to keep. However, clarify the following:

- Remove `OrderDatabase/` SSDT project from the solution entirely
- Consolidate model definitions: the `OrderItems/` folder in the backend and `models/` folder in the frontend should stay in sync; consider generating TypeScript types from C# models (NSwag or Swashbuckle + openapi-generator)
- Move the hardcoded API base URL (`https://localhost:7005/api`) out of `environment.ts` and into a proper environment-specific config so production and dev point to different hosts
- Add a `.editorconfig` and formatting baseline if not present

### 0.3 — Code Quality Baseline

- Fix the TODO in `OrderController.cs` regarding route naming
- Add basic global error handling middleware in `Program.cs`
- Add an Angular global HTTP error interceptor so API failures surface to the user
- Verify EF Core relationships for `Cake`, `Cupcake`, `Cookie`, `Pupcake` — these need proper foreign keys to `Order` or the form data will not persist correctly

---

## Phase 1 — Core MVP Hardening ✅ Completed

*The application must be reliable and complete before adding new features.*

**Completed 2026-02-21.** Fixed `createOrder()` to correctly assemble form values before submitting. Fixed `formArrayName` + `@for` template pattern. Fixed column order in all-orders table. Added `GetDeletedOrders` and `RestoreOrder` endpoints. Added `DeletedOrdersComponent` with route and nav link. Rewrote `edit-order.component.html` with `@if`/`@for`, inline validation, and radio buttons. Rewrote `home.component.html` with dashboard cards. Both `dotnet build` and `ng build` passing with 0 errors.

### 1.1 — Fix Order Item Persistence

**Problem:** `Cake`, `Cupcake`, `Cookie`, and `Pupcake` models exist but it is unclear if they are properly related to `Order` via EF navigation properties and stored in their own tables. This is the most critical bug risk.

**Action:**
- Audit `Order.cs` and `DataContext.cs` — confirm navigation properties, foreign keys, and EF `OnModelCreating` configuration
- Confirm that creating/updating an order correctly saves all nested items (cakes, cupcakes, etc.) and that they load back correctly
- Write a migration if the schema needs correction

### 1.2 — Form Validation & UX

- Complete form validation in `EditOrderComponent` — all required fields should block submission
- Show validation errors inline (ng-bootstrap has form feedback support)
- Confirm vs. discard changes prompt when navigating away with unsaved edits
- Success/error toast notifications on save

### 1.3 — Order Numbering

- Verify that `GET /api/order/newOrderNumber` returns the correct next order number reliably
- Add a uniqueness constraint on `OrderNumber` at the database level if not already present

### 1.4 — Soft Delete Audit

- Confirm deleted orders are excluded from all-orders list and bake sheet
- Add a "View Deleted Orders" admin screen so records can be recovered if needed

---

## Phase 2 — Attachments ✅ Completed

*Reference photos and inspiration links are part of the current paper workflow.*

**Decisions confirmed:** Files stored on disk at `./attachments/{orderNumber}/` relative to app. Upload types: images and PDFs only, by employees. No inspiration URL feature — URLs go in the notes field. Phase 2.3 (link attachments) is skipped.

**Completed 2026-02-21.** Also upgraded .NET 9 → 10 and EF Core 9 → 10 to match the installed SDK/tools (dotnet 10.0.100, dotnet-ef 10.0.1). Created `OrderAttachment` model (`Id`, `OrderNumber`, `FileName`, `StoredFileName`, `ContentType`, `UploadedAt`). Added `DbSet<OrderAttachment>` to DataContext. Created `AttachmentController` with GET list, POST upload (multipart), DELETE, and GET file endpoints. Files stored at `{ContentRootPath}/attachments/{orderNumber}/` with guid-based stored filenames. EF migration `AddOrderAttachment` generated (run `dotnet ef database update` when DB is accessible). Angular: added `OrderAttachment` interface, `AttachmentService`, and attachment card grid in edit-order form (images show thumbnail, PDFs show document icon, View + Delete per file). Attachments load automatically when editing an existing order.

### 2.1 — File Upload

- Add a `POST /api/order/{orderNumber}/attachments` endpoint that accepts multipart form data
- Store files on disk in a configured folder (e.g., `attachments/{orderNumber}/`) or in the database as BLOBs
  - **Recommendation:** Store on disk, store file path/metadata in a new `OrderAttachment` table
- `OrderAttachment` model: `Id`, `OrderNumber` (FK), `FileName`, `ContentType`, `FilePath`, `UploadedAt`

### 2.2 — Attachment UI

- Add an attachments section to `EditOrderComponent`
- Drag-and-drop or click-to-upload for image files and PDFs
- Show thumbnails for images, icons for other file types
- Allow deletion of individual attachments

### 2.3 — Link Attachments

- Allow adding URLs as "link attachments" (customer pastes inspiration link)
- Stored in the same `OrderAttachment` table with a `Url` field instead of a file path

---

## Phase 3 — Calendar View ✅ Partially Complete (3.1 done)

*Replaces the manual Google Calendar entry step in the current workflow.*

### 3.1 — Calendar Component (In-App) ✅ Completed

**Completed 2026-02-21.** Custom standalone monthly calendar grid (no external library — angular-calendar requires Angular >=20). Orders appear on their `OrderDateTime` date. Blue = pickup, green = delivery. Click any order pill to open it in the edit form. Prev/Next/Today navigation. Located at `/calendar`, linked in nav and home dashboard. Database seeded with 15 realistic fake orders (spread over current month) via `Data/SeedData.cs`; seeds automatically on first startup if Orders table is empty. Also added `context.Database.Migrate()` to `Program.cs` for zero-touch deployment.

**Remaining for 3.1:** Weekly view toggle (future enhancement).

### 3.2 — Due Date Field

- Add a `DueDate` (DateTime) field to the `Order` model if not already distinct from `OrderDateTime`
- Add a migration
- Surface it prominently in the edit form and use it as the calendar event date

### 3.3 — Google Calendar Integration ⏳ TODO

**Prerequisites:**
- Cloudflare Tunnel + domain must be live first (OAuth redirect URI must be a public HTTPS URL)
- Production URL: `https://orders.canonsburgcakecompany.com`
- OAuth redirect URI will be: `https://orders.canonsburgcakecompany.com/api/google/callback`

**Steps when ready:**
1. Create a Google Cloud project, enable Calendar API, create OAuth 2.0 Web Application credentials
2. Add redirect URI above to the OAuth client in Google Cloud Console
3. Add `Google.Apis.Calendar.v3` NuGet package to backend
4. Add `GoogleCalendarEventId` field to `Order` model + migration
5. Create `GoogleCalendarService` (token storage, create/update/delete events)
6. Create `GoogleCalendarController` (`/authorize`, `/callback`, `/status`, `/disconnect`)
7. Hook into `OrderController` — sync calendar after every create/update/delete
8. Add connection status indicator to the UI

**Event format:**
- Title: `#1001 — Emily Johnson (Pickup)` or `(Delivery)`
- Date/time: `OrderDateTime`
- Description: product summary + notes + delivery address if applicable
- Store `GoogleCalendarEventId` on the Order for future updates/deletes

---

## Phase 4 — Bake Sheet ✅ Completed

*Every Monday the baker needs a calculated list of what to bake for the week.*

**Completed 2026-02-21.** Created `BakeSheetController` (`GET /api/bakesheet?weekOf={date}`) that returns orders in a Monday–Sunday window (defaults to current week) with all navigation properties included. Angular: added `BakeSheetService`, `BakeSheetComponent` at `/bake-sheet`. Shows: orders-this-week list (with clickable order links, product lines per order), and separate grouped totals sections for cakes, cupcakes, cookies, and pupcakes. Prev/Next/This-Week navigation. Print button (window.print()) with `@media print` styles that hide nav controls. Added Bake Sheet nav link and home dashboard card. Both builds pass.

### 4.1 — Bake Sheet Logic ✅

- `GET /api/bakesheet?weekOf={date}` — returns `BakeSheetResponse { weekStart, weekEnd, orders[] }` with all product nav properties included
- Week = Monday–Sunday; defaults to current week if no `weekOf` param
- Frontend aggregates totals by product type

### 4.2 — Bake Sheet UI ✅

- Route `/bake-sheet`, nav link, home dashboard card
- Orders-this-week section + per-product-type totals tables
- Print-friendly layout (`@media print` hides nav controls, print button)

### 4.3 — Bake Sheet Timing ✅

- Week date range displayed prominently in header (e.g., "Feb 23 – Mar 1, 2026")

---

## Phase 4.5 — Management Page ✅ Completed

**Completed 2026-02-21.** Created `OptionItem` model (Id, Category, Value, IsActive, SortOrder) and `SignatureCupcake` model. Added `SignatureName` to `Cupcake`. Updated `DataContext` with new DbSets. EF migration `AddManagementTables` written manually. Created `OptionController` (CRUD) and `SignatureCupcakeController` (CRUD). Updated `SeedData.cs` to seed 7 option categories and 4 starter signatures on first run. Angular: added `option-item.ts`, `signature-cupcake.ts` models; updated `cupcake.ts` with `signatureName`; created `OptionService`, `SignatureCupcakeService`; created `ManagementComponent` at `/management` with collapsible sections for each option category, signature cupcakes table (inline edit/add/delete), and deleted orders. Order form updated: "Custom Cupcakes" + "Signature Cupcakes" buttons; signature rows have datalist combobox that auto-fills flavor fields on match; all flavor text inputs get `<datalist>` autocomplete; tier size, cake shape, cupcake size, cookie type selects now dynamic from API. "Deleted Orders" nav link replaced with "Management"; `/deleted-orders` route removed. Both builds pass.

*Form dropdowns (tier sizes, shapes, flavors, cookie types) are currently hardcoded. Seasonal menus change — the owner needs a way to add, rename, disable, or reorder options without touching code. Also consolidates deleted order recovery and signature cupcake menu management.*

### Confirmed Design Decisions

- All configurable option lists stored in `OptionItems` table: `Id`, `Category`, `Value`, `IsActive`, `SortOrder`
- `IsActive = false` hides option from new orders; existing orders that used the value are unaffected
- `CakeFlavor` and `CupcakeFlavor` use the **same** `Flavor` category — one shared list
- `FillingFlavor` and `IcingFlavor` are also shared across cakes and cupcakes
- Cookies are included in management (seasonal types come and go)
- Pupcakes remain hardcoded (stable)
- **Signature Cupcakes**: separate `SignatureCupcakes` table; `Cupcake` model gets optional `SignatureName` field
- **Deleted Orders** moved entirely to Management page; `/deleted-orders` route removed from nav
- Order form: "Custom Cupcakes" button (current behavior) + new "Signature Cupcakes" button (separate row type)
- Signature rows: combobox auto-fills flavor fields, but fields remain **editable** for modifications (allergy subs, etc.)
- Flavor fields on signature rows remain visible (so staff can see what's in the cupcake)

### 4.5.1 — Backend

**New model — `OptionItem.cs`:**
```csharp
public class OptionItem {
    [Key] public int Id { get; set; }
    public string Category { get; set; } = null!;  // see categories below
    public string Value { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
```

**Categories:**
| Category key | Used in |
|---|---|
| `CakeTierSize` | Cake tier size dropdown |
| `CakeShape` | Cake shape dropdown |
| `Flavor` | Cake flavor + Cupcake flavor (shared) |
| `FillingFlavor` | Cake filling + Cupcake filling (shared) |
| `IcingFlavor` | Cake icing + Cupcake icing (shared) |
| `CupcakeSize` | Cupcake size dropdown |
| `CookieType` | Cookie type dropdown (seasonal) |

**New model — `SignatureCupcake.cs`:**
```csharp
public class SignatureCupcake {
    [Key] public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string CakeFlavor { get; set; } = null!;
    public string FillingFlavor { get; set; } = null!;
    public string IcingFlavor { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
```

**Cupcake model change:** Add `public string? SignatureName { get; set; }` (nullable — null = custom cupcake)

**New controllers:**
- `OptionController`: `GET /api/option`, `GET /api/option/{category}`, `POST /api/option`, `PUT /api/option/{id}`, `DELETE /api/option/{id}`
- `SignatureCupcakeController`: `GET /api/signaturecupcake`, `POST`, `PUT /{id}`, `DELETE /{id}`

**Seed data:** Populate `OptionItems` with current hardcoded values + initial signature cupcake list.

**EF migration:** Add `OptionItems` and `SignatureCupcakes` tables; add `SignatureName` column to `Cupcakes`.

### 4.5.2 — Order Form Changes

**Cupcake section — two row types:**
- "Custom Cupcakes" button → adds current-style row (size, qty, cake flavor dropdown, filling dropdown, icing dropdown)
- "Signature Cupcakes" button → adds signature row:
  - Combobox (`<input>` + `<datalist>`) for signature name — type to filter, ~50 items
  - When a signature is selected: auto-fills CakeFlavor, FillingFlavor, IcingFlavor from the signature definition
  - Flavor fields remain **editable** for modifications
  - Size and Qty still free inputs
- Both row types stored in the same `Cupcakes` table; `SignatureName` is set only on signature rows

**Cake section:**
- `CakeFlavor`, `FillingFlavor`, `IcingFlavor` text inputs → `<select>` dropdowns backed by option lists

**Edge case:** If a loaded order has a value not in the current option list (disabled or deleted), inject it as a selected disabled `<option>` so it survives re-save without data loss.

### 4.5.3 — Management UI (`/management`)

Single scrollable page with collapsible sections (chevron toggle, same pattern as bake sheet):

**Option list sections** (one per category — Flavors, Fillings, Icings, Cake Sizes, Cake Shapes, Cupcake Sizes, Cookie Types):
- Sorted list of values; each row: value text (click-to-edit inline) | Active toggle | ↑↓ reorder
- Inactive items shown muted/strikethrough
- "Add" input at the bottom of each section

**Signature Cupcakes section** (collapsible, same UI as bake sheet "Orders This Week"):
- Scrollable inline-editable table: Name | Cake Flavor | Filling | Icing | Active
- Flavor cells use dropdowns backed by option lists
- "Add row" button at bottom

**Deleted Orders section** (collapsible):
- Existing deleted-orders table UI moved here
- Restore button per row

### 4.5.4 — Nav & Home

- Add "Management" to nav; remove "Deleted Orders" from nav
- Update home dashboard: replace Deleted Orders card with Management card
- `/deleted-orders` route removed (no redirect needed — it was internal only)

---

## Phase 5 — Google Maps Integration

*For delivery orders, the driver needs a clickable maps link.*

### 5.1 — Address Autocomplete

- Add Google Maps Places Autocomplete to the delivery address field in `EditOrderComponent`
- Use the Angular Google Maps library (`@angular/google-maps`)
- Validate and normalize the address on selection

### 5.2 — Maps Link Generation

- On save, generate a Google Maps URL for any delivery order:
  `https://www.google.com/maps/dir/?api=1&destination=<encoded address>`
- Store the generated URL on the order (`DeliveryMapsUrl` field)
- Display as a clickable link on the order form and in the calendar event description (Phase 3.3)

---

## Phase 6 — Historical Order Entry & Bulk Import

*Orders exist going back ~4 years (3000–4000+ records) on paper.*

### 6.1 — Bulk Import Tool

- Design a simple CSV/spreadsheet format for historical order entry
- Create a `POST /api/order/import` endpoint that accepts a CSV and bulk-inserts orders
- Validate for duplicate `OrderNumber` before inserting
- Return a summary report of successes/failures

### 6.2 — Historical Entry UI (Optional)

- Consider a simplified "quick entry" form for historical orders (fewer fields, just the essentials)
- Or use the existing `EditOrderComponent` with a "historical order" toggle that hides irrelevant fields

### 6.3 — Order Number Continuity

- Ensure the system can accept any `OrderNumber` (not just auto-incremented from current max)
- Confirm no gaps break existing numbering logic

---

## Phase 7 — Authentication

*No auth currently exists. The app will be accessible on a local network — minimal auth is still required.*

### 7.1 — Authentication Strategy

**Recommendation:** Simple username/password with ASP.NET Core Identity + JWT tokens

- Single shared admin account for the shop is acceptable as a starting point
- Add login page at `/login`
- Protect all API routes with `[Authorize]`
- JWT stored in `localStorage` or `sessionStorage` on the frontend
- Angular route guard on all routes

**Alternative:** Windows Authentication if the shop PC is domain-joined — simpler to set up, no password management needed.

### 7.2 — Role Considerations (Future)

- Baker role: read-only bake sheet access
- Shop staff: full order CRUD
- Admin: can delete, view audit logs

---

## Phase 8 — Deployment & Hosting

*The biggest architectural change. Currently single-machine with no remote access.*

### 8.1 — Database

**Decision:** SQLite — switched from SQL Server Express 2026-02-21.

- Zero installation required on shop PC — just a file (`orders.db`) next to the app
- EF Core Sqlite provider in use; connection string: `Data Source=orders.db`
- `*.db` files excluded from git via `.gitignore`
- **TODO (do during this phase):** Add `dbContext.Database.Migrate()` call at startup in `Program.cs` so the database is created/migrated automatically on first run — no manual `dotnet ef database update` step needed on the shop PC

### 8.2 — Backup Strategy

- Use Windows Task Scheduler to run a nightly PowerShell script that copies `orders.db` (and the `attachments/` folder) to a backup location
- Example: copy to a network share, a USB drive, or sync to home PC via Tailscale
- Retain last 30 days of daily backups
- SQLite backup is as simple as `Copy-Item orders.db "backups\orders_$(Get-Date -f yyyyMMdd).db"`

### 8.3 — Application Deployment

**Architecture decision:** Cloudflare Tunnel + real domain (chosen 2026-02-21)

- App runs as a Windows Service on the shop PC (Kestrel on `localhost:5000`)
- `cloudflared` tunnel on the shop PC routes public traffic to the local port — no open firewall ports needed, works through any ISP/CGNAT
- Cloudflare handles HTTPS/TLS automatically
- **Production URL:** `https://orders.canonsburgcakecompany.com`
- **Subdomain:** confirm final subdomain with bakery owner (placeholder: `orders`)

**Setup steps (one-time, do when ready to go live):**
1. Create Cloudflare account, add domain, install `cloudflared` on shop PC
2. `cloudflared tunnel create ccc-inventory`
3. Configure tunnel to forward to `http://localhost:5000`
4. Add CNAME record in Cloudflare DNS
5. Publish .NET app as self-contained `win-x64` Windows Service
6. Install and start the service with `sc.exe`

**⚠️ Auth (Phase 7) must be implemented before going live** — the site will be publicly accessible.

### 8.4 — Dev/Prod Configuration ⏳ TODO

- `appsettings.Production.json` for shop PC config (Data Source path, Kestrel port)
- Frontend `environment.prod.ts`: `apiUrl: '/api'` (already set — app and API served from same origin in production)
- Angular build for production: `ng build --configuration production`

### 8.5 — Deployment Workflow ⏳ TODO

1. Developer commits to `main` on home PC
2. Shop PC pulls latest (`git pull`)
3. `deploy.ps1` script: runs `ng build --configuration production`, then `dotnet publish`, copies output, restarts Windows Service
4. Database auto-migrates on startup (already implemented in `Program.cs`)

---

## Phase 9 — Polish & Operational Features

*Nice-to-haves that improve daily usability.*

- **Print order form:** A print-friendly view of a single order that replicates the paper form layout
- **Order status tracking:** Statuses like Pending, Confirmed, In Progress, Ready, Delivered/Picked Up, Cancelled
- **Customer history:** View all orders for a given customer by name/email
- **Notifications:** Email/SMS confirmation to customer when order is saved (SendGrid or Twilio)
- **Audit log:** Track who changed what and when on each order

---

## Work Order / Priority Sequence

| Priority | Phase | Rationale |
|---|---|---|
| 1 | Phase 0 — Tech Refresh ✅ | Must be done first; reduces future debt |
| 2 | Phase 1 — MVP Hardening ✅ | Core reliability before new features |
| 3 | Phase 2 — Attachments ✅ | High daily value, self-contained |
| 4 | Phase 3.1 — Calendar ✅ | High value; bake sheet depends on it |
| 5 | Phase 4 — Bake Sheet ✅ | High operational value |
| 6 | Phase 4.5 — Management Page ✅ | Fixes hardcoded dropdowns; unblocks seasonal menu changes |
| 7 | Phase 8 — Deployment | Needed to get it into the shop |
| 8 | Phase 7 — Auth | Required before any network exposure |
| 9 | Phase 5 — Maps | Complements calendar and delivery orders |
| 10 | Phase 6 — Historical Import | Useful but not blocking daily use |
| 11 | Phase 9 — Polish | Ongoing |

---

## Open Questions

These need answers before specific phases can be fully designed:

1. **Due date vs. order date:** Is `OrderDateTime` the date the order was placed, or when it's due? A separate `DueDate` field is likely needed.
2. **File storage for attachments:** Local disk on the shop PC is simplest — confirm this is acceptable.
3. **Google account:** Is there a Google account associated with the bakery that will own the Calendar integration?
4. **Authentication level:** Is a single shared login acceptable, or do multiple staff members need individual accounts?
5. **Historical order data:** Is the goal to enter all ~3000+ historical orders, or just recent/relevant ones?
6. **Network setup:** Does the shop PC have a static local IP or hostname that can be bookmarked?
7. **Mobile access:** Does the calendar/order form need to work well on mobile (phone/tablet in the shop)?
