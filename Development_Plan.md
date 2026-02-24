# CCCInventory — Development Plan

**Project:** Custom Cake & Cookie (CCC) Bakery Order Management System
**Last Updated:** 2026-02-23
**Status:** Active Development — Phase 4.5 Complete; Phase 5 (Order Form Overhaul) Next

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

**What works:** Full order CRUD with soft delete, multi-product support (cakes, cupcakes, cookies, pupcakes), file attachments, monthly/weekly calendar view, bake sheet with grouped totals, management page for option lists and signature cupcakes.

**What is still missing:**
- No authentication
- No Google Calendar or Google Maps integration
- Production deployment not yet set up (single-machine dev only)
- Significant UX refinements from first demo (Phase 5–7)
- Party Rentals feature not yet built

---

## Phase 0 — Tech Refresh & Project Cleanup ✅

.NET 8→10, Angular 17→19 standalone, esbuild, fixed EF Core `.Include()` / `GetNewOrderNumber()`, global error handling, removed legacy NgModule/SSR/oidc-client.

---

## Phase 1 — Core MVP Hardening ✅

Fixed order item persistence, form validation, toast notifications, inline `@if`/`@for` templates, home dashboard, `GetDeletedOrders`/`RestoreOrder` endpoints.

---

## Phase 2 — Attachments ✅

`OrderAttachment` model, `AttachmentController` (upload/list/delete/serve), files stored at `attachments/{orderNumber}/` with guid filenames, Angular attachment card grid with thumbnails in edit form.

---

## Phase 3 — Calendar View

### 3.1 ✅

Custom standalone monthly/weekly calendar at `/calendar`. Orders appear on their `OrderDateTime`. Blue = pickup, green = delivery. Month and week view toggle. Click order pill to open edit form. Seed data: 30 realistic orders distributed Tue–Sat of the upcoming week.

**Note:** The calendar page will be removed once Google Calendar integration (Phase 10) is live. The component and route are temporary infrastructure.

### 3.2 — Due Date Field ✅ (N/A)

Confirmed: `OrderDateTime` is the pickup/delivery due date. `DateOrderPlaced` (already on the model) tracks when the order was placed. No separate `DueDate` field is needed.

### 3.3 — Google Calendar Integration

Moved to Phase 10.

---

## Phase 4 — Bake Sheet ✅

`BakeSheetController` (`GET /api/bakesheet?weekOf={date}`), Mon–Sun window, navigation properties included. Angular `BakeSheetComponent` at `/bake-sheet`: collapsible orders list (collapsed by default), sortable cakes totals table (default: grouped by flavor, size ascending), cupcakes/cookies/pupcakes totals. Prev/Next/This-Week navigation. Print button with `@media print` styles.

**Note:** Bake Sheet is being fully redesigned in Phase 7.

---

## Phase 4.5 — Management Page ✅

`OptionItem` and `SignatureCupcake` models, `OptionController` (CRUD + in-use check + recheck endpoint), `SignatureCupcakeController`. `ManagementComponent` at `/management`: collapsible option category sections (inline rename, active toggle, delete protection if in use), sortable signature cupcakes table (activate/deactivate, dropdown editors), deleted orders recovery. Order form: dynamic dropdowns from API, Custom + Signature cupcake row types, datalist combobox auto-fill.

**Note:** The "Deleted Orders" section will be removed in Phase 5. Archived order management moves to All Orders. Party Rentals management items will be added in Phase 8.

---

## Phase 5 — Order Form & Core Data Model Overhaul

*First demo feedback. This is the largest phase — it touches the core data model, the order form, and several cross-cutting behaviors (Archive, Autosave, status system). All sub-phases share the same migration and should be implemented together.*

### 5.1 — Data Model Changes

**New fields on `Order`:**
- `Title` (string?) — short label displayed at the top of the form; used in calendar event titles
- `CancellationReason` (string?) — required when cancelling an order
- `CancelledAt` (DateTime?) — timestamp set when the order is cancelled
- `CancelledFlag` (bool) — replaces `DeleteFlag`; set via the cancellation flow
- `IsReadyForPickup` (bool) — set manually via the "Ready for Pickup" toggle on the form
- Rename `DeleteFlag` → `CancelledFlag` (and all related backend references throughout)

**New `OptionItem` category:** Cookie Sizes (seed: "Standard", "Small")

**New order item type:** `OtherItem` — two free-text fields: `Name` and `Item`

**Cake model additions:**
- `Flavor2` (string?) — second flavor for half-and-half cakes
- Per-layer flavor support — design TBD (JSON column or related table); see Open Questions

**Migration required** for all of the above.

### 5.2 — Terminology: Delete → Cancel/Archive

Rename all "Delete" references throughout the application:
- Backend: `DeleteFlag` → `CancelledFlag`, `GetDeletedOrders` → `GetCancelledOrders`, `DeleteOrder` → `CancelOrder`
- Frontend: "Delete" button → "Archive" button; toast messages and other UI text updated accordingly
- No functional change — soft-delete behavior is preserved; the user-facing label is "Archive" but the internal concept is now "Cancelled"

### 5.3 — Cancel Flow (Archive button)

When the user clicks "Archive" (cancel an order):
1. A modal opens with a required free-text field labeled "Cancellation Reason" and Archive / Cancel buttons
2. On Archive: save `CancellationReason`, set `CancelledAt` = now, set `CancelledFlag` = true
3. On Cancel: close modal, do nothing

The order remains visible in the **Cancelled** filter until its `OrderDateTime` passes, at which point it transitions to **Archived** automatically.

When a **cancelled order** is opened:
- Display the `CancellationReason` prominently between the "Order Form" header and the order title — large font, editable textbox, thick red border (matching the Archive button style, but not a button)
- Show a "Restore" button at the bottom alongside "Update Order"
- On Restore: prepend to the Details field: `"Original Cancellation Reason - [CancelledAt timestamp] - [CancellationReason text]"`; clear `CancelledFlag` and `CancellationReason`/`CancelledAt`

### 5.4 — Order Status System

Five states used across the application:

| State | Meaning |
|---|---|
| **Active** | Open order, due date in the future, required fields complete, not cancelled |
| **Incomplete** | Active, but missing required fields or has no items added to the order |
| **Ready for Pickup** | User has toggled "Ready for Pickup" on the order form |
| **Cancelled** | Cancellation reason recorded, but order date has not yet passed |
| **Archived** | Order date has passed — applies to all orders regardless of how they ended (fulfilled, ready, or cancelled) |

Backend flags: `CancelledFlag` (manually cancelled via Archive button), `IsReadyForPickup` (toggle on form). Incomplete is derived (required fields null/empty or no items). Archived is derived when `OrderDateTime < now`. Active is the remaining default.

**Note:** The old "Inactive" concept is removed. The old "Delete/Archive" flag is now "Cancelled." True archival (moving to the Archived filter) happens automatically once the order date passes for any order.

### 5.5 — Autosave & Incomplete Orders

**Autosave behavior:**
- Order Number is auto-filled on page load — it does not count toward the trigger
- The first autosave is triggered once at least one other field has been filled
- Saves are debounced (~3–5 seconds after the last change)
- On the first autosave of a new order: create the order record with whatever data is present, bypassing required field validation
- Orders missing required fields or with no items are flagged as **Incomplete**

**Homepage — Incomplete Orders section:**
- Add a new section on the homepage/overview
- Link to the All Orders page pre-filtered to Incomplete
- If any incomplete orders exist: solid red button; otherwise: white button with blue text (matching other overview buttons)

### 5.6 — Order Form: Title Field

At the very top of the order form, before the Order Information section:
- A "Title" label and single-line text input on the same line, styled the same as other subheaders ("Order Information", "Add to order:", etc.)
- Followed by a `<hr>` horizontal rule

### 5.7 — Order Form: Layout Reorganization

Reorganize the Order Information section into a 3-column grid:

| Column 1 | Column 2 | Column 3 |
|---|---|---|
| Order Number | Customer Name | Details (textarea, spans rows) |
| Customer Email | Customer Phone Number | |
| Delivery/Pickup Date | Delivery/Pickup Time | |
| Delivery Location | Initial Contact Method | |
| Day-of Contact Name | Day-of Contact Phone | |

- Date and Time are two separate controls (DatePicker + TimePicker), combined into the existing `OrderDateTime` on save
- Below the grid: 3 radio buttons — Pickup / Delivery / **Tasting**
- Below the radio buttons: 4 toggles in a row — Contract Sent, Day-of Text Sent, Confirmation Text Sent, **Ready for Pickup**
  - Ready for Pickup is set manually by the user; it does not auto-toggle
  - In the backend this maps to `IsReadyForPickup = true`
- Below the toggles: the "Add to order:" section with item type buttons
- Cake/cupcake/cookie/pupcake/other rows follow

### 5.8 — Order Form: Dropdown Uniformity

All dropdowns in the order form must have a uniform look and feel matching the Cake Tier Size, Cake Shape, and Cupcake Size dropdowns.

### 5.9 — Order Form: Navigation Bug Fix

**Bug:** Opening an order from All Orders or the calendar page, then clicking "Order Form" in the nav, loads that order instead of a blank new order form.

Fix: ensure navigating to the new-order route always resets component state. The edit-order component should not retain the previous order when the route has no order ID.

### 5.10 — Order Form: Cake Improvements

**Half-and-half cakes:**
- The existing toggle should reveal a second Flavor dropdown when active
- When toggled off, only one flavor dropdown is shown

**Per-layer flavor customization:**
- Add a "Custom layer flavors" option (toggle or checkbox)
- When enabled, each tier gets its own Flavor dropdown instead of the shared flavor
- Show/hide to keep the UI uncluttered when not in use

**Cake labeling:**
- Next to the add/remove buttons for each cake, display a large letter label: A, B, C, etc.

**Auto-notes:**
- When a cake is added, automatically append `"Cake A -\n"` (or B, C, etc.) to the order Notes field

### 5.11 — Order Form: Cupcake Cleanup

When "Custom Cupcakes" row type is selected, hide the Signature dropdown — it is not relevant for custom cupcakes.

### 5.12 — Order Form: Cookies

Add a Size dropdown to cookie rows, populated from the new Cookie Sizes option category in Management (seed data: "Standard", "Small").

### 5.13 — Order Form: Other Item Type

Add an "Other" button to the "Add to order:" section. Other rows have two free-text fields: Name and Item.

### 5.14 — Order Form: Attachment Fixes

**Bug fix:** The Attachments section does not appear when loading a fresh new order form. It should be visible and accept uploads before the order has been first saved.

**Attachment modal carousel:** Clicking an attachment thumbnail opens a modal showing the file at a larger size. If multiple attachments exist, the modal shows a carousel allowing navigation through all of them.

### 5.15 — Order Form: Pricing Fields (Placeholder)

Add the following new fields to the Pricing section: Labor, Flavor Upgrade, Lookbook Price.

⚠ Full pricing auto-calculation design is TBD — see Phase 13. For now, surface these as editable numeric inputs with no auto-calculation logic.

### 5.16 — Management Page Cleanup

Remove the "Deleted Orders" collapsible section. Archived order management moves to the All Orders page (Phase 6).

---

## Phase 6 — All Orders & Homepage

*Depends on Phase 5 (status system, new fields). Primarily display, filtering, and sorting work — no additional model changes.*

### 6.1 — All Orders: Table Improvements

- All columns are sortable (click header to toggle asc/desc)
- Add a **Date** column (`OrderDateTime` formatted as date)
- Default sort: Date ascending (soonest first)

### 6.2 — All Orders: Status Filter Toggle

At the top of the page, add a toggle button group (same UI as Month/Week on the calendar page):

**Active | Incomplete | Ready for Pickup | Cancelled | Archived**

- Default on page load: **Active**
- Exception: when navigated from the Incomplete button on the homepage, default to **Incomplete**
- The Incomplete tab displays a visual indicator (red text or badge) if any incomplete orders exist
- Archived = orders whose `OrderDateTime` has passed (includes all fulfilled, ready, and cancelled orders past their date)
- Cancelled = orders with `CancelledFlag = true` whose `OrderDateTime` has not yet passed

### 6.3 — Auto-Archive

Orders automatically move to the Archived state once their `OrderDateTime` passes. This is derived at query time — no stored flag needed. The backend filters results by comparing `OrderDateTime` to `DateTime.Now` when returning orders for each filter tab.

---

## Phase 7 — Bake Sheet Redesign

*Full redesign. The existing aggregated-totals approach is replaced with an order-level, per-layer row model. Mostly independent from Phase 5/6.*

### 7.1 — Structure Changes

- **Remove** the "Orders This Week" collapsible section entirely
- **Combine** cakes and cupcakes into a single table — rows interleaved, grouped by flavor
- Each **cake layer** is its own row (no QTY aggregation)
- Each **cupcake order** is its own row (not aggregated across orders)
- Cookies and Pupcakes remain as separate tables below
- **Other** items (from the Other item type added in Phase 5) appear at the bottom of the page

### 7.2 — Column Structure

| Order # | Size / Qty | Flavor | Day of Week | Complete / Notes |
|---|---|---|---|---|

- **Order #:** Order number. Will repeat for multi-layer cakes (one row per layer).
- **Size / Qty:** For cakes: the tier size. For cupcakes: the quantity for that order.
- **Flavor:** The flavor for that layer/order.
- **Day of Week:** Full day name (e.g., "Thursday").
- **Complete / Notes:** Static display text only — not editable, not saved. Pre-populated for special display cases (see 7.4); otherwise empty. Used for the printed sheet only.

### 7.3 — Day of Week Color Highlighting

Background color applied to the Day of Week cell, following the rainbow (ROYGBP):

| Day | Color |
|---|---|
| Monday | Red |
| Tuesday | Orange |
| Wednesday | Yellow |
| Thursday | Green |
| Friday | Blue |
| Saturday | Purple |

### 7.4 — Special Display Rules

- **Micro cakes:** Display Size as `6"`, pre-populate Complete/Notes with `"Micro"`
- **Quarter Sheet:** Display Size as `"Half Sheet"`, pre-populate Complete/Notes with `"Cut"`
- **Quarter Sheet + Half-and-Half:** Display Size as `"Half Sheet"`, pre-populate Complete/Notes with `"Cut twice"`

### 7.5 — Order Number Color Coding

Rows sharing the same Order Number receive a matching light background color on the Order # cell. Each distinct order number gets a unique color (assigned at render time). Orders with only a single row remain default/white.

### 7.6 — Bake Week

The computation window runs **Thursday through Wednesday** (not Monday–Sunday). Prev/Next/This-Week navigation steps by 7 days aligned to this window.

### 7.7 — Print Layout

- Page is compact and prints on a standard 8.5×11 sheet
- Cakes/cupcakes table displays gridlines (like a spreadsheet)
- When printing: 100% width, no side margins, only the bake tables print (nav bar and other page chrome suppressed via `@media print`)

---

## Phase 8 — Party Rentals

*Entirely new feature. New backend models, controllers, Angular routes, and management items.*

### 8.1 — Data Model

**`PartyRental` model:**
- Name, Phone, Email
- Type (FK to OptionItem — Party Rental Types category)
- DateOfEvent (DateTime)
- StartTime, EndTime (TimeSpan or DateTime)
- NumberOfGuests (int)
- RoomArrangementId (FK to `RoomArrangement`)
- BaseRentalRate (decimal — prefilled from management config)
- AdditionalHours (int)
- AdditionalHourlyRate (decimal — from management config)
- Collection of `PartyRentalAddOn`

**`PartyRentalAddOn` model:**
- AddOnType (FK to OptionItem — Party Rental Add-Ons category)
- Price (decimal — prefilled from management config, editable)
- Notes (string)

**`RoomArrangement` model:**
- Label (string)
- ImagePath (string — stored like attachments)
- IsActive (bool)

### 8.2 — Management Items

Add to the Management page:
- Party Rental Types (option list — same pattern as other dropdowns)
- Party Rental Add-Ons (option list with configurable default price per item)
- Base Rental Rate (single configurable numeric value)
- Additional Hourly Rate (single configurable numeric value)
- Room Arrangements (image + label — upload image, set label, activate/deactivate)

### 8.3 — Booking Page

New route at `/party-rental/new` (and `/party-rental/:id` for edit).

**Booking Info section:**
- Name, Phone, Email inputs
- Type dropdown (from management)
- Date of Event (date picker)
- Time of Rental: Start Time + End Time (two time pickers)
- Number of Guests input
- Room Arrangement: image tile selector — large radio-button-style tiles with images from management

**Cost section:**
- Base Rental Rate (prefilled from management, editable)
- Additional Hours: displayed as `"[$rate] × [____] hours"` (max 2-digit number input)

**Add-Ons section:**
- "Add Add-On" button — adds a row with: Add-On Type dropdown, Price (prefilled, editable), Notes
- Same add/remove pattern as order items in the order form

### 8.4 — Google Calendar Integration

Party Rentals appear on Google Calendar at their actual scheduled start time (unlike orders, which stack from midnight — see Phase 10).

### 8.5 — Google Doc Auto-Fill (Wishlist)

When a booking is saved, optionally: copy a master Google Doc template, rename it to `"Name - Date"`, fill in all booking fields, and route for digital signature via Google Workspace tools.

**Prerequisites:** Google Drive API + Google Docs API OAuth scopes; confirm exact template document and field mapping before scoping.

**Scope:** Post-launch only. Build the Party Rentals booking page first (Phase 8.3); Google Doc auto-fill is deferred until feasibility can be confirmed after launch.

---

## Phase 9 — Authentication (Dual Login + PIN + Audit)

### 9.1 — Primary Authentication

- Username/password login via ASP.NET Core Identity + JWT tokens
- JWT stored as an `HttpOnly` cookie with long expiry — staff stay logged in at the shop without daily re-authentication (similar to YouTube/Facebook session behavior)
- Login page at `/login`; all API routes protected with `[Authorize]`; Angular route guard on all routes
- Single shared account is sufficient for launch; data model supports multiple users from day one

### 9.2 — PIN Secondary Login

- After primary auth, a secondary PIN screen is shown
- Each 4-digit PIN is tied to a named staff member record in the database — this is what makes the audit trail meaningful
- On inactivity timeout (configurable, default: a few minutes), the app soft-logs-out to the PIN screen — the primary JWT cookie is NOT cleared
- Any staff member can enter their PIN to resume the session
- The PIN screen is local-only and does not require re-authentication against the backend

**Staff member management** is handled via a new **"Manage Users"** section on the Management page: a simple list of staff names with an editable PIN field per user. The PIN is explicitly non-secure (no hashing required), so a plain editable field is appropriate.

### 9.3 — Audit Trail

All data modifications (create, update, archive, restore) write to an `AuditLog` table:
- Staff member (from the active PIN session)
- Entity type + ID
- Field changed: from-value → to-value
- Timestamp

Enables detecting malicious changes and rolling back data entry errors.

### 9.4 — Implementation Notes

- Long-lived `HttpOnly` JWT cookie handles primary auth persistence
- PIN session state held in memory/sessionStorage — cleared on inactivity
- Angular inactivity service monitors user activity and redirects to PIN screen on timeout, without clearing the primary auth cookie

---

## Phase 10 — Google Calendar Integration

*Replaces the built-in calendar page. Depends on Phase 5 (Title field for event titles) and Phase 9 (auth for OAuth redirect URI).*

**Prerequisites:** Cloudflare Tunnel + domain live before implementing (OAuth redirect URI must be a public HTTPS URL).
- Production URL: `https://orders.canonsburgcakecompany.com`
- OAuth redirect URI: `https://orders.canonsburgcakecompany.com/api/google/callback`

### 10.1 — Setup

1. Create Google Cloud project, enable Calendar API, create OAuth 2.0 Web Application credentials
2. Add redirect URI to OAuth client in Google Cloud Console
3. Add `Google.Apis.Calendar.v3` NuGet package
4. Add `GoogleCalendarEventId` field to `Order` + migration
5. Create `GoogleCalendarService` (token storage, create/update/delete events)
6. Create `GoogleCalendarController` (`/authorize`, `/callback`, `/status`, `/disconnect`)
7. Hook into `OrderController` — sync after every create, update, and archive

### 10.2 — Event Format

- **Title:** `"OrderNumber - Title - FirstName LastName"` (using the new Title field from Phase 5)
- **Color key:**
  - Blue = cakes
  - Purple = cupcakes
  - Green = completed / ready for pickup
  - Red = cancelled / archived
  - Yellow = delivery
  - Priority when an order has mixed item types: **cakes take priority** over all others. Cupcakes, cookies, and pupcakes all share the same color (Purple) — mixed non-cake orders use Purple.
- **Timing:** Events do NOT appear at their actual scheduled time. On their due date, events stack starting at **midnight**, in the order they were created (creation timestamp). Each event is 30 minutes long.
- **Party Room Rentals** (Phase 8) appear at their actual scheduled start time — they are not stacked.

### 10.3 — Calendar Page Removal

The `/calendar` route and `CalendarComponent` are removed once Google Calendar sync is live. Until then, the built-in calendar page remains as temporary infrastructure.

---

## Phase 11 — Google Maps Integration

### 11.1 — Address Autocomplete

- Add Google Maps Places Autocomplete to the delivery address field
- Use `@angular/google-maps`
- Validate and normalize the address on selection

### 11.2 — Maps Link Generation

- On save, generate a Google Maps URL for delivery orders:
  `https://www.google.com/maps/dir/?api=1&destination=<encoded address>`
- Store as `DeliveryMapsUrl` on the order
- Display as a clickable link in the edit form and in the Google Calendar event description (Phase 10)

---

## Phase 12 — Historical Order Bulk Import

*Paper forms scanned to Google Drive as PDFs. Filenames follow the pattern `OrderNumber - LastName FirstName.pdf`. No OCR needed — filename parsing creates a stub order and attaches the scan. Details can be manually filled in for recent orders later.*

### 12.1 — Bulk File Importer

> **⚠ Before building:** Audit the actual Google Drive folder to document exact filename patterns and multi-page file conventions before writing the parser.

**Filename patterns (to be confirmed):**
- Expected base pattern: `OrderNumber - LastName FirstName.pdf`
- Known variation exists — exact format to be confirmed before implementing the parser
- Some orders span multiple pages — determine naming convention for page 2+ files

**Backend — `POST /api/order/import-files`:**
- Accepts a multipart upload of multiple PDF files
- For each file:
  - Parse filename to extract `OrderNumber` and customer name (regex TBD after audit)
  - Create a minimal `Order` with only those two fields; skip all other field validation
  - Save the file as an `OrderAttachment` on the new order
  - Flag duplicates in the result — do not silently skip
- Return: created count, duplicate count, failed (unparseable) count, per-file result list

**Frontend — Management page (new collapsible section):**
- Multi-file picker or folder drag-and-drop
- Preview table of parsed filenames before submitting (order number, name, filename)
- Progress indicator during upload
- Results summary with duplicate and failure details

### 12.2 — Order Number Continuity

- Historical imports bypass `GetNewOrderNumber()` — use the explicit order number from the filename
- After import, `GetNewOrderNumber()` (`MAX + 1`) naturally continues from the highest imported number
- Duplicates are expected to be zero at import time; flag any that occur for manual review

---

## Phase 13 — Pricing

⚠ **Needs detailed design work before implementation.** Get full pricing requirements from the bakery before building this phase. Configuration, auto-calculation logic, and additional fields are all TBD.

Fields identified so far (placeholders added in Phase 5): Labor, Flavor Upgrade, Lookbook Price.

---

## Phase 14 — Mobile Optimization

*Post-launch, after all other features are implemented and stable.*

Responsive layout improvements for phone and tablet use in the shop. The MVP is explicitly desktop-only; this phase addresses mobile accessibility once the core product is stable.

---

## Phase 15 — Deployment & Hosting

*Phase 9 (Auth) must be complete before go-live.*

### 15.1 — Database

SQLite (`orders.db`). Zero installation on shop PC. `Database.Migrate()` called at startup — DB is auto-created/migrated on first run. `*.db` files excluded from git.

### 15.2 — Backup Strategy

- Windows Task Scheduler: nightly PowerShell script copying `orders.db` and `attachments/` to a backup location
- Retain last 30 days of daily backups
- Sync to network share, USB drive, or home PC via Tailscale

### 15.3 — Application Deployment

**Architecture:** Cloudflare Tunnel + subdomain CNAME

- App runs as a Windows Service on the shop PC (Kestrel on `localhost:5000`)
- `cloudflared` tunnel routes public traffic to the local port — no open firewall ports, works through any ISP/CGNAT
- Cloudflare handles HTTPS/TLS automatically
- **Production URL:** `https://orders.canonsburgcakecompany.com`

**Notes:**
- Shop PC not yet purchased
- Bakery domain is on Squarespace and used by Square and other commerce platforms — do not transfer. Add a CNAME record for the `orders` subdomain in Squarespace DNS pointing to the Cloudflare Tunnel hostname.

**One-time setup:**
1. Purchase shop PC; install Windows
2. Create Cloudflare account, install `cloudflared` on shop PC
3. `cloudflared tunnel create ccc-inventory`
4. Configure tunnel to forward to `http://localhost:5000`
5. Add `orders` CNAME in Squarespace DNS
6. Publish .NET app as self-contained `win-x64` Windows Service
7. Install and start the service with `sc.exe`

### 15.4 — Dev/Prod Configuration

- `appsettings.Production.json` for shop PC (Data Source path, Kestrel port)
- Frontend `environment.prod.ts`: `apiUrl: '/api'` (already set — app and API served from same origin in production)
- Angular build: `ng build --configuration production`

### 15.5 — Deployment Workflow

1. Developer commits to `main` on home PC
2. Shop PC pulls latest (`git pull`)
3. `deploy.ps1`: `ng build --configuration production` → `dotnet publish` → copy output → restart Windows Service
4. Database auto-migrates on startup

---

## Phase 16 — Polish

*Nice-to-haves that improve daily usability.*

- **Print order form:** Print-friendly view of a single order replicating the paper form layout
- **Customer history:** View all orders for a given customer by name/email

---

## Work Order / Priority Sequence

| Priority | Phase | Rationale |
|---|---|---|
| 1 | Phases 0–4.5 ✅ | Done |
| 2 | Phase 5 — Order Form Overhaul | Core UX; largest immediate need; model changes other phases depend on |
| 3 | Phase 6 — All Orders & Homepage | Depends on Phase 5 status system |
| 4 | Phase 7 — Bake Sheet Redesign | Mostly independent; high daily operational value |
| 5 | Phase 8 — Party Rentals | New independent feature |
| 6 | Phase 9 — Authentication | Required before deployment; PIN/audit system |
| 7 | Phase 10 — Google Calendar | Requires live domain (Phase 15 prereq) + Phase 5 Title field |
| 8 | Phase 11 — Google Maps | Complements delivery orders |
| 9 | Phase 12 — Historical Import | Useful but not blocking launch |
| 10 | Phase 13 — Pricing | Needs design work first |
| 11 | Phase 14 — Mobile Optimization | Post-launch |
| 12 | Phase 15 — Deployment | After Phase 9 auth |
| 13 | Phase 16 — Polish | Ongoing |

---

## Open Questions

1. **Google Calendar event format:** Confirm the exact event title format before implementing Phase 10. Current spec: `"OrderNumber - Title - FirstName LastName"`. Confirm this is correct.
