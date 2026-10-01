# Historical Order Import (Phase 12): design

Status: **design / not started**. This replaces the earlier "filename-only bulk import" plan for Phase 12.

## Goal

Bring the ~4,000 paper and early digital orders (≈ 4.5 years, #1 through roughly #4000) into the app so that:

- a customer's past orders show up on their customer page (repeat-customer history), and
- the customer list is built from historical contact info.

It is **not** a goal to rebuild every historical order field-for-field (cake tiers, per-layer flavors, etc.).
Each imported order gets a small set of key fields plus a readable summary, with the original scan attached.

Reviewers: Zak and Lindsey (owner). Both know the app well, so the review UI optimizes for speed (keyboard
driven) over hand-holding.

---

## Source material

Drive folder **order archives** (owner `info@canonsburgcakecompany.com`), subfolders `0001-1000`,
`1001-2000`, `2001-3000`, `3001-4000`, plus a few loose scans at the top level.

**File naming:** `NNNN - Customer Name.ext`. Extra pages are separate files with the same number:
`NNNN - page 2.jpg`, `NNNN - pg 2.jpg`, `NNNN - page 3.pdf`, `005 - z photo.jpg`. Number padding varies
(`005`, `060`, `509`, `1965`). There is a `0000 - …` file and a `171 - unknown`. **Group files by the leading number;
an order = all files sharing it.**

**File types:** `.jpg` / `.JPG` / `.jpeg` / `.png` / `.pdf`. Mostly phone photos of the form (some on a
counter, some slightly skewed, some with the next sheet visible underneath), some flatbed scans, and
screenshots for the earliest digital orders.

### Form versions observed (sampled 17 orders across the range)

Boundaries are approximate; versions overlap (paper v1 and the digital table were both in use in early 2022).
The extractor does **not** need to be told which version it's looking at.

| Era | Seen on | Format | Notes |
|---|---|---|---|
| **A: typed table** | #5, #30, #145, ~#135–#204 | Screenshot of a typed Google Doc table: Order Number, Due Date, Time, Pick up or Delivery, Location, Name, Email, Phone, Cost, Deposit, Form of payment, Paid in full, Flavor, Shape, Size, Quantity + two Notes boxes | Typed, so it's the easiest era. Inspiration photos pasted into the notes. Cancelled orders have a big red **CANCELED** over them. |
| **B: "ORDER" rolling-pin form** | #60, #80, #509 | Name / Email / Phone 1 / Phone 2 / Address; Order Date (+ "Date" on some printings); Pick up / Delivery; Type of orders checkboxes; Flavors / Shape / Size / Tiers / Quantity; Design dot grid; Delivery/PU Location; Cost / Deposit / Form of Payment / Paid in Full | Handwritten. |
| **B2** | #750 (Jan 2023) | Same as B but Flavors split into Cake / Filling / Icing Flavor | |
| **C: logo form** | #1250 (mid-2023) | Canonsburg Cake Company logo, ORDER #, NAME / EMAIL / PHONE, `*NO MARBLE*`, `*MICRO CANNOT BE DAIRY FREE*` | |
| **D: + admin block** | #1965, #2166, #2500, #2663, #2900 (2024–25) | Adds Confirmation text sent / Day-of text sent / Wedding contract sent; Total cost, Deposit amount, Deposit payment method + date/time, Final payment method + date/time, **Date order placed**, Paid in full; later "# of labor hours" box and a **Tasting** option | |
| **E: + pricing column** | #3300, #3500, #3904 (Dec 2025 →) | Adds a pricing column (Base ×4, Flavor upgrade, Stack, Board, Labor, Other, Delivery, Total) and Contract sent? | |

### Things the extractor has to handle (seen in the samples)

- **"ORDER DATE" means the due/event date.** The weekday written at the top ("Saturday") matches it. Era D+ has
  a separate **DATE ORDER PLACED** (often without a year: "Web 5/10", "phone 7/15", "9-12-24 in person").
- **Dates without a year or with unclear handwriting.** #509 reads "3/13" with "Saturday" at the top. Neither
  3/13/22 nor 3/13/23 is a Saturday, but **8/13/2022** is, and it falls between #145 (Apr 2022) and #750
  (Jan 2023). So: cross-check against the written weekday, and against the dates of nearby order numbers.
- **Order numbers are not in due-date order.** Weddings are booked far ahead (#2166 placed 9/12/24 for
  1/17/26; #2663 for 8/1/26). Nearby numbers are only a loose check on date placed, not on due date.
- **"Facebook" in the phone field** (#750, #1250) means the customer was contacted on Facebook Messenger, so
  there's often no phone number at all. Map that to `InitialContact`, not `CustPhone`. Similarly "Web", "phone",
  "in person" in Date Order Placed.
- **Cancelled / never-happened orders:** handwritten "Canceled" (#80), "CANCEL" (#2900), red stamp (#145),
  "never heard back" (#1965). A big diagonal line alone is **not** a cancellation (#60 has an X through the
  design box on a paid order).
- **Not real customers:** "Decorating Class" (#2500), "Hilltop" (recurring business, #510/#534/#577),
  "unknown" (#171), first name only ("Jackie", #3300).
- **Info lives outside the boxes:** sticky notes (#1250, #2166, #2900), "SEE BACK" pointing to page 2 (#60,
  #2663), price math scribbled in the design area, highlighter, crossed-out values.
- **Page 2+** is usually an inspiration photo (#1965 p2) and sometimes overflow details. Send every page of
  the order in the same extraction call.
- **Payment fields often hold a number** ("#273", "#466", "1397", "1556"), presumably a Square receipt or
  invoice number. Keep it as text.
- **Weddings** are identifiable ("WEDDING - 08/01/26", "Contract Sent 7/18/26", "Wedding contract sent? Yes").
- **Google Drive's own OCR is not enough.** For #3904 it extracted the text but split values from their labels
  and garbled the email. Field mapping needs a vision model.

---

## Decisions

### 1. Order numbers: reserved range, real numbers for imports

- **Production starts numbering at a reserved value** (4250 / 4500 / 5000; see below) so imported orders
  keep their paper numbers and never collide with new ones.
  - SQL Server: set the identity seed in the Phase 9 migration (`UseIdentityColumn(seed)`), or
    `DBCC CHECKIDENT ('Orders', RESEED, seed - 1)` once on the empty production DB.
  - SQLite (dev): explicit-key inserts just work; `rowid` continues from MAX + 1.
- **Imports insert with the explicit paper number.** On SQL Server that needs `SET IDENTITY_INSERT Orders ON`
  around the insert (in the same open connection/transaction). This is contained in the import endpoint.
  Duplicate numbers are rejected (409) so an order can't be imported twice.
- **No pre-created blank orders.** The import queue (below) is the to-do list. Blank `Order` rows would show up
  in All Orders, customer history and reports, and would stay blank for any number that was voided on paper.
- **Choosing the seed:** recent pace is ≈ 70 orders/month (#3300 Dec 2025 → #3904 Aug 2026), and paper keeps
  going until go-live. 4250 is ≈ 3 months of headroom past #4000, 4500 ≈ 7 months. Pick it from the paper
  order number at go-live plus a margin, or use 5000.

### 2. Key fields only, plus a summary

Extracted / reviewed per order:

| Field | → `Order` | Notes |
|---|---|---|
| Order # | `OrderNumber` | From the form; flag if it disagrees with the filename. |
| Due date + time | `OrderDateTime` | Year inferred when missing (weekday + nearby numbers). |
| Pickup / Delivery / Tasting | `OrderType` | |
| Location / address | `DeliveryLocation` | |
| Name | `CustName` | |
| Email | `CustEmail` | |
| Phone | `CustPhone` | Digits only; "Facebook" etc. go to `InitialContact`. |
| How they contacted us | `InitialContact` | Facebook / Web / Phone / In person / Email. |
| Title | `Title` | Short, generated: "2-tier baby shower cake", "Mini cupcakes (90)". |
| Summary | `Details` | Readable summary of everything ordered: flavors, sizes, quantities, design, sticky notes, price breakdown. Any text the model couldn't place goes here too, so nothing on the paper is lost. |
| Total cost | `TotalCost` | |
| Deposit | `DepositAmount` | |
| Paid in full | `PaidInFull` | |
| Date order placed | `DateOrderPlaced` | Falls back to the due date if unknown (column is non-nullable). |
| Cancelled | `CancelledFlag` + `CancellationReason` | Reason from the scan ("Never heard back") or "Cancelled (imported)". |
| Wedding | `IsWedding` | Ring icon in customer history. No `WeddingDetails` row is created for past weddings. |

Structured items (`Cakes`, `Cupcakes`, …) are **not** created for past orders. Past-dated orders are already
excluded from the Incomplete filter and the bake sheet, so the missing items don't cause warnings.

**Exception: future-dated orders.** Anything in the backlog with a due date after the import date (weddings
booked ahead, and everything from the last few weeks before go-live) is a live order. The review page flags
these and offers **Open full form** so they get entered completely (items, wedding details) and show up on the
bake sheet.

### 3. Customer matching (import only)

1. Email match (existing rule).
2. Else **phone match** on normalized digits.
3. Else a **name suggestion** (shown, never auto-linked): "Possible match: Jane Smith · 412-555-… · 3 orders".
   Accept/reject with one key.
4. No email and no phone → no customer link (as today), unless the reviewer picks one.

Imports **never update** an existing customer's name/phone/email. A 2022 form must not overwrite a
customer's current phone number. (Today `LinkOrCreateCustomerAsync` updates the phone on an email match; the
import path skips that.) New customers are created from the order's contact info.

Non-customers ("Decorating Class", internal events, "unknown") are imported unlinked. The extractor marks them
`isCustomer: false`. Recurring businesses ("Hilltop") are linked like any customer.

### 4. Imported marker

Add `ImportedAt` (`DateTime?`) to `Order`. Null for orders created in the app. Used for an "Imported" badge on
the order form and in customer history, for filtering, and so the audit log can record `Import` instead of
`Create`.

---

## Workflow

### Stage 1: queue + pre-extraction (background)

- **Ingest** (Management → Order Import → *Scan Drive folder*): list the archive folder recursively via the
  existing Google connection (`GoogleAuthService` already has the Drive scope; the bakery account owns the
  folder). Group files by leading number → one `ImportQueueItem` per order.
- **Extract** every queued order ahead of time with the Claude API (batch API: half price, results within
  hours), so reviewers never wait.

`ImportQueueItem`:

| Column | |
|---|---|
| `Id`, `OrderNumber` | Number parsed from the filenames. |
| `DriveFileIds` / `FileNames` (JSON) | All pages, in page order. |
| `Status` | Pending → Extracted → Imported / Skipped / Flagged / Failed. |
| `ExtractedJson` | Model output: the fields above, each with a confidence (high/medium/low), plus `formEra`, `isCustomer`, `isCancelled`, `isWedding`, `notes`. |
| `ReviewedBy`, `ReviewedAt`, `Note` | |

Extraction prompt essentials: the field table above; the era descriptions and gotchas from this doc (ORDER DATE
= due date, weekday cross-check, Facebook-as-phone, cancellation markers, sticky notes, SEE BACK); the
date of the nearest neighbouring orders already extracted, for year inference; structured JSON output.

**Rough cost:** each page is ≈ 1.5–2.5k image tokens. At ≈ 4–6k input + ≈ 1–2k output per order, 4,000
orders comes to roughly **$100–250** on the batch API, depending on model choice. Run a 50-order pilot
first and measure.

### Stage 2: review page (`/import`)

```
┌──────────────────────────────┬─────────────────────────────┐
│ scan (zoom, rotate, pages)   │ #2663 · Era D · ⚠ future     │
│                              │ Date      [08/01/2026 1:00p]│
│                              │ Type      [Delivery      ▾] │
│                              │ Name      [Dakota Brown    ]│
│                              │ Email     [dbrown5920@…    ]│
│                              │ Phone     [724-470-7061    ]│
│                              │ Customer  ● new  ○ match…   │
│                              │ Title     [Wedding cake, 4…]│
│                              │ Summary   [6/8/10/12 vanil…]│
│                              │ Total [520] Paid [✓] Wed [✓]│
│                              │ Cancelled [ ] Reason [     ]│
│                              │ [Save & Next ⏎] [Skip] [Flag]│
└──────────────────────────────┴─────────────────────────────┘
```

- Compact panel: not the full order form, and **no autosave**. One explicit save per order.
- Low-confidence fields highlighted amber; Tab jumps between them.
- Keyboard: Enter = Save & Next, `S` = Skip, `F` = Flag, `[` / `]` = page through the scan.
- **Save & Next:** create the order with its paper number, link/create the customer, copy every page of the scan
  into `attachments/{orderNumber}/` as `OrderAttachment`s, write an `Import` audit entry, mark the queue item
  Imported.
- **Open full form** (always available, highlighted for future-dated orders): save first, then open
  `/edit-order?orderNumber=…` to fill in items.
- Queue view: counts by status, filter by Flagged / low confidence / era / folder; pick up where you left off.
  Two reviewers can work at once; each takes the next unclaimed item.

Estimated review time: 15–30 s per order for most, longer for weddings and future orders. Roughly 15–35 hours
in total across both reviewers.

---

## Build order

0. **Pilot (no UI).** A script runs extraction on ~50 orders spread across all eras and prints the result next
   to the filename. Check field accuracy by hand, tune the prompt. Go/no-go on the approach and the model.
1. **Backend:** `ImportedAt` migration; `ImportQueueItem` table; Drive ingest; extraction job (batch);
   import endpoint (explicit order number, import-only customer matching with phone fallback, attachments
   from Drive).
2. **Review page:** split view, compact panel, keyboard flow, queue view.
3. **Polish:** reviewer claim/lock, re-extract a single item, a report of skipped/flagged items.

Prerequisite for production: the identity seed (Decision 1) must be set **before** the first real order is
created in prod.

## Open questions

- Go-live date and the paper order number at that point → seed value.
- Should cancelled quotes / "never heard back" orders be imported? (Proposed: yes, as cancelled. They still
  hold customer contact info.)
- Is `0000 - Tami Szyper` a real order, and what number should it get?
- Payment-field numbers ("#273", "1397"): Square receipt / invoice numbers? Worth a dedicated field, or fine
  as text in the summary?
- Recurring business customers ("Hilltop"): one customer record each, or tag them differently?
