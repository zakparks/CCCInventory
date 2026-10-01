using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Drive.v3;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace CCCInventory.Services
{
    public class ContractResult
    {
        public string DocId { get; set; } = "";
        public string Url { get; set; } = "";
        public string Name { get; set; } = "";
        public List<string> Warnings { get; set; } = [];
    }

    public class ContractException(string message) : Exception(message);

    public static class ContractErrors
    {
        // What to tell the user about a failed Google call, or null for an unexpected error (a real 500)
        public static string? UserMessage(Exception ex) => ex switch
        {
            ContractException => ex.Message,
            // The saved Google login was revoked or expired (in OAuth "Testing" mode, after 7 days)
            Google.Apis.Auth.OAuth2.Responses.TokenResponseException =>
                "The Google connection has expired. Reconnect Google on the Management page and try again.",
            Google.GoogleApiException => $"Google API error: {ex.Message}",
            _ => null
        };
    }

    // An inspiration photo for the contract: Token is "cake_photo" or "cupcake_photo".
    public record ContractPhoto(string Token, string FilePath, string ContentType, string FileName);

    // Fills the wedding contract Google Doc template from an order.
    //
    // The template holds {{tokens}} (see docs/wedding-contract-template.md):
    //   {{event_date}}                   scalar value
    //   {{main.size}} / {{cupcake.qty}}  table-row tokens; the row they sit in is repeated per item
    //   {{mark:board_color=Gold}}        put at the start of an option line. When board_color has a value,
    //                                    the matching line gets "✔ " and loses its client box, and the
    //                                    other option lines are removed; with no value all lines stay.
    //   {{if:pickup}} … {{endif:pickup}}  conditional section: everything from the paragraph holding
    //                                    {{if:x}} through the paragraph holding {{endif:x}} is deleted when
    //                                    x doesn't apply; otherwise just the markers are removed (see
    //                                    Conditions for the names).
    //   {{cake_photo}} / {{cupcake_photo}} the chosen inspiration photo, inserted as an image after the
    //                                    text fill (see InsertPhotosAsync)
    // A scalar token directly before a client (eSignature) box, e.g. "{{ceremony_time}} [box]", removes
    // the box when it has a value, so the contract never shows both data and a box to fill.
    // Token names are case-insensitive and may contain spaces inside the braces.
    public class WeddingContractService
    {
        private const string Check = "✔ ";
        // Stand-in for a client (eSignature) box, an inline object, in ParagraphText
        private const char ClientBox = '\uFFFC';
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");
        private static readonly Regex TokenRegex = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled);
        private static readonly string[] RowGroups = ["main", "kitchen", "cupcake"];

        // Servings for a 1"x2" slice, from the contract template.
        private static readonly Dictionary<string, int> RoundServings = new()
        {
            ["6"] = 12, ["8"] = 24, ["10"] = 38, ["12"] = 56
        };
        private static readonly Dictionary<string, int> SheetServings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Half Sheet"] = 70, ["Quarter Sheet"] = 33
        };

        private readonly GoogleAuthService _google;

        public WeddingContractService(GoogleAuthService google)
        {
            _google = google;
        }

        // ── Naming ────────────────────────────────────────────────────────────

        public static string BuildFileName(Order order, bool revised)
        {
            var date = order.WeddingDetails?.EventDate ?? order.OrderDateTime;
            var datePart = date?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? "Date TBD";
            var name = string.IsNullOrWhiteSpace(order.CustName) ? "Unknown" : order.CustName.Trim();
            return revised
                ? $"{datePart} - REVISED - {name} - Wedding Contract"
                : $"{datePart} - {name} - Wedding Contract";
        }

        public static bool IsRevisedName(string? name) =>
            name != null && name.Contains(" - REVISED - ", StringComparison.Ordinal);

        // ── Generate ──────────────────────────────────────────────────────────

        private async Task<(string TemplateId, DriveService Drive, DocsService Docs)> ConnectAsync(CancellationToken ct)
        {
            var templateId = _google.Settings.WeddingContractTemplateId;
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ContractException("No wedding contract template is configured (Google:WeddingContractTemplateId).");
            var services = await _google.CreateServicesAsync(ct)
                ?? throw new ContractException("Google is not connected. Connect it on the Management page.");
            return (templateId, services.Drive, services.Docs);
        }

        // mode: "overwrite" replaces the current contract (new doc, old one moved to Drive trash);
        //       "revision" creates an additional REVISED doc and leaves the current one alone.
        public async Task<ContractResult> GenerateAsync(Order order, string mode,
            IReadOnlyList<ContractPhoto> photos, CancellationToken ct)
        {
            var (templateId, drive, docs) = await ConnectAsync(ct);

            var details = order.WeddingDetails ?? new WeddingDetails();
            var existingId = details.ContractDocId;
            bool revised = mode == "revision" || (mode == "overwrite" && IsRevisedName(details.ContractDocName));
            var name = BuildFileName(order, revised);

            // 1. Copy the template
            var copyRequest = drive.Files.Copy(new DriveFile
            {
                Name = name,
                Parents = string.IsNullOrWhiteSpace(_google.Settings.WeddingContractFolderId)
                    ? null
                    : [_google.Settings.WeddingContractFolderId]
            }, templateId);
            copyRequest.SupportsAllDrives = true;
            copyRequest.Fields = "id, name, webViewLink, parents";
            var copy = await copyRequest.ExecuteAsync(ct);

            var result = new ContractResult
            {
                DocId = copy.Id,
                Name = copy.Name,
                Url = copy.WebViewLink ?? $"https://docs.google.com/document/d/{copy.Id}/edit"
            };

            // 2. Fill it
            var raw = BuildRawValues(order);
            result.Warnings.AddRange(await FillAsync(docs, copy.Id, BuildValues(order), raw, BuildRows(order), ct));

            // 3. Inspiration photos
            var chosen = PhotoTokens.Where(t => raw.GetValueOrDefault(t) is { Length: > 0 }).ToHashSet();
            try
            {
                result.Warnings.AddRange(await InsertPhotosAsync(drive, docs, copy.Id, copy.Parents?.FirstOrDefault(),
                    order.OrderNumber, photos, chosen, ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The contract itself is filled; don't lose it over a photo
                result.Warnings.Add($"The inspiration photos could not be added: {ex.Message}");
            }

            // 4. Overwrite → trash the previous contract
            if (mode == "overwrite" && !string.IsNullOrEmpty(existingId))
            {
                try
                {
                    var trash = drive.Files.Update(new DriveFile { Trashed = true }, existingId);
                    trash.SupportsAllDrives = true;
                    await trash.ExecuteAsync(ct);
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"The new contract was created, but the previous one could not be moved to the trash: {ex.Message}");
                }
            }

            return result;
        }

        // ── Template check ────────────────────────────────────────────────────

        public async Task<object> CheckTemplateAsync(CancellationToken ct)
        {
            var (templateId, _, docs) = await ConnectAsync(ct);
            var doc = await docs.Documents.Get(templateId).ExecuteAsync(ct);
            var found = AllText(doc).SelectMany(t => TokenRegex.Matches(t).Select(m => Normalize(m.Groups[1].Value)))
                .Distinct().OrderBy(t => t).ToList();
            var known = KnownTokens();
            var unknown = found.Where(t => !IsKnownToken(t, known)).ToList();
            var unused = known.Where(k => !found.Contains(k) && !found.Any(f => f.StartsWith($"mark:{k}="))).ToList();
            return new { templateName = doc.Title, found, unknown, unused };
        }

        public static List<string> KnownTokens()
        {
            var tokens = BuildValues(new Order { WeddingDetails = new WeddingDetails() }).Keys.ToList();
            tokens.AddRange(RowFields.SelectMany(g => g.Value.Select(f => $"{g.Key}.{f}")));
            tokens.AddRange(PhotoTokens);
            return tokens.OrderBy(t => t).ToList();
        }

        private static bool IsKnownToken(string token, List<string> known) =>
            known.Contains(token) ||
            (token.StartsWith("mark:") && token.Contains('=') && known.Contains(token[5..token.IndexOf('=')].Trim())) ||
            (ConditionName(token) is { } name && ConditionNames.Contains(name));

        // ── Conditional sections ──────────────────────────────────────────────

        public static readonly string[] ConditionNames =
        [
            "delivery", "pickup", "florist", "flowers", "kitchen_cakes", "cupcakes",
            "choose_board_color", "choose_flower_type", "choose_flowers_provided_by",
            "cake_photo", "cupcake_photo"
        ];

        // "if:pickup" / "endif:pickup" → "pickup"; anything else → null
        private static string? ConditionName(string token) =>
            token.StartsWith("if:") ? token[3..].Trim() : token.StartsWith("endif:") ? token[6..].Trim() : null;

        // Which conditional sections apply. With no order type chosen, both delivery and pickup stay.
        public static Dictionary<string, bool> Conditions(Dictionary<string, string> values, Dictionary<string, string> rawValues) => new()
        {
            ["delivery"] = values.GetValueOrDefault("delivering") != "No",
            ["pickup"] = values.GetValueOrDefault("delivering") != "Yes",
            ["florist"] = rawValues.GetValueOrDefault("flowers_provided_by") is "Florist" or "" or null,
            ["flowers"] = values.GetValueOrDefault("flowers") != "No",
            ["kitchen_cakes"] = values.GetValueOrDefault("kitchen_cakes") != "No",
            ["cupcakes"] = values.GetValueOrDefault("cupcake_servings_total") != "",
            // "(Choose ONE option below)" prompts: only while nothing has been chosen
            ["choose_board_color"] = string.IsNullOrEmpty(rawValues.GetValueOrDefault("board_color")),
            ["choose_flower_type"] = string.IsNullOrEmpty(rawValues.GetValueOrDefault("flower_type")),
            ["choose_flowers_provided_by"] = string.IsNullOrEmpty(rawValues.GetValueOrDefault("flowers_provided_by")),
            ["cake_photo"] = !string.IsNullOrEmpty(rawValues.GetValueOrDefault("cake_photo")),
            ["cupcake_photo"] = !string.IsNullOrEmpty(rawValues.GetValueOrDefault("cupcake_photo")),
        };

        // Body ranges to delete for conditional sections that don't apply.
        //  - {{if:x}} and {{endif:x}} in different paragraphs: from the start of the {{if}} paragraph to the
        //    end of the {{endif}} paragraph (whole paragraphs, tables between them included).
        //  - both in the same paragraph: just the markers and the text between them (inline), unless they
        //    wrap the whole paragraph, which is then removed like a multi-paragraph section.
        public static List<(int Start, int End)> FindRemovedSections(Document doc, Dictionary<string, bool> conditions)
        {
            var removed = new List<(int Start, int End)>();
            var body = doc.Body?.Content ?? [];
            var open = new Dictionary<string, (int Element, int ParaStart, int TokenStart)>();
            for (int e = 0; e < body.Count; e++)
            {
                if (body[e].Paragraph == null || body[e].StartIndex == null || body[e].EndIndex == null) continue;
                int baseIndex = body[e].StartIndex!.Value;
                var text = ParagraphText(body[e].Paragraph);
                foreach (Match m in TokenRegex.Matches(text))
                {
                    var token = Normalize(m.Groups[1].Value);
                    var name = ConditionName(token);
                    if (name == null || !conditions.ContainsKey(name)) continue;
                    if (token.StartsWith("if:"))
                    {
                        open.TryAdd(name, (e, baseIndex, baseIndex + m.Index));
                    }
                    else if (open.Remove(name, out var start) && !conditions[name])
                    {
                        // Same paragraph, but the markers wrap all of it: remove the paragraph itself
                        bool wrapsParagraph = start.TokenStart == baseIndex && m.Index + m.Length == text.TrimEnd('\n').Length;
                        if (start.Element == e && !wrapsParagraph)
                        {
                            removed.Add((start.TokenStart, baseIndex + m.Index + m.Length));
                        }
                        else
                        {
                            removed.Add((start.ParaStart, ParagraphDeleteEnd(body, e)));
                        }
                    }
                }
            }
            // A removal inside another (e.g. a "(Choose ONE…)" prompt inside a removed flowers block) is covered by it
            return removed.Where(r => !removed.Any(o => o != r && o.Start <= r.Start && r.End <= o.End)).ToList();
        }

        // Where deleting body paragraph e (through its end) stops: the body's last paragraph, and one right
        // before a table, must keep its newline
        private static int ParagraphDeleteEnd(IList<StructuralElement> body, int e) =>
            body[e].EndIndex!.Value - (e == body.Count - 1 || body[e + 1].Table != null ? 1 : 0);

        private static Request DeleteRange(int start, int end) => new()
        {
            DeleteContentRange = new DeleteContentRangeRequest
            {
                Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = start, EndIndex = end }
            }
        };

        // Paragraph text with one placeholder character per index of each non-text element (inline
        // objects such as client boxes become \uFFFC), so string positions map to document indices.
        private static string ParagraphText(Paragraph para)
        {
            var sb = new StringBuilder();
            foreach (var pe in para.Elements ?? [])
            {
                if (pe.TextRun != null) sb.Append(pe.TextRun.Content);
                else sb.Append(pe.InlineObjectElement != null ? ClientBox : '\uFFFD',
                    Math.Max(1, (pe.EndIndex ?? 0) - (pe.StartIndex ?? 0)));
            }
            return sb.ToString();
        }

        // ── Values ────────────────────────────────────────────────────────────

        private static string Normalize(string token) => token.Trim().ToLowerInvariant();

        private static string Date(DateTime? d, string format = "MMMM d, yyyy") => d?.ToString(format, Us) ?? "";

        private static string Money(double? v) => v?.ToString("C", Us) ?? "";

        private static string YesNo(bool? b) => b == null ? "" : b.Value ? "Yes" : "No";

        private static string Time(string? hhmm) =>
            TimeOnly.TryParseExact(hhmm ?? "", "HH:mm", out var t) ? t.ToString("h:mm tt", Us) : "";

        private static string OrderTime(DateTime? dt) =>
            dt == null || dt.Value.TimeOfDay == TimeSpan.Zero ? "" : dt.Value.ToString("h:mm tt", Us);

        private static string FlowerTypeText(string? v) => v switch
        {
            "Live" => "Live flowers/greens",
            "Fake" => "Fake flowers/greens",
            "Buttercream" => "Buttercream Flowers",
            _ => v ?? ""
        };

        private static string ProvidedByText(string? v) => v switch
        {
            "Florist" => "Customer's contracted florist",
            "Customer" => "Customer sourcing their own flowers/greens",
            "CCC" => "Canonsburg Cake Company",
            _ => v ?? ""
        };

        private static bool IsSheet(Cake c) => c.TierSize?.Contains("Sheet", StringComparison.OrdinalIgnoreCase) == true;

        private static int? TierInches(Cake c)
        {
            var m = Regex.Match(c.TierSize ?? "", @"^\s*(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : null;
        }

        public static int? Servings(Cake c)
        {
            if (SheetServings.TryGetValue(c.TierSize?.Trim() ?? "", out var s)) return s;
            var inches = TierInches(c);
            if (inches != null && string.Equals(c.CakeShape, "Round", StringComparison.OrdinalIgnoreCase)
                && RoundServings.TryGetValue(inches.Value.ToString(), out var r)) return r;
            return null;
        }

        private static List<Cake> MainCakes(Order o) =>
            (o.Cakes ?? []).Where(c => !IsSheet(c)).OrderBy(c => TierInches(c) ?? int.MaxValue).ToList();

        private static List<Cake> KitchenCakes(Order o) => (o.Cakes ?? []).Where(IsSheet).ToList();

        public static Dictionary<string, string> BuildValues(Order o)
        {
            var w = o.WeddingDetails ?? new WeddingDetails();
            bool delivery = o.OrderType == "Delivery";
            bool pickup = o.OrderType == "Pickup";

            var main = MainCakes(o);
            var kitchen = KitchenCakes(o);
            int mainServings = main.Sum(c => Servings(c) ?? 0);
            int kitchenServings = kitchen.Sum(c => Servings(c) ?? 0);
            int cupcakeServings = (o.Cupcakes ?? []).Sum(c => c.CupcakeQuantity);
            int? largestTier = main.Select(TierInches).Where(i => i != null).Max();
            var eventDate = w.EventDate ?? o.OrderDateTime;

            string Count(int n) => n > 0 ? n.ToString(Us) : "";

            return new Dictionary<string, string>
            {
                ["order_number"] = o.OrderNumber > 0 ? o.OrderNumber.ToString(Us) : "",
                ["today"] = Date(DateTime.Today),

                // Event
                ["event_date"] = Date(eventDate, "dddd, MMMM d, yyyy"),
                ["reception_location"] = o.DeliveryLocation ?? "",
                ["ceremony_same_location"] = YesNo(w.CeremonySameLocation),
                ["ceremony_time"] = Time(w.CeremonyTime),
                ["reception_time"] = Time(w.ReceptionTime),

                // Contacts
                ["partner1_name"] = o.CustName ?? "",
                ["partner1_phone"] = o.CustPhone ?? "",
                ["partner2_name"] = w.Partner2Name ?? "",
                ["partner2_phone"] = w.Partner2Phone ?? "",
                ["email"] = o.CustEmail ?? "",
                ["day_of_title"] = w.DayOfContactTitle ?? "",
                ["day_of_name"] = o.SecondaryName ?? "",
                ["day_of_phone"] = o.SecondaryPhone ?? "",

                // Contract
                ["return_by_date"] = o.OrderDateTime != null ? Date(o.OrderDateTime.Value.AddDays(-10)) : "",

                // Display / adornments
                ["cake_stand_size"] = largestTier != null ? (largestTier.Value + 2).ToString(Us) : "",
                ["board_color"] = w.CakeBoardColor ?? "",
                ["cake_topper"] = YesNo(w.CakeTopper),
                ["flowers"] = YesNo(w.HasFlowers),
                ["flower_type"] = FlowerTypeText(NoFlowers(w) ? "N/A" : w.FlowerType),
                ["flowers_provided_by"] = ProvidedByText(NoFlowers(w) ? "N/A" : w.FlowersProvidedBy),
                ["florist_name"] = w.FloristName ?? "",
                ["florist_phone"] = w.FloristPhone ?? "",
                ["florist_time"] = Time(w.FloristDeliveryTime),

                // Delivery
                ["delivering"] = delivery ? "Yes" : pickup ? "No" : "",
                ["delivery_start"] = delivery ? OrderTime(o.OrderDateTime) : "",
                ["delivery_end"] = delivery ? Time(w.DeliveryWindowEnd) : "",
                ["delivery_address"] = delivery ? o.DeliveryLocation ?? "" : "",
                ["venue_contact_name"] = w.VenueContactName ?? "",
                ["venue_contact_phone"] = w.VenueContactPhone ?? "",

                // Pickup
                ["pickup_date"] = pickup ? Date(o.OrderDateTime) : "",
                ["pickup_time"] = pickup ? OrderTime(o.OrderDateTime) : "",
                ["pickup_person_name"] = w.PickupPersonName ?? "",
                ["pickup_person_phone"] = w.PickupPersonPhone ?? "",

                // Cake description
                ["main_design_description"] = w.MainCakeDesignDescription ?? "",
                ["main_servings_total"] = Count(mainServings),
                ["kitchen_cakes"] = kitchen.Count > 0 ? "Yes" : "No",
                ["kitchen_servings_total"] = Count(kitchenServings),
                ["cupcake_design_description"] = w.CupcakeDesignDescription ?? "",
                ["cupcake_servings_total"] = Count(cupcakeServings),
                ["total_servings"] = w.TotalServings?.ToString(Us) ?? Count(mainServings + kitchenServings + cupcakeServings),

                // Cost
                ["total_cost"] = Money(o.TotalCost),
                ["deposit_amount"] = Money(o.DepositAmount),
                ["deposit_date"] = Date(o.DepositDateTime),
                ["balance"] = o.TotalCost != null ? Money(o.TotalCost - (o.DepositAmount ?? 0)) : "",
                ["final_due_date"] = eventDate != null ? Date(eventDate.Value.AddDays(-10)) : "",
                ["final_payment_date"] = Date(o.FinalPaymentDateTime),
            };
        }

        // Raw values for {{mark:key=Value}} so a mark can match either the stored value ("Live")
        // or the displayed text ("Live flowers/greens").
        public static Dictionary<string, string> BuildRawValues(Order o)
        {
            var w = o.WeddingDetails ?? new WeddingDetails();
            return new Dictionary<string, string>
            {
                ["board_color"] = w.CakeBoardColor ?? "",
                ["flower_type"] = NoFlowers(w) ? "N/A" : w.FlowerType ?? "",
                ["flowers_provided_by"] = NoFlowers(w) ? "N/A" : w.FlowersProvidedBy ?? "",
                // Chosen inspiration photo attachment ids (the images go in after the text fill)
                ["cake_photo"] = w.CakePhotoAttachmentId?.ToString(Us) ?? "",
                ["cupcake_photo"] = w.CupcakePhotoAttachmentId?.ToString(Us) ?? "",
            };
        }

        // "No flowers" answers the follow-up questions too: both become N/A
        private static bool NoFlowers(WeddingDetails w) => w.HasFlowers == false;

        private static readonly Dictionary<string, string[]> RowFields = new()
        {
            ["main"] = ["size", "shape", "layers", "servings", "cake", "filling", "icing"],
            ["kitchen"] = ["size", "shape", "layers", "servings", "cake", "filling", "icing"],
            ["cupcake"] = ["qty", "size", "cake", "filling", "icing"],
        };

        private static string CakeFlavorText(Cake c)
        {
            if (!string.IsNullOrEmpty(c.LayerFlavors))
            {
                try
                {
                    var layers = JsonSerializer.Deserialize<List<string>>(c.LayerFlavors)?
                        .Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToList();
                    if (layers is { Count: > 0 }) return string.Join(" / ", layers);
                }
                catch (JsonException) { }
            }
            if (c.SplitTier && !string.IsNullOrWhiteSpace(c.Flavor2)) return $"{c.CakeFlavor} / {c.Flavor2}";
            return c.CakeFlavor ?? "";
        }

        private static Dictionary<string, string> CakeRow(Cake c) => new()
        {
            ["size"] = IsSheet(c) ? c.TierSize ?? "" : $"{c.TierSize} {c.CakeShape?.ToLowerInvariant()}".Trim(),
            ["shape"] = c.CakeShape ?? "",
            ["layers"] = c.NumTierLayers > 0 ? c.NumTierLayers.ToString(Us) : "",
            ["servings"] = Servings(c)?.ToString(Us) ?? "",
            ["cake"] = CakeFlavorText(c),
            ["filling"] = c.FillingFlavor ?? "",
            ["icing"] = c.IcingFlavor ?? "",
        };

        public static Dictionary<string, List<Dictionary<string, string>>> BuildRows(Order o) => new()
        {
            ["main"] = MainCakes(o).Select(CakeRow).ToList(),
            ["kitchen"] = KitchenCakes(o).Select(CakeRow).ToList(),
            ["cupcake"] = (o.Cupcakes ?? []).Select(c => new Dictionary<string, string>
            {
                ["qty"] = c.CupcakeQuantity > 0 ? c.CupcakeQuantity.ToString(Us) : "",
                ["size"] = c.CupcakeSize ?? "",
                ["cake"] = string.IsNullOrWhiteSpace(c.SignatureName) ? c.CupcakeFlavor ?? "" : $"{c.SignatureName} ({c.CupcakeFlavor})",
                ["filling"] = c.FillingFlavor ?? "",
                ["icing"] = c.IcingFlavor ?? "",
            }).ToList(),
        };

        // ── Inspiration photos ────────────────────────────────────────────────

        public static readonly string[] PhotoTokens = ["cake_photo", "cupcake_photo"];

        // Google Docs inserts images only from formats it supports, within 50 MB and 25 megapixels
        private static readonly string[] DocsImageTypes = ["image/jpeg", "image/png", "image/gif"];
        private const long MaxPhotoBytes = 50L * 1024 * 1024;
        private const string PhotoFolderName = "_Contract Photos (temporary)";
        // Fit inside 3" x 3.5", keeping the photo's proportions (Docs scales to fit both dimensions)
        private const double PhotoMaxWidthPt = 216, PhotoMaxHeightPt = 252;

        private static string PhotoLabel(string token) => token == "cupcake_photo" ? "cupcake" : "cake";

        // Pass 3: each {{cake_photo}} / {{cupcake_photo}} left in the filled contract becomes its image.
        // Docs only inserts images from a URL it can fetch without signing in, so each photo is uploaded
        // to a temporary Drive folder, shared "anyone with the link" for the insert, then deleted (the
        // contract keeps its own copy). A photo that can't be inserted is reported and its token removed,
        // so the rest of the contract is unaffected.
        private async Task<List<string>> InsertPhotosAsync(DriveService drive, DocsService docs, string docId,
            string? parentFolderId, int orderNumber, IReadOnlyList<ContractPhoto> photos,
            ISet<string> chosen, CancellationToken ct)
        {
            var warnings = new List<string>();
            var doc = await docs.Documents.Get(docId).ExecuteAsync(ct);
            var spots = FindPhotoTokens(doc);
            if (spots.Count == 0) return warnings;

            string? folderId = null;
            if (photos.Count > 0 && spots.Any(sp => photos.Any(p => p.Token == sp.Token)))
            {
                try
                {
                    folderId = await GetPhotoFolderAsync(drive, parentFolderId, ct);
                    await DeleteStalePhotosAsync(drive, folderId, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    warnings.Add($"The inspiration photos could not be added (Drive folder error: {ex.Message}).");
                }
            }

            // Bottom-most first so the positions of the tokens above stay valid
            foreach (var spot in spots.OrderByDescending(sp => sp.Start))
            {
                var photo = photos.FirstOrDefault(p => p.Token == spot.Token);
                if (photo != null && folderId != null)
                {
                    var problem = PhotoProblem(photo);
                    if (problem == null)
                    {
                        string? uploadedId = null;
                        try
                        {
                            uploadedId = await UploadSharedPhotoAsync(drive, folderId, orderNumber, photo, ct);
                            await BatchAsync(docs, docId, null,
                                BuildPhotoRequests(spot, $"https://drive.google.com/uc?export=download&id={uploadedId}"), ct);
                            continue;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            problem = uploadedId == null
                                ? $"Google Drive wouldn't upload or share the temporary copy ({ex.Message}). If the Google account's sharing settings block \"Anyone with the link\", the photo has to be pasted into the contract by hand."
                                : $"Google Docs couldn't use it ({ex.Message}). Photos must be under 50 MB and 25 megapixels.";
                        }
                        finally
                        {
                            if (uploadedId != null)
                            {
                                try { await drive.Files.Delete(uploadedId).ExecuteAsync(CancellationToken.None); }
                                catch (Google.GoogleApiException) { /* swept up on the next run */ }
                            }
                        }
                    }
                    warnings.Add($"The {PhotoLabel(spot.Token)} inspiration photo ({photo.FileName}) wasn't added: {problem}");
                }
                else if (photo == null && chosen.Contains(spot.Token))
                {
                    warnings.Add($"The {PhotoLabel(spot.Token)} inspiration photo is no longer attached to the order.");
                }

                await BatchAsync(docs, docId, null, [DeleteRange(spot.Start, spot.End)], ct);
            }
            return warnings;
        }

        private static string? PhotoProblem(ContractPhoto photo)
        {
            if (!DocsImageTypes.Contains(photo.ContentType.ToLowerInvariant()))
                return "Google Docs only accepts JPEG, PNG or GIF photos. Save it as a JPEG and choose that instead.";
            var info = new FileInfo(photo.FilePath);
            if (!info.Exists) return "the file is missing from the attachments folder.";
            if (info.Length > MaxPhotoBytes) return "it is larger than Google Docs' 50 MB limit.";
            return null;
        }

        public record PhotoSpot(string Token, int Start, int End);

        // The image goes where the token starts, then the token text (one index later) is removed
        public static List<Request> BuildPhotoRequests(PhotoSpot spot, string imageUri) =>
        [
            new Request
            {
                InsertInlineImage = new InsertInlineImageRequest
                {
                    Location = new Location { Index = spot.Start },
                    Uri = imageUri,
                    ObjectSize = new Size
                    {
                        Width = new Dimension { Magnitude = PhotoMaxWidthPt, Unit = "PT" },
                        Height = new Dimension { Magnitude = PhotoMaxHeightPt, Unit = "PT" }
                    }
                }
            },
            DeleteRange(spot.Start + 1, spot.End + 1)
        ];

        public static List<PhotoSpot> FindPhotoTokens(Document doc)
        {
            var spots = new List<PhotoSpot>();
            foreach (var el in doc.Body?.Content ?? [])
            {
                if (el.Paragraph == null || el.StartIndex == null) continue;
                var text = ParagraphText(el.Paragraph);
                foreach (Match m in TokenRegex.Matches(text))
                {
                    var token = Normalize(m.Groups[1].Value);
                    if (PhotoTokens.Contains(token))
                        spots.Add(new PhotoSpot(token, el.StartIndex.Value + m.Index, el.StartIndex.Value + m.Index + m.Length));
                }
            }
            return spots;
        }

        private static async Task<string> GetPhotoFolderAsync(DriveService drive, string? parentFolderId, CancellationToken ct)
        {
            var parent = string.IsNullOrEmpty(parentFolderId) ? "root" : parentFolderId;
            var list = drive.Files.List();
            list.Q = $"name = '{PhotoFolderName}' and '{parent}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            list.Fields = "files(id)";
            list.SupportsAllDrives = true;
            list.IncludeItemsFromAllDrives = true;
            var existing = (await list.ExecuteAsync(ct)).Files?.FirstOrDefault();
            if (existing != null) return existing.Id;

            var create = drive.Files.Create(new DriveFile
            {
                Name = PhotoFolderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = [parent]
            });
            create.Fields = "id";
            create.SupportsAllDrives = true;
            return (await create.ExecuteAsync(ct)).Id;
        }

        // Leftovers from a run that stopped before deleting its upload
        private static async Task DeleteStalePhotosAsync(DriveService drive, string folderId, CancellationToken ct)
        {
            var list = drive.Files.List();
            var cutoff = DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            list.Q = $"'{folderId}' in parents and createdTime < '{cutoff}' and trashed = false";
            list.Fields = "files(id)";
            list.SupportsAllDrives = true;
            list.IncludeItemsFromAllDrives = true;
            foreach (var f in (await list.ExecuteAsync(ct)).Files ?? [])
            {
                try { await drive.Files.Delete(f.Id).ExecuteAsync(ct); }
                catch (Google.GoogleApiException) { }
            }
        }

        private static async Task<string> UploadSharedPhotoAsync(DriveService drive, string folderId, int orderNumber,
            ContractPhoto photo, CancellationToken ct)
        {
            await using var stream = File.OpenRead(photo.FilePath);
            var upload = drive.Files.Create(new DriveFile
            {
                Name = $"{orderNumber} - {PhotoLabel(photo.Token)} - {photo.FileName}",
                Parents = [folderId]
            }, stream, photo.ContentType);
            upload.Fields = "id";
            upload.SupportsAllDrives = true;
            var progress = await upload.UploadAsync(ct);
            if (progress.Exception != null) throw progress.Exception;
            var id = upload.ResponseBody?.Id ?? throw new ContractException("Drive upload returned no file id.");

            try
            {
                var share = drive.Permissions.Create(new Google.Apis.Drive.v3.Data.Permission { Type = "anyone", Role = "reader" }, id);
                share.SupportsAllDrives = true;
                await share.ExecuteAsync(ct);
            }
            catch
            {
                try { await drive.Files.Delete(id).ExecuteAsync(CancellationToken.None); } catch (Google.GoogleApiException) { }
                throw;
            }
            return id;
        }

        // ── Docs API fill ─────────────────────────────────────────────────────

        private record RowLocation(string Group, int TableStart, int RowIndex, Table Table);

        private async Task<List<string>> FillAsync(
            DocsService docs, string docId,
            Dictionary<string, string> values,
            Dictionary<string, string> rawValues,
            Dictionary<string, List<Dictionary<string, string>>> rows,
            CancellationToken ct)
        {
            var doc = await docs.Documents.Get(docId).ExecuteAsync(ct);

            var rowRequests = BuildRowInsertRequests(doc, rows);
            if (rowRequests.Count > 0)
            {
                await BatchAsync(docs, docId, doc.RevisionId, rowRequests, ct);
                doc = await docs.Documents.Get(docId).ExecuteAsync(ct);
            }

            var (requests, unknown) = BuildFillRequests(doc, values, rawValues, rows);
            if (requests.Count > 0)
                await BatchAsync(docs, docId, doc.RevisionId, requests, ct);

            return unknown.Count > 0
                ? [$"Unrecognized template tokens were left in the contract: {string.Join(", ", unknown)}"]
                : [];
        }

        // Pass 1: one InsertTableRow per extra item, below the row holding the group's tokens.
        // Bottom-most table first so the start indices of tables above stay valid.
        public static List<Request> BuildRowInsertRequests(Document doc,
            Dictionary<string, List<Dictionary<string, string>>> rows)
        {
            var requests = new List<Request>();
            foreach (var loc in FindRowLocations(doc).OrderByDescending(l => l.TableStart))
            {
                for (int i = 0; i < rows[loc.Group].Count - 1; i++)
                {
                    requests.Add(new Request
                    {
                        InsertTableRow = new InsertTableRowRequest
                        {
                            TableCellLocation = new TableCellLocation
                            {
                                TableStartLocation = new Location { Index = loc.TableStart },
                                RowIndex = loc.RowIndex,
                                ColumnIndex = 0
                            },
                            InsertBelow = true
                        }
                    });
                }
            }
            return requests;
        }

        // Pass 2 (on the document after rows were added): write items 2..n into the new, empty rows
        // (highest index first so earlier positions don't shift), then replace every token literal.
        // Row tokens still in the original row take the first item's values.
        public static (List<Request> Requests, List<string> Unknown) BuildFillRequests(Document doc,
            Dictionary<string, string> values,
            Dictionary<string, string> rawValues,
            Dictionary<string, List<Dictionary<string, string>>> rows)
        {
            var inserts = new List<(int Index, string Text)>();
            foreach (var loc in FindRowLocations(doc))
            {
                var items = rows[loc.Group];
                var templateCells = loc.Table.TableRows[loc.RowIndex].TableCells
                    .Select(cell => CellText(cell).TrimEnd('\n')).ToList();
                for (int k = 1; k < items.Count && loc.RowIndex + k < loc.Table.TableRows.Count; k++)
                {
                    var row = loc.Table.TableRows[loc.RowIndex + k];
                    for (int c = 0; c < row.TableCells.Count && c < templateCells.Count; c++)
                    {
                        var text = SubstituteRowTokens(templateCells[c], loc.Group, items[k]);
                        var start = row.TableCells[c].Content?.FirstOrDefault()?.StartIndex;
                        if (!string.IsNullOrEmpty(text) && start != null)
                            inserts.Add((start.Value, text));
                    }
                }
            }

            // Index-based edits, highest position first: the new table-row text, removing client boxes
            // and unchosen option lines (see BuildFieldDeletes), and removing conditional sections that
            // don't apply. Edits inside a removed section are dropped; the rest never overlap (row text is
            // inside tables, the other deletes are whole or partial body paragraphs).
            var sections = FindRemovedSections(doc, Conditions(values, rawValues));
            bool InRemovedSection(int index) => sections.Any(r => index >= r.Start && index < r.End);

            var requests = inserts
                .Where(i => !InRemovedSection(i.Index))
                .Select(i => (Index: i.Index, Request: new Request { InsertText = new InsertTextRequest { Location = new Location { Index = i.Index }, Text = i.Text } }))
                .Concat(BuildFieldDeletes(doc, values, rawValues, rows)
                    .Where(d => !InRemovedSection(d.Start))
                    .Select(d => (Index: d.Start, Request: DeleteRange(d.Start, d.End))))
                .Concat(sections.Select(r => (Index: r.Start, Request: DeleteRange(r.Start, r.End))))
                .OrderByDescending(x => x.Index)
                .Select(x => x.Request)
                .ToList();

            var unknown = new List<string>();
            var literals = AllText(doc).SelectMany(t => TokenRegex.Matches(t).Select(m => m.Value)).Distinct();
            foreach (var literal in literals)
            {
                var token = Normalize(literal[2..^2]);
                if (PhotoTokens.Contains(token)) continue;   // replaced by images in pass 3
                var value = Resolve(token, values, rawValues, rows);
                if (value == null)
                {
                    unknown.Add(literal);
                    continue;
                }
                requests.Add(new Request
                {
                    ReplaceAllText = new ReplaceAllTextRequest
                    {
                        ContainsText = new SubstringMatchCriteria { Text = literal, MatchCase = true },
                        ReplaceText = value
                    }
                });
            }
            return (requests, unknown);
        }

        // Client boxes appear in the API as inline objects. For each body paragraph:
        //  - option line ({{mark:key=value}}): when key has a value, the matching line loses its boxes and
        //    every other option line for that key is deleted; with no value nothing changes.
        //  - "{{token}} [box]": when the token has a value the box (and the space before it, unless text
        //    follows the box directly) is deleted;
        //    when it is blank only the space goes, leaving the box for the client.
        private static List<(int Start, int End)> BuildFieldDeletes(Document doc,
            Dictionary<string, string> values,
            Dictionary<string, string> rawValues,
            Dictionary<string, List<Dictionary<string, string>>> rows)
        {
            var deletes = new List<(int Start, int End)>();
            var body = doc.Body?.Content ?? [];
            for (int e = 0; e < body.Count; e++)
            {
                var para = body[e].Paragraph;
                if (para == null || body[e].StartIndex == null || body[e].EndIndex == null) continue;
                int baseIndex = body[e].StartIndex!.Value;

                var text = ParagraphText(para);
                var tokens = TokenRegex.Matches(text).Cast<Match>().ToList();

                var mark = tokens.FirstOrDefault(m => Normalize(m.Groups[1].Value).StartsWith("mark:"));
                if (mark != null)
                {
                    var key = Normalize(mark.Groups[1].Value)[5..].Split('=', 2)[0].Trim();
                    bool keyHasValue = (values.TryGetValue(key, out var v) && v != "")
                                    || (rawValues.TryGetValue(key, out var r) && r != "");
                    if (!keyHasValue) continue;

                    if (Resolve(Normalize(mark.Groups[1].Value), values, rawValues, rows) == Check)
                    {
                        for (int i = 0; i < text.Length; i++)
                        {
                            if (text[i] != ClientBox) continue;
                            if (i > 0 && text[i - 1] == ' ') deletes.Add((baseIndex + i - 1, baseIndex + i + 1));
                            else if (i + 1 < text.Length && text[i + 1] == ' ') deletes.Add((baseIndex + i, baseIndex + i + 2));
                            else deletes.Add((baseIndex + i, baseIndex + i + 1));
                        }
                    }
                    else
                    {
                        deletes.Add((baseIndex, ParagraphDeleteEnd(body, e)));   // the whole line
                    }
                    continue;
                }

                foreach (var m in tokens)
                {
                    int after = m.Index + m.Length, j = after;
                    if (j < text.Length && text[j] == ' ') j++;
                    if (j >= text.Length || text[j] != ClientBox) continue;
                    var value = Resolve(Normalize(m.Groups[1].Value), values, rawValues, rows);
                    if (value == null) continue;
                    if (value != "")
                    {
                        // Keep the separating space when a label follows the box directly ("[box]Phone number:")
                        bool textFollows = j + 1 < text.Length && !char.IsWhiteSpace(text[j + 1]);
                        deletes.Add(textFollows && j > after ? (baseIndex + j, baseIndex + j + 1) : (baseIndex + after, baseIndex + j + 1));
                    }
                    else if (j > after) deletes.Add((baseIndex + after, baseIndex + after + 1));
                }
            }
            return deletes;
        }

        private static Task BatchAsync(DocsService docs, string docId, string? revisionId, IList<Request> requests, CancellationToken ct) =>
            docs.Documents.BatchUpdate(new BatchUpdateDocumentRequest
            {
                Requests = requests,
                WriteControl = revisionId == null ? null : new WriteControl { RequiredRevisionId = revisionId }
            }, docId).ExecuteAsync(ct);

        private static string? Resolve(string token, Dictionary<string, string> values,
            Dictionary<string, string> rawValues,
            Dictionary<string, List<Dictionary<string, string>>> rows)
        {
            // Section markers that survive (their section applies) are simply removed
            if (ConditionName(token) is { } condition)
                return ConditionNames.Contains(condition) ? "" : null;

            if (token.StartsWith("mark:"))
            {
                var parts = token[5..].Split('=', 2);
                if (parts.Length != 2) return null;
                var key = parts[0].Trim();
                var expected = parts[1].Trim();
                if (!values.TryGetValue(key, out var display)) return null;
                rawValues.TryGetValue(key, out var raw);
                bool match = (display != "" && string.Equals(display, expected, StringComparison.OrdinalIgnoreCase))
                          || (!string.IsNullOrEmpty(raw) && string.Equals(raw, expected, StringComparison.OrdinalIgnoreCase));
                return match ? Check : "";
            }

            var dot = token.IndexOf('.');
            if (dot > 0 && rows.TryGetValue(token[..dot], out var items))
            {
                var field = token[(dot + 1)..];
                if (!RowFields[token[..dot]].Contains(field)) return null;
                return items.Count > 0 ? items[0][field] : "";
            }

            return values.TryGetValue(token, out var v) ? v : null;
        }

        private static string SubstituteRowTokens(string text, string group, Dictionary<string, string> item) =>
            TokenRegex.Replace(text, m =>
            {
                var token = Normalize(m.Groups[1].Value);
                return token.StartsWith(group + ".") && item.TryGetValue(token[(group.Length + 1)..], out var v)
                    ? v
                    : m.Value;
            });

        // Finds the table row holding each group's row tokens (first match per group).
        private static List<RowLocation> FindRowLocations(Document doc)
        {
            var found = new Dictionary<string, RowLocation>();
            foreach (var element in doc.Body?.Content ?? [])
            {
                if (element.Table == null || element.StartIndex == null) continue;
                for (int r = 0; r < element.Table.TableRows.Count; r++)
                {
                    var rowText = string.Concat(element.Table.TableRows[r].TableCells.Select(CellText));
                    foreach (Match m in TokenRegex.Matches(rowText))
                    {
                        var token = Normalize(m.Groups[1].Value);
                        var group = RowGroups.FirstOrDefault(g => token.StartsWith(g + "."));
                        if (group != null && !found.ContainsKey(group))
                            found[group] = new RowLocation(group, element.StartIndex.Value, r, element.Table);
                    }
                }
            }
            return found.Values.ToList();
        }

        private static string CellText(TableCell cell) => ElementsText(cell.Content);

        private static string ElementsText(IList<StructuralElement>? elements)
        {
            var sb = new StringBuilder();
            foreach (var e in elements ?? [])
            {
                if (e.Paragraph != null)
                    foreach (var pe in e.Paragraph.Elements ?? [])
                        sb.Append(pe.TextRun?.Content);
                if (e.Table != null)
                    foreach (var row in e.Table.TableRows ?? [])
                        foreach (var cell in row.TableCells ?? [])
                            sb.Append(CellText(cell));
            }
            return sb.ToString();
        }

        // Body, headers, and footers. Text is returned per paragraph group so a token can't be
        // matched across a header/body boundary.
        private static IEnumerable<string> AllText(Document doc)
        {
            yield return ElementsText(doc.Body?.Content);
            foreach (var h in doc.Headers?.Values ?? Enumerable.Empty<Header>())
                yield return ElementsText(h.Content);
            foreach (var f in doc.Footers?.Values ?? Enumerable.Empty<Footer>())
                yield return ElementsText(f.Content);
        }
    }
}
