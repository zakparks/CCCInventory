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
- Wedding-only fields live in `WeddingDetails` (1:1 with `Order`, keyed by `OrderNumber`); everything shared (dates, customer, items, pricing) stays on `Order`. `Order.IsWedding` is the flag. `OrderController.UpdateOrder` never overwrites the server-managed `Contract*` fields.

## Seed Data

`CCCInventory/Data/SeedData.cs` must be kept in sync with the current data model. After any phase that adds fields to `Order`, `WeddingDetails`, `Cake`, `Cupcake`, `Cookie`, `Pupcake`, or `OtherItem`, update `SeedData.cs` to:
- Populate every new field on all existing seed orders (no blank/default values that mask functionality)
- Include at least one order that exercises the new field
- Keep orders distributed across **three weeks**: one week prior (archived), current week (mixed active/archived), one week ahead (active), so all five status filters (Active, Incomplete, Ready for Pickup, Cancelled, Archived) are always exercised from a fresh seed

---

## Current State (as of 2026-10-01)

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
| Wedding | Wedding Orders — "Wedding Order" toggle on the order form; `WeddingDetails` entity (1:1 with Order) for contract-only fields (event date, ceremony/reception times, Bride/Groom #2, day-of title, venue/pickup contacts, board color, topper, flowers/florist, delivery window end, total servings); reception location = `DeliveryLocation`, contract return-by = delivery date − 10 days; Cake/Cupcake Design boxes (wedding only) under the item sections feed the contract's design descriptions; wedding-mode labels (Bride/Groom, Reception Location / Delivery Address, Delivery Start/End beside the date); day-of contact name/phone shown only in Wedding Details; (?) tooltip explaining Event Date vs Delivery/Pickup Date; staff-filled contract blanks are required (red + Incomplete, still saveable); ring icon with "Wedding Order" tooltip on All Orders, Bake Sheet, customer history, order form; **Generate Contract** copies the Google Doc template into Drive and fills `{{tokens}}` (Overwrite → trash old / New Revision → `YYYY/MM/DD - REVISED - …`); answered questions lose their client box, chosen options get ✔ on the left and the other options are removed; delivery/pickup/florist/kitchen/cupcake sections and "(Choose ONE…)" prompts are removed when they don't apply (`{{if:x}}…{{endif:x}}`); cake and cupcake inspiration photos (one image attachment each, picked under the Design boxes; `CakePhotoAttachmentId` / `CupcakePhotoAttachmentId`) are inserted via a temporary public Drive copy that is deleted right after; Management → Google Integration (connect account, template token check). Setup + token reference: `docs/wedding-contract-template.md` |
| Form UX | Order form: Order # + Title on one row; Order Information laid out row-by-row so Tab goes name → phone → email → initial contact → date → time → location (Details, radios, toggles skip Tab); `app-time-input` (type any time — "3", "330p", "15:30", "noon" — or pick hour / 15-min / AM-PM; bare 1–6 = PM, 7–11 = AM); Pickup/Delivery labels for every order; new cake tier copies the previous tier except size; Signature Cupcakes button blue |
| 14 | Customer Profiles — `Customer` entity (FirstName, LastName, Email, Phone); `CustomerId` FK on Order (nullable, SetNull on delete); `CustomerController` (list, detail, search, CRUD, merge); customer link/create logic on order save (email as unique key); autocomplete dropdown on `custName` field in order form; `/customers` list page; `/customers/:id` detail/edit page with order history; "New Order" from customer pre-fills contact fields; seed customers linked to seed orders; customer merge (re-points all orders to kept record, deletes duplicate) |

### Known Gaps in Completed Phases

None — all known gaps resolved as of 2026-04-11.

### Calendar UI

`/calendar` route and `CalendarComponent` are **kept in the codebase** but hidden from the UI (nav link and homepage card removed as of 2026-04-10). The route is still directly navigable. Removed entirely when Phase 10 ships post-launch.

---

## Remaining Phases

Phases are numbered in execution order.

---

### Phase 9 — Deployment & Hosting *(go-live blocker)* ← NEXT

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

**Pre-release TODOs:**
- **Pricing** — incorporate the shop's pricing matrix into the app logic for all orders (see Phase 13; get the matrix from the bakery).
- **Order number seed** — set the production `Orders` identity seed above the paper order range (≈ 70 orders/month; e.g. 4500 or 5000, decided from the paper number at go-live) **before the first real order is created**, so historical orders can be imported with their own numbers (Phase 12).

**Wedding contract / Google OAuth checks at deployment** (see `docs/wedding-contract-template.md`):
- OAuth consent screen **Publishing status must be "In production"**, not "Testing" — in Testing, Google expires the refresh token after 7 days and contract generation silently stops (users see an "unverified app" warning on connect; that's expected).
- Add `https://orders.canonsburgcakecompany.com/api/google/callback` as an Authorized redirect URI; set `Google__ClientId` / `Google__ClientSecret` env vars; the callback URL is built from the browser's address, so `Google:RedirectUri` normally stays blank.
- Point `Google:WeddingContractTemplateId` at the **production** template (tokens added), not `9999 - Zak's Development Template - Wedding contract`.
- Reconnect Google on Management as the bakery account and run **Check Contract Template**.

**One-time setup checklist:**
1. Purchase bakery PC; install Windows + SQL Server Express
2. Install Tailscale on both home PC and bakery PC
3. Create Cloudflare account; install `cloudflared` on bakery PC; create tunnel
4. Add `orders` CNAME in Squarespace DNS
5. Register self-hosted GitHub Actions runner on bakery PC
6. Publish .NET app as self-contained `win-x64` Windows Service; install with `sc.exe`
7. Configure Google OAuth redirect URI once domain is live (prerequisite for Phase 10)

### Wedding contract follow-ups *(open)*

- **Inspiration photos — verify on the bakery account**: first real Generate Contract with a photo confirms the bakery Google account allows "Anyone with the link" sharing (needed briefly for each photo). If a Workspace policy blocks it, fall back to a short-lived signed URL served by the app once it is public (Phase 9).

### Phase 10 — Google Calendar Integration *(post-launch)*

**Prerequisites:** Cloudflare Tunnel + domain live before implementing — OAuth redirect URI must be a public HTTPS URL.
- Production URL: `https://orders.canonsburgcakecompany.com`
- OAuth redirect URI: `https://orders.canonsburgcakecompany.com/api/google/callback`

**Event format:**
- Title: `"OrderNumber - Title - FirstName LastName"`
- Color key: Blue=cakes, Purple=cupcakes/cookies/pupcakes (mixed non-cake orders use Purple), Green=ready for pickup, Red=cancelled/archived, Yellow=delivery. Cakes take color priority over all others.
- Timing: orders stack from midnight on their due date in creation order, 30 min each. Party Rentals appear at actual scheduled start time.

**Implementation:** Add `GoogleCalendarEventId` to `Order` + migration; `GoogleCalendarService` (create/update/delete). Reuse the existing Google OAuth from the wedding-contract feature (`GoogleAuthService`, `GoogleController` `/authorize-url`, `/callback`, `/status`, `/disconnect`, token in `GoogleTokens`) — add the Calendar scope to `GoogleAuthService.Scopes` (users must reconnect once); hook into `OrderController` on every create/update/archive.

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

### Phase 12 — Historical Order Import *(post-launch)*

**Full design: `docs/historical-order-import.md`** (form eras, extraction gotchas, data mapping, review UI, build order, open questions).

~4,000 orders (#1–~#4000, ≈ 4.5 years) in the Drive folder **order archives** (`0001-1000` … `3001-4000`), one file per page named `NNNN - Name.ext` / `NNNN - page 2.ext` (jpg/jpeg/png/pdf; group by leading number). Goal is customer history + contact info, not field-for-field rebuilds.

- **Pipeline:** Drive ingest → `ImportQueueItem` per order → Claude vision extraction of key fields (batch, pre-run) → `/import` review page (scan beside a compact key-field panel, no autosave, keyboard-driven, Save & Next) → order created with its **paper order number**, scan pages attached, `Import` audit entry.
- **Key fields only:** date/time, pickup/delivery/location, name/email/phone, initial contact, generated Title, readable summary in `Details`, total/deposit/paid, date placed, cancelled + reason, IsWedding. No cake/cupcake items for past orders; **future-dated** imports get "Open full form".
- **Order numbers:** prod identity seed starts above the paper range (seed chosen at go-live — see Phase 9); imports insert explicit numbers (`IDENTITY_INSERT` on SQL Server). No placeholder rows.
- **Customers (import path only):** match by email, then normalized phone, then name *suggestion*; imports never overwrite an existing customer's contact info.
- New `Order.ImportedAt` (nullable) marks imported orders.

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
- Historical imports (Phase 12): customers are created/linked during import (email, then phone fallback); see `docs/historical-order-import.md`
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
