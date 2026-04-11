# CLAUDE.md — CCCInventory Project Instructions

## Project Structure

- Backend: `CCCInventory/` — ASP.NET Core 9, EF Core 9, SQL Server Express (dev: SQLite until Phase 15 migration)
- Frontend: `CCCInventory/ClientApp/` — Angular 19 standalone components
- Models: `CCCInventory/OrderItems/` (C#), `CCCInventory/ClientApp/src/app/models/` (TypeScript)
- Services: `CCCInventory/ClientApp/src/app/services/`
- Components: `CCCInventory/ClientApp/src/app/components/`

## Key Conventions

- All Angular components are standalone
- Angular new control flow syntax (`@if`, `@for`) — no `*ngIf`/`*ngFor`
- EF Core navigation collection properties use `= null!` to suppress CS8618
- File attachments stored at `./attachments/{orderNumber}/` relative to the app working directory
- API base URL comes from Angular environment files (`environment.ts` / `environment.prod.ts`)
- Terminology: orders are **Archived** (soft-cancel with required reason), never Deleted. Backend flag is `CancelledFlag`.

## Seed Data

`CCCInventory/Data/SeedData.cs` must be kept in sync with the current data model. After any phase that adds fields to `Order`, `Cake`, `Cupcake`, `Cookie`, `Pupcake`, or `OtherItem`, update `SeedData.cs` to:
- Populate every new field on all existing seed orders (no blank/default values that mask functionality)
- Include at least one order that exercises the new field
- Keep orders distributed across **three weeks**: one week prior (archived), current week (mixed active/archived), one week ahead (active), so all five status filters (Active, Incomplete, Ready for Pickup, Cancelled, Archived) are always exercised from a fresh seed

---

## Current State (as of 2026-04-09)

### Completed Phases

| Phase | What was built |
|---|---|
| 0 | Tech refresh — .NET 9, Angular 19, standalone components, SQLite, esbuild |
| 1 | Core MVP — order CRUD, form validation, toasts, home dashboard |
| 2 | Attachments — upload/list/delete/serve; stored at `attachments/{orderNumber}/` with GUID filenames |
| 3 | Calendar — monthly/weekly view at `/calendar` (temporary; removed when Phase 10 ships) |
| 4 | Bake sheet — first version (superseded by Phase 7) |
| 4.5 | Management — option lists (CRUD, in-use protection, active toggle), signature cupcakes |
| 5 | Order form overhaul — `Title`, `CancellationReason`, `CancelledFlag`, `IsReadyForPickup`, `Labor`/`FlavorUpgrade`/`LookbookPrice`, `Flavor2`, `LayerFlavors` (JSON), `CookieSize`, `OtherItem`; 3-col grid; split date/time; Tasting radio; 4 toggles; Archive modal (required reason) + Restore flow; autosave (4 s debounce); incomplete detection 10+ conditions; red field highlighting; cake A/B/C labels; auto-notes on cake add; per-layer flavors; half-and-half; cookie size dropdown; Other item type; attachment upload from new order (auto-saves first) |
| 6 | All Orders & Homepage — sortable columns, Date column, status toggle (Active / Incomplete / Ready for Pickup / Cancelled / Archived), default Active, red Incomplete badge, auto-archive derived at query time |
| 7 | Bake sheet redesign — per-layer rows, cakes+cupcakes combined, Thu–Wed bake week, day-of-week color highlights (Mon=red…Sat=purple), order # pastel color coding, Micro/Quarter Sheet special display rules, Other items at bottom, print layout with gridlines |
| 8 | Authentication — ASP.NET Core Identity + JWT HttpOnly cookie; 4-digit PIN per staff member; inactivity timeout → PIN screen; staff management in Management page; AuditLog table |
| pre-9 | Pre-phase fixes — attachment carousel modal (click image thumbnail → full-size modal w/ prev/next); CookieSize already present in management categories |

### Known Gaps in Completed Phases

None — all known gaps resolved as of 2026-04-11.

### Calendar UI

`/calendar` route and `CalendarComponent` are **kept in the codebase** but hidden from the UI (nav link and homepage card removed as of 2026-04-10). The route is still directly navigable. Removed entirely when Phase 10 ships post-launch.

---

## Remaining Phases

Phases are numbered in execution order.

---

### Phase 8 — Authentication *(go-live blocker)*

- **Primary:** ASP.NET Core Identity + JWT in `HttpOnly` cookie with long expiry (stays logged in at shop like YouTube/Facebook)
- **Secondary PIN:** 4-digit PIN per named staff member record; inactivity timeout (configurable, default ~minutes) soft-logs out to PIN screen without clearing primary JWT; any staff member can enter their PIN to resume
- **Staff management:** "Manage Users" section on Management page — list of staff names with editable PIN (explicitly non-secure, plain text is appropriate)
- **Audit trail:** `AuditLog` table — staff member (from active PIN session), entity type + ID, field changed (from→to), timestamp; enables detecting malicious changes and rolling back data entry errors

### Phase 9 — Deployment & Hosting *(go-live blocker)*

**Architecture:**
```
Developer (home) ──Tailscale──► Bakery PC  (SSH/RDP, SSMS, deploy)
                                     │
                               SQL Server Express
                               Kestrel :5000
                                     │
                               cloudflared tunnel
                                     │
Public internet ──HTTPS──► Cloudflare edge ──► orders.canonsburgcakecompany.com
```

**Database:** SQL Server Express (free, 10GB limit). EF Core provider: `Microsoft.EntityFrameworkCore.SqlServer`. `Database.Migrate()` at startup (or EF Core migration bundle in deploy script for explicit control). Migration from SQLite: swap NuGet package + connection string; existing EF Core migrations apply cleanly.

**Backup:** Nightly PowerShell script (Windows Task Scheduler) running `BACKUP DATABASE` to a local folder + `attachments/` copy; retain 30 days; sync to network share, USB, or home PC over Tailscale.

**Remote access (developer):** Tailscale — private admin channel for SSH/RDP, SSMS connections, and triggering deploys. Completely separate from the public Cloudflare Tunnel.

**Public hosting:** Cloudflare Tunnel (`cloudflared`) on bakery PC routes `orders.canonsburgcakecompany.com` → `localhost:5000`. Cloudflare handles TLS. No open firewall ports required. Works through any ISP/CGNAT.

**DNS:** Add `orders` CNAME in Squarespace DNS pointing to the Cloudflare Tunnel hostname. Do NOT transfer the domain — it is used by Square and other commerce platforms.

**CI/CD:** Self-hosted GitHub Actions runner installed as a Windows Service on bakery PC. Push to `main` triggers: `ng build --configuration production` → `dotnet publish` → stop service → swap files → `Database.Migrate()` bundle → restart service.

**⚠ Phase 8 (auth) must be complete before the public URL goes live.**

**One-time setup checklist:**
1. Purchase bakery PC; install Windows + SQL Server Express
2. Install Tailscale on both home PC and bakery PC
3. Create Cloudflare account; install `cloudflared` on bakery PC; create tunnel
4. Add `orders` CNAME in Squarespace DNS
5. Register self-hosted GitHub Actions runner on bakery PC
6. Publish .NET app as self-contained `win-x64` Windows Service; install with `sc.exe`
7. Configure Google OAuth redirect URI once domain is live (prerequisite for Phase 10)

### Phase 10 — Google Calendar Integration *(post-launch)*

**Prerequisites:** Cloudflare Tunnel + domain live before implementing — OAuth redirect URI must be a public HTTPS URL.
- Production URL: `https://orders.canonsburgcakecompany.com`
- OAuth redirect URI: `https://orders.canonsburgcakecompany.com/api/google/callback`

**Event format:**
- Title: `"OrderNumber - Title - FirstName LastName"`
- Color key: Blue=cakes, Purple=cupcakes/cookies/pupcakes (mixed non-cake orders use Purple), Green=ready for pickup, Red=cancelled/archived, Yellow=delivery. Cakes take color priority over all others.
- Timing: orders stack from midnight on their due date in creation order, 30 min each. Party Rentals appear at actual scheduled start time.

**Implementation:** Add `GoogleCalendarEventId` to `Order` + migration; `GoogleCalendarService` (token storage, create/update/delete); `GoogleCalendarController` (`/authorize`, `/callback`, `/status`, `/disconnect`); hook into `OrderController` on every create/update/archive.

**Removes `/calendar` route and `CalendarComponent` once live.** Until then the built-in calendar remains as temporary infrastructure.

### Phase 11 — Party Rentals *(backburnered — post-launch)*

New independent feature. New backend models, controller, Angular route, management items.

**Models:**
- `PartyRental`: Name, Phone, Email, Type (FK→OptionItem "Party Rental Types"), DateOfEvent, StartTime, EndTime, NumberOfGuests, RoomArrangementId (FK→`RoomArrangement`), BaseRentalRate, AdditionalHours, AdditionalHourlyRate, `ICollection<PartyRentalAddOn>`
- `PartyRentalAddOn`: AddOnType (FK→OptionItem "Party Rental Add-Ons"), Price (decimal, prefilled from management config, editable), Notes
- `RoomArrangement`: Label, ImagePath, IsActive

**Management additions:**
- Party Rental Types (option list — same pattern as other categories)
- Party Rental Add-Ons (option list with configurable default price per item)
- Base Rental Rate (single numeric config value)
- Additional Hourly Rate (single numeric config value)
- Room Arrangements (image + label — upload image, set label, activate/deactivate)

**Booking page** at `/party-rental/new` (edit: `/party-rental/:id`):
- *Booking Info section:* Name, Phone, Email, Type dropdown, Date of Event, Start/End Time (two time pickers), Number of Guests, Room Arrangement (image tile selector — large radio-button-style tiles)
- *Cost section:* Base Rental Rate (prefilled, editable); Additional Hours shown as `"[$rate] × [N] hours"` (max 2-digit input)
- *Add-Ons section:* add/remove rows with Add-On Type dropdown, Price (prefilled, editable), Notes

**Google Calendar:** Party Rentals appear at their actual scheduled start time (unlike orders, which stack from midnight).

**Google Doc auto-fill (wishlist/post-launch):** Copy master Google Doc template → rename to "Name - Date" → fill fields → route for digital signature via Google Workspace. Requires Drive + Docs API OAuth; confirm template and field mapping before scoping. Do not build until after launch.

### Phase 12 — Historical Order Bulk Import *(post-launch)*

Paper forms scanned to Google Drive as PDFs. Filename pattern: `OrderNumber - LastName FirstName.pdf` (exact pattern to be confirmed against actual Drive folder before writing parser).

**Backend** `POST /api/order/import-files`: parse filename → create minimal `Order` (order number + name only, skip required field validation) → attach PDF as `OrderAttachment`. Return per-file result with created/duplicate/failed counts.

**Frontend** (Management page, new section): multi-file picker or drag-and-drop, preview table before submit, progress indicator, results summary.

Historical imports bypass `GetNewOrderNumber()` — use explicit number from filename. `MAX + 1` naturally continues after import.

### Phase 13 — Pricing *(post-launch)*

⚠ Needs full pricing requirements from the bakery before building. Placeholder fields (`Labor`, `FlavorUpgrade`, `LookbookPrice`) already exist on the model and form. Configuration, auto-calculation logic, and additional fields are TBD.

### Phase 14 — Customer Profiles & Autocomplete *(post-launch)*

New `Customer` entity + autocomplete on the order form + Customers page.

**`Customer` model:** `CustomerId`, `FirstName`, `LastName`, `Email`, `Phone`

**`Order` change:** add nullable `CustomerId` FK. Order keeps its own `FirstName`/`LastName`/`Email`/`Phone` fields (denormalized snapshot at time of order — Customer record is the current source of truth, order fields are historical).

**Autocomplete on order form:**
- On `LastName` field blur → `GET /api/customer/search?name=...`
- Dropdown shows Name + Phone + Email for disambiguation (handles same-name customers)
- Selecting a result fills all four contact fields + stores a hidden `customerId` on the form
- Dismissing the dropdown leaves fields as typed

**Customer matching on order save:**
- Email blank → skip customer logic entirely; `CustomerId` stays null (walk-in/quick orders with no email float without a customer link — by design)
- Email present → find `Customer` by email
  - Found → link order to existing customer; optionally update their name/phone if changed
  - Not found → create new `Customer` from the order's contact fields, link

**Customers page** (`/customers`):
- Search/list view of all customers who have an email on file
- Customer detail: editable contact info + full order history with status indicators
- "New Order" button → navigates to new order form pre-filled with customer data

**Notes:**
- Historical imports (Phase 12): no backfill needed; data integrity addressed post-import if desired
- Email is the unique match key — two people with the same name but different emails are correctly separate customers

### Phase 15 — Mobile Optimization *(post-launch)*

Post-launch, after all other features. Responsive layout for phone and tablet. MVP is explicitly desktop-only.

### Phase 16 — Google Maps *(post-launch)*

- Google Maps Places Autocomplete on the delivery address field (`@angular/google-maps`)
- On save, generate `DeliveryMapsUrl` (`https://www.google.com/maps/dir/?api=1&destination=<addr>`) and store on the order
- Display as clickable link in edit form and in Google Calendar event description (Phase 10)

### Phase 17 — Polish *(ongoing)*

- Print-friendly single-order view replicating the paper form layout

### Phase 18 — Feature Requests *(catch-all for nice-to-haves)*

Unscoped ideas that came up during development. Requires proper requirements gathering before building.

**Order Audit / History view**
- A timeline per order showing who created it, who edited it, and when — data is partially there (AuditLog records Create/Update/Archive/Restore with staff + timestamp)
- Field-level diffs ("Customer Name: John → Jane") are not yet stored — `Details` column exists but is always null; would need diff logic added to `UpdateOrder` before any historical data is captured
- Autosave currently generates an Update entry every time it fires, so collapsing nearby entries into a single "editing session" would be needed for readability
- UI options: button on All Orders row or on the open order form to open a history panel/modal
