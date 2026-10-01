# Wedding Contract: Google setup & template tokens

Wedding orders (the **Wedding Order** toggle on the order form) can generate the wedding contract as a
Google Doc. **Generate Contract** copies the template into the Wedding Contracts folder, names it
`YYYY/MM/DD - First Last - Wedding Contract` (event date + Bride/Groom #1), and fills in every
`{{token}}` from the order. If the order already has a contract, a dialog offers:

- **Overwrite**: creates a fresh contract and moves the previous one to the Drive trash (same name).
- **New Revision**: keeps the previous contract and adds `YYYY/MM/DD - REVISED - First Last - Wedding Contract`.

The existing workflow (send from Google Docs with eSignature, signed PDF comes back) is unchanged.

---

## 1. One-time Google Cloud setup

1. In [Google Cloud Console](https://console.cloud.google.com/), create a project (e.g. "CCC Orders").
2. **APIs & Services → Library**: enable **Google Drive API** and **Google Docs API**.
3. **APIs & Services → OAuth consent screen**:
   - User type: **External**. Add the bakery Google account as a test user while setting up.
   - Scopes: `.../auth/drive` and `.../auth/documents`.
   - **Before relying on it day-to-day, set Publishing status to "In production".** While the app is in
     "Testing", Google expires the refresh token after **7 days** and the connection silently stops
     working. In production, the consent screen shows an "unverified app" warning (expected; click
     *Advanced → continue*). No verification is needed for the bakery's own account.
4. **APIs & Services → Credentials → Create credentials → OAuth client ID**, type **Web application**.
   Add **Authorized redirect URIs**:
   - Development: `http://localhost:44401/api/google/callback` (the Angular dev server; note **http**,
     not https) and `https://localhost:7005/api/google/callback` (backend opened directly). The app builds
     the callback from the address in the browser's address bar, so register whichever you use.
   - Production: `https://orders.canonsburgcakecompany.com/api/google/callback`
5. Give the app the client ID/secret. Never commit them:
   - Development: `dotnet user-secrets set "Google:ClientId" "<id>"` and
     `dotnet user-secrets set "Google:ClientSecret" "<secret>"` (run in `CCCInventory/`).
   - Production: environment variables `Google__ClientId` and `Google__ClientSecret`.

## 2. App configuration and connecting

### Settings (the `"Google"` block in `CCCInventory/appsettings*.json`)

| Key | Meaning |
|---|---|
| `Google:ClientId` / `Google:ClientSecret` | OAuth client from step 1. Set via user-secrets (dev) or env vars (production), never in the JSON files |
| `Google:WeddingContractTemplateId` | The template Google Doc ID: the part of its URL between `/d/` and `/edit`. Already set to the dev template in `appsettings.Development.json` |
| `Google:WeddingContractFolderId` | *Optional, leave blank.* Folder for generated contracts; defaults to the template's own folder |
| `Google:RedirectUri` | *Optional, leave blank.* Defaults to `<address in the browser>/api/google/callback`; set it only to force a specific callback URL |

For local testing nothing in the JSON needs editing once the user-secrets from step 1 are set.

### Connect the bakery Google account (in the running CCCInventory app)

1. Run the app and log in.
2. Click **Management** in the top nav bar, scroll to the bottom, and expand
   **Google Integration (Wedding Contracts)**.
3. Click **Connect Google Account** and sign in as the **bakery account that owns the Wedding Contracts
   folder and template** (info@canonsburgcakecompany.com). Approve the permissions; on the
   "unverified app" warning choose *Advanced → continue*.
4. You return to Management with "Google account connected." Click **Check Contract Template**; it
   should report every token recognized.
5. Open a wedding order (ring icon in All Orders) and click **Generate Contract** in Wedding Details.

## 3. Preparing the template

Keep the real `00 - Template - Wedding Contract` untouched while developing. Make a copy named
**`9999 - Zak's Development Template - Wedding contract`** in the same Wedding Contracts folder (it
sorts to the bottom and is clearly not for staff use), point `Google:WeddingContractTemplateId` at it,
and add tokens to the copy. When going live, add the same tokens to the production template (or
promote the dev copy) and switch the ID.

The dev copy exists ([9999 - Zak's Development Template - Wedding contract](https://docs.google.com/document/d/1Idvu4Sh-i23018TFUaFIpJT8MhAi5_m8uc_suS7eEUc/edit))
with every token below already placed, and its ID is set in `appsettings.Development.json`. A filled
example made from seed order 35 is next to it ("9999 - Zak's Development SAMPLE - …").

Type tokens directly into the document text in place of the `______` blanks. Token names are
case-insensitive, and spaces inside the braces are fine (`{{ event_date }}`). Unknown tokens are left
as-is and reported after generation, and by the template check.

**eSignature boxes:** the boxes clients fill when signing are Google eSignature fields; the Docs API
can't write into them. For those questions, either leave the box alone (the client answers it), or
put the token next to the box so the answer the bakery already knows is pre-printed. Because those
app fields are optional, the token is blank when staff hasn't filled them in.

### Scalar tokens

| Token | Value | Contract spot |
|---|---|---|
| `{{order_number}}` | Order number | Header "Contract/Order Number" |
| `{{today}}` | Date generated | |
| `{{event_date}}` | *Saturday, October 17, 2026* | Date of Event |
| `{{reception_location}}` | Order's Reception Location / Delivery Address field | Location of Reception |
| `{{ceremony_same_location}}` | Yes / No | Ceremony at the same location? |
| `{{ceremony_time}}` / `{{reception_time}}` | *4:30 PM* | Ceremony / Reception Start Time |
| `{{partner1_name}}` / `{{partner1_phone}}` | Order customer name / phone | Bride/Groom #1 |
| `{{partner2_name}}` / `{{partner2_phone}}` | | Bride/Groom #2 |
| `{{email}}` | Order email | Email address |
| `{{day_of_title}}` / `{{day_of_name}}` / `{{day_of_phone}}` | | Day-of contact |
| `{{return_by_date}}` | Delivery/pickup date − 10 days | Return contract on or before |
| `{{cake_stand_size}}` | Largest tier + 2 (inches) | Cake stand at least ___ inches |
| `{{board_color}}` | White / Gold / Silver / Black | Board color |
| `{{cake_topper}}` / `{{flowers}}` | Yes / No | Topper / flowers questions |
| `{{flower_type}}` | *Live flowers/greens*, … | |
| `{{flowers_provided_by}}` | *Customer's contracted florist*, … | |
| `{{florist_name}}` / `{{florist_phone}}` / `{{florist_time}}` | | Florist line |
| `{{delivering}}` | Yes (Delivery) / No (Pickup) | CCC is delivering |
| `{{delivery_start}}` / `{{delivery_end}}` | *10:00 AM* / *12:00 PM* (delivery orders only) | Delivery time window |
| `{{delivery_address}}` | Order delivery address (delivery orders only) | Delivery Address |
| `{{venue_contact_name}}` / `{{venue_contact_phone}}` | | Contact Person for venue |
| `{{pickup_date}}` / `{{pickup_time}}` | Order date/time (pickup orders only) | Pickup Date / Time |
| `{{pickup_person_name}}` / `{{pickup_person_phone}}` | | Person picking up |
| `{{main_design_description}}` | Cake Design box (under Cake Details, wedding orders) | Main Cake: Description of Design |
| `{{main_servings_total}}` | Sum of tier servings | Total servings in tiered cake |
| `{{kitchen_cakes}}` | Yes / No (any sheet cakes) | Will additional kitchen cakes… |
| `{{kitchen_servings_total}}` | Servings sum of the sheet cakes | Total servings in kitchen cakes |
| `{{cupcake_design_description}}` | Cupcake Design box (under Cupcake Details, wedding orders) | Description of Cupcake Design |
| `{{cupcake_servings_total}}` | Sum of cupcake quantities | Total servings in cupcakes |
| `{{total_servings}}` | Total Servings field (falls back to the sum) | Total servings for entire order |
| `{{total_cost}}` / `{{deposit_amount}}` | *$980.00* | Cost breakdown |
| `{{deposit_date}}` / `{{final_payment_date}}` | Deposit / final payment dates | |
| `{{balance}}` | Total − deposit | Final payment balance |
| `{{final_due_date}}` | Event date − 10 days | Due by |

### Table-row tokens (cake and cupcake tables)

Put the tokens in **one row** of the table; that row is repeated once per item (rows are added
below it). Replace the fixed 6"/8"/10"/12" rows with a single token row.

- Main cake (all non-sheet tiers, smallest first): `{{main.size}}` (*8" round*), `{{main.servings}}`,
  `{{main.cake}}`, `{{main.filling}}`, `{{main.icing}}`, plus `{{main.shape}}`, `{{main.layers}}`
- Kitchen cakes (Quarter/Half Sheet tiers): same fields with `kitchen.`
- Cupcakes: `{{cupcake.qty}}`, `{{cupcake.size}}`, `{{cupcake.cake}}`, `{{cupcake.filling}}`, `{{cupcake.icing}}`

Example main-cake row: `{{main.size}} | {{main.servings}} | {{main.cake}} | {{main.filling}} | {{main.icing}}`

Servings come from the template's chart (6"=12, 8"=24, 10"=38, 12"=56 round; ½ sheet=70, ¼ sheet=33);
other sizes/shapes leave the cell blank.

### Option lists (`{{mark:…}}`)

Put `{{mark:<token>=<value>}}` at the **start** of each option line, e.g. `{{mark:board_color=White}}White [box]`,
`{{mark:flower_type=Live}}[box] Live flowers/greens`. When the order has a value for that token, the matching
line becomes `✔ White` (its client box removed) and the other option lines are deleted. With no value, every
line stays as-is for the client. Values match either the stored option (`Live`, `Fake`, `Buttercream`,
`Florist`, `Customer`, `CCC`, `N/A`) or the displayed text. "Flowers: No" answers both flower lists as N/A.

### Conditional sections (`{{if:…}}` / `{{endif:…}}`)

Wrap a part of the template in `{{if:<name>}}` … `{{endif:<name>}}`. When the condition doesn't apply to the
order, everything from the paragraph holding `{{if:…}}` through the paragraph holding `{{endif:…}}` is deleted;
when it does, only the two markers are removed. Put the markers inline (e.g. at the start of the section
heading and at the end of its "Initial:" line), or put `{{if:…}}` on the blank line above a section so the
separator goes with it.

| Name | Applies when |
|---|---|
| `delivery` | Order type is Delivery (or not chosen yet) |
| `pickup` | Order type is Pickup (or not chosen yet) |
| `florist` | Flowers provided by the customer's florist (or not answered yet) |
| `flowers` | Flowers/greenery is Yes (or not answered yet) |
| `kitchen_cakes` | The order has sheet (kitchen) cakes |
| `cupcakes` | The order has cupcakes |

### Client boxes next to tokens

A token placed directly before a client (eSignature) box, `{{ceremony_time}} [box]`, removes the box when the
order has a value, so the contract never shows both a value and a box. Blank tokens leave the box.

`GET /api/weddingcontract/{orderNumber}/preview` shows the exact values an order would produce.
