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

    // Fills the wedding contract Google Doc template from an order.
    //
    // The template holds {{tokens}} (see docs/wedding-contract-template.md):
    //   {{event_date}}                   scalar value
    //   {{main.size}} / {{cupcake.qty}}  table-row tokens; the row they sit in is repeated per item
    //   {{mark:board_color=Gold}}        put at the start of an option line. When board_color has a value,
    //                                    the matching line gets "✔ " and loses its client box, and the
    //                                    other option lines are removed; with no value all lines stay.
    // A scalar token directly before a client (eSignature) box, e.g. "{{ceremony_time}} [box]", removes
    // the box when it has a value, so the contract never shows both data and a box to fill.
    // Token names are case-insensitive and may contain spaces inside the braces.
    public class WeddingContractService
    {
        private const string Check = "✔ ";
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

        // mode: "overwrite" replaces the current contract (new doc, old one moved to Drive trash);
        //       "revision" creates an additional REVISED doc and leaves the current one alone.
        public async Task<ContractResult> GenerateAsync(Order order, string mode, CancellationToken ct)
        {
            var templateId = _google.Settings.WeddingContractTemplateId;
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ContractException("No wedding contract template is configured (Google:WeddingContractTemplateId).");

            var services = await _google.CreateServicesAsync(ct)
                ?? throw new ContractException("Google is not connected. Connect it on the Management page.");
            var (drive, docs) = services;

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
            copyRequest.Fields = "id, name, webViewLink";
            var copy = await copyRequest.ExecuteAsync(ct);

            var result = new ContractResult
            {
                DocId = copy.Id,
                Name = copy.Name,
                Url = copy.WebViewLink ?? $"https://docs.google.com/document/d/{copy.Id}/edit"
            };

            // 2. Fill it
            result.Warnings.AddRange(await FillAsync(docs, copy.Id,
                BuildValues(order), BuildRawValues(order), BuildRows(order), ct));

            // 3. Overwrite → trash the previous contract
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
            var templateId = _google.Settings.WeddingContractTemplateId;
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ContractException("No wedding contract template is configured (Google:WeddingContractTemplateId).");
            var services = await _google.CreateServicesAsync(ct)
                ?? throw new ContractException("Google is not connected.");

            var doc = await services.Docs.Documents.Get(templateId).ExecuteAsync(ct);
            var found = AllText(doc).SelectMany(t => TokenRegex.Matches(t).Select(m => Normalize(m.Groups[1].Value)))
                .Distinct().OrderBy(t => t).ToList();
            var known = KnownTokens();
            var unknown = found.Where(t => !IsKnownToken(t, known)).ToList();
            var unused = known.Where(k => !found.Contains(k)).ToList();
            return new { templateName = doc.Title, found, unknown, unused };
        }

        public static List<string> KnownTokens()
        {
            var tokens = BuildValues(new Order { WeddingDetails = new WeddingDetails() }).Keys.ToList();
            tokens.AddRange(RowFields.SelectMany(g => g.Value.Select(f => $"{g.Key}.{f}")));
            return tokens.OrderBy(t => t).ToList();
        }

        private static bool IsKnownToken(string token, List<string> known) =>
            known.Contains(token) ||
            (token.StartsWith("mark:") && token.Contains('=') && known.Contains(token[5..token.IndexOf('=')].Trim()));

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

        // "Cake: Vanilla, Chocolate; Filling: Raspberry; Icing: Vanilla Buttercream" from the order's items
        private static string FlavorSummary(IEnumerable<(IEnumerable<string?> Cake, string? Filling, string? Icing)> items)
        {
            var list = items.ToList();
            string Join(IEnumerable<string?> xs) => string.Join(", ", xs
                .Where(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, "None", StringComparison.OrdinalIgnoreCase))
                .Select(x => x!.Trim()).Distinct());
            var parts = new[]
            {
                ("Cake", Join(list.SelectMany(i => i.Cake))),
                ("Filling", Join(list.Select(i => i.Filling))),
                ("Icing", Join(list.Select(i => i.Icing))),
            };
            return string.Join("; ", parts.Where(p => p.Item2 != "").Select(p => $"{p.Item1}: {p.Item2}"));
        }

        private static string CakeFlavorSummary(IEnumerable<Cake> cakes) =>
            FlavorSummary(cakes.Select(c => ((IEnumerable<string?>)CakeFlavorText(c).Split(" / "), c.FillingFlavor, c.IcingFlavor)));

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
                ["main_flavor_description"] = CakeFlavorSummary(main),
                ["main_design_description"] = "",   // no app field yet; staff writes it in the doc
                ["main_servings_total"] = Count(mainServings),
                ["kitchen_cakes"] = kitchen.Count > 0 ? "Yes" : "No",
                ["kitchen_flavor_description"] = CakeFlavorSummary(kitchen),
                ["kitchen_servings_total"] = Count(kitchenServings),
                ["cupcake_flavor_description"] = FlavorSummary((o.Cupcakes ?? []).Select(c =>
                    ((IEnumerable<string?>)[c.CupcakeFlavor], c.FillingFlavor, c.IcingFlavor))),
                ["cupcake_design_description"] = "",   // no app field yet; staff writes it in the doc
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

            // Index-based edits, highest position first: the new table-row text, plus removing client
            // boxes and unchosen option lines (see BuildFieldDeletes). They never overlap: the row text
            // is inside tables and the deletes are in body paragraphs.
            var requests = inserts
                .Select(i => (Index: i.Index, Request: new Request { InsertText = new InsertTextRequest { Location = new Location { Index = i.Index }, Text = i.Text } }))
                .Concat(BuildFieldDeletes(doc, values, rawValues, rows).Select(d => (Index: d.Start,
                    Request: new Request { DeleteContentRange = new DeleteContentRangeRequest { Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = d.Start, EndIndex = d.End } } })))
                .OrderByDescending(x => x.Index)
                .Select(x => x.Request)
                .ToList();

            var unknown = new List<string>();
            var literals = AllText(doc).SelectMany(t => TokenRegex.Matches(t).Select(m => m.Value)).Distinct();
            foreach (var literal in literals)
            {
                var value = Resolve(Normalize(literal[2..^2]), values, rawValues, rows);
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
            const char Box = '\uFFFC';
            var deletes = new List<(int Start, int End)>();
            var body = doc.Body?.Content ?? [];
            for (int e = 0; e < body.Count; e++)
            {
                var para = body[e].Paragraph;
                if (para == null || body[e].StartIndex == null || body[e].EndIndex == null) continue;
                int baseIndex = body[e].StartIndex!.Value;

                // Paragraph text with one placeholder character per index of each non-text element
                var sb = new StringBuilder();
                foreach (var pe in para.Elements ?? [])
                {
                    if (pe.TextRun != null) sb.Append(pe.TextRun.Content);
                    else sb.Append(pe.InlineObjectElement != null ? Box : '\uFFFD',
                        Math.Max(1, (pe.EndIndex ?? 0) - (pe.StartIndex ?? 0)));
                }
                var text = sb.ToString();
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
                            if (text[i] != Box) continue;
                            if (i > 0 && text[i - 1] == ' ') deletes.Add((baseIndex + i - 1, baseIndex + i + 1));
                            else if (i + 1 < text.Length && text[i + 1] == ' ') deletes.Add((baseIndex + i, baseIndex + i + 2));
                            else deletes.Add((baseIndex + i, baseIndex + i + 1));
                        }
                    }
                    else
                    {
                        // Whole line; the body's last paragraph and one right before a table must keep its newline
                        bool keepNewline = e == body.Count - 1 || body[e + 1].Table != null;
                        deletes.Add((baseIndex, body[e].EndIndex!.Value - (keepNewline ? 1 : 0)));
                    }
                    continue;
                }

                foreach (var m in tokens)
                {
                    int after = m.Index + m.Length, j = after;
                    if (j < text.Length && text[j] == ' ') j++;
                    if (j >= text.Length || text[j] != Box) continue;
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

        private static Task BatchAsync(DocsService docs, string docId, string revisionId, IList<Request> requests, CancellationToken ct) =>
            docs.Documents.BatchUpdate(new BatchUpdateDocumentRequest
            {
                Requests = requests,
                WriteControl = new WriteControl { RequiredRevisionId = revisionId }
            }, docId).ExecuteAsync(ct);

        private static string? Resolve(string token, Dictionary<string, string> values,
            Dictionary<string, string> rawValues,
            Dictionary<string, List<Dictionary<string, string>>> rows)
        {
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
