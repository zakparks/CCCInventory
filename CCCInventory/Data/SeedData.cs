namespace CCCInventory.Data
{
    public static class SeedData
    {
        public static void Initialize(DataContext context)
        {
            SeedOptions(context);
            SeedSignatureCupcakes(context);
            SeedOrders(context);
        }

        private static void SeedOptions(DataContext context)
        {
            // Seed CookieSize category if not yet present (added in Phase 5)
            if (!context.OptionItems.Any(o => o.Category == "CookieSize"))
            {
                context.OptionItems.AddRange(
                    new OptionItem { Category = "CookieSize", Value = "Standard", SortOrder = 0 },
                    new OptionItem { Category = "CookieSize", Value = "Small",    SortOrder = 1 }
                );
                context.SaveChanges();
            }

            if (context.OptionItems.Any()) return;

            var options = new List<OptionItem>
            {
                // CakeTierSize
                new() { Category = "CakeTierSize", Value = "Micro",         SortOrder = 0 },
                new() { Category = "CakeTierSize", Value = "6\"",            SortOrder = 1 },
                new() { Category = "CakeTierSize", Value = "8\"",            SortOrder = 2 },
                new() { Category = "CakeTierSize", Value = "10\"",           SortOrder = 3 },
                new() { Category = "CakeTierSize", Value = "12\"",           SortOrder = 4 },
                new() { Category = "CakeTierSize", Value = "Quarter Sheet",  SortOrder = 5 },
                new() { Category = "CakeTierSize", Value = "Half Sheet",     SortOrder = 6 },

                // CakeShape
                new() { Category = "CakeShape", Value = "Round",  SortOrder = 0 },
                new() { Category = "CakeShape", Value = "Square", SortOrder = 1 },
                new() { Category = "CakeShape", Value = "Heart",  SortOrder = 2 },

                // Flavor (shared cake + cupcake)
                new() { Category = "Flavor", Value = "Vanilla",      SortOrder = 0 },
                new() { Category = "Flavor", Value = "Chocolate",     SortOrder = 1 },
                new() { Category = "Flavor", Value = "Red Velvet",    SortOrder = 2 },
                new() { Category = "Flavor", Value = "Lemon",         SortOrder = 3 },
                new() { Category = "Flavor", Value = "Funfetti",      SortOrder = 4 },
                new() { Category = "Flavor", Value = "White",         SortOrder = 5 },
                new() { Category = "Flavor", Value = "Champagne",     SortOrder = 6 },
                new() { Category = "Flavor", Value = "Strawberry",    SortOrder = 7 },

                // FillingFlavor
                new() { Category = "FillingFlavor", Value = "None",             SortOrder = 0 },
                new() { Category = "FillingFlavor", Value = "Vanilla",          SortOrder = 1 },
                new() { Category = "FillingFlavor", Value = "Chocolate",        SortOrder = 2 },
                new() { Category = "FillingFlavor", Value = "Strawberry",       SortOrder = 3 },
                new() { Category = "FillingFlavor", Value = "Raspberry",        SortOrder = 4 },
                new() { Category = "FillingFlavor", Value = "Lemon Curd",       SortOrder = 5 },
                new() { Category = "FillingFlavor", Value = "Cream Cheese",     SortOrder = 6 },
                new() { Category = "FillingFlavor", Value = "Chocolate Ganache",SortOrder = 7 },
                new() { Category = "FillingFlavor", Value = "Chocolate Mousse", SortOrder = 8 },

                // IcingFlavor
                new() { Category = "IcingFlavor", Value = "Vanilla Buttercream",        SortOrder = 0 },
                new() { Category = "IcingFlavor", Value = "Chocolate Buttercream",      SortOrder = 1 },
                new() { Category = "IcingFlavor", Value = "Cream Cheese",               SortOrder = 2 },
                new() { Category = "IcingFlavor", Value = "Lavender Buttercream",       SortOrder = 3 },
                new() { Category = "IcingFlavor", Value = "Pink Buttercream",           SortOrder = 4 },
                new() { Category = "IcingFlavor", Value = "Buttercream",                SortOrder = 5 },
                new() { Category = "IcingFlavor", Value = "Dark Chocolate Ganache",     SortOrder = 6 },
                new() { Category = "IcingFlavor", Value = "Gold Shimmer",               SortOrder = 7 },
                new() { Category = "IcingFlavor", Value = "Blush Buttercream",          SortOrder = 8 },
                new() { Category = "IcingFlavor", Value = "White Chocolate",            SortOrder = 9 },
                new() { Category = "IcingFlavor", Value = "Vanilla",                    SortOrder = 10 },

                // CupcakeSize
                new() { Category = "CupcakeSize", Value = "Regular", SortOrder = 0 },
                new() { Category = "CupcakeSize", Value = "Mini",     SortOrder = 1 },

                // CookieType
                new() { Category = "CookieType", Value = "Chocolate Chip",   SortOrder = 0 },
                new() { Category = "CookieType", Value = "M&M",              SortOrder = 1 },
                new() { Category = "CookieType", Value = "Peanut Butter",    SortOrder = 2 },
                new() { Category = "CookieType", Value = "Sugar Cookie",     SortOrder = 3 },
                new() { Category = "CookieType", Value = "Oatmeal Raisin",   SortOrder = 4 },
                new() { Category = "CookieType", Value = "Gingerbread",      SortOrder = 5 },
                new() { Category = "CookieType", Value = "Pumpkin",          SortOrder = 6 },
            };

            context.OptionItems.AddRange(options);
            context.SaveChanges();
        }

        private static void SeedSignatureCupcakes(DataContext context)
        {
            if (context.SignatureCupcakes.Any()) return;

            context.SignatureCupcakes.AddRange(
                new SignatureCupcake { Name = "Classic Vanilla Dream", CupcakeFlavor = "Vanilla", FillingFlavor = "None", IcingFlavor = "Vanilla Buttercream", SortOrder = 0 },
                new SignatureCupcake { Name = "Death by Chocolate", CupcakeFlavor = "Chocolate", FillingFlavor = "Chocolate Ganache", IcingFlavor = "Chocolate Buttercream", SortOrder = 1 },
                new SignatureCupcake { Name = "Lemon Raspberry Bliss", CupcakeFlavor = "Lemon", FillingFlavor = "Raspberry", IcingFlavor = "Vanilla Buttercream", SortOrder = 2 },
                new SignatureCupcake { Name = "Champagne Celebration", CupcakeFlavor = "Champagne", FillingFlavor = "None", IcingFlavor = "Gold Shimmer", SortOrder = 3 }
            );
            context.SaveChanges();
        }

        private static void SeedOrders(DataContext context)
        {
            if (context.Orders.Any()) return;

            var today = DateTime.Today;

            // Orders are distributed across three weeks (Tue–Sat each week, shop is closed Sun–Mon).
            // Compute the next Tuesday from today (or next week's if today IS Tuesday).
            int daysUntilTuesday = ((int)DayOfWeek.Tuesday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilTuesday == 0) daysUntilTuesday = 7;

            // w3 = next week (future — Active / special-status orders)
            var w3Tue = today.AddDays(daysUntilTuesday);
            var w3Wed = w3Tue.AddDays(1);
            var w3Thu = w3Tue.AddDays(2);
            var w3Fri = w3Tue.AddDays(3);
            var w3Sat = w3Tue.AddDays(4);

            // w2 = current week (mix of archived + active depending on today's date)
            var w2Tue = w3Tue.AddDays(-7);
            var w2Wed = w3Wed.AddDays(-7);
            var w2Thu = w3Thu.AddDays(-7);
            var w2Fri = w3Fri.AddDays(-7);
            var w2Sat = w3Sat.AddDays(-7);

            // w1 = previous week (all in the past → all archived)
            var w1Tue = w3Tue.AddDays(-14);
            var w1Wed = w3Wed.AddDays(-14);
            var w1Thu = w3Thu.AddDays(-14);
            var w1Fri = w3Fri.AddDays(-14);
            var w1Sat = w3Sat.AddDays(-14);

            context.Orders.AddRange(

                // ══════════════════════════════════════════════════════════════
                // PREVIOUS WEEK (w1) — all OrderDateTimes in the past → Archived
                // ══════════════════════════════════════════════════════════════

                new Order
                {
                    Title = "Amy - Baby Shower Cake",
                    OrderDateTime = w1Tue.AddHours(10),
                    CustName = "Amy Chang", CustPhone = "555-501-6007", CustEmail = "amy.c@email.com",
                    Details = "Baby shower cake — lemon with raspberry filling",
                    OrderType = "Pickup",
                    TotalCost = 80.00, DepositAmount = 80.00, DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-21), PaidInFull = true, ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Lemon", FillingFlavor = "Raspberry", IcingFlavor = "Blush Buttercream", SplitTier = false }]
                },
                new Order
                {
                    Title = "Marcus - Choc Chip Cookies",
                    OrderDateTime = w1Tue.AddHours(12),
                    CustName = "Marcus Williams", CustPhone = "555-876-5432",
                    Details = "3 dozen chocolate chip cookies",
                    OrderType = "Pickup",
                    TotalCost = 42.00, DepositAmount = 42.00, DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-19), PaidInFull = true,
                    Cookies = [new Cookie { CookieType = "Chocolate Chip", CookieQuantity = 36 }]
                },
                new Order
                {
                    Title = "Greg - Retirement Party",
                    OrderDateTime = w1Wed.AddHours(12),
                    CustName = "Greg Hoffman", CustPhone = "555-601-7008",
                    Details = "Retirement party — 2 sheet cakes",
                    OrderType = "Delivery", DeliveryLocation = "325 Oak Hill Rd",
                    TotalCost = 120.00, DepositAmount = 60.00, DepositPaymentMethod = "Check",
                    DateOrderPlaced = today.AddDays(-35), ContractSent = true, DayOfTextSent = true,
                    Cakes = [
                        new Cake { TierSize = "Half Sheet", NumTierLayers = 1, CakeShape = "Round", CakeFlavor = "Vanilla",   FillingFlavor = "None", IcingFlavor = "Vanilla Buttercream",   SplitTier = false },
                        new Cake { TierSize = "Half Sheet", NumTierLayers = 1, CakeShape = "Round", CakeFlavor = "Chocolate", FillingFlavor = "None", IcingFlavor = "Chocolate Buttercream", SplitTier = false }
                    ]
                },
                new Order
                {
                    Title = "James - Cookies",
                    OrderDateTime = w1Wed.AddHours(14),
                    CustName = "James O'Brien", CustPhone = "555-789-0123",
                    Details = "2 dozen M&M cookies + 1 dozen peanut butter",
                    OrderType = "Pickup",
                    TotalCost = 48.00, DepositAmount = 48.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-18), PaidInFull = true,
                    Cookies = [
                        new Cookie { CookieType = "M&M",          CookieQuantity = 24 },
                        new Cookie { CookieType = "Peanut Butter", CookieQuantity = 12 }
                    ]
                },
                new Order
                {
                    Title = "Tiffany - Sugar Cookies",
                    OrderDateTime = w1Thu.AddHours(11),
                    CustName = "Tiffany Brooks", CustPhone = "555-234-5679",
                    Details = "4 dozen sugar cookies, fall theme",
                    OrderType = "Pickup",
                    TotalCost = 64.00, DepositAmount = 32.00, DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-22),
                    Cookies = [new Cookie { CookieType = "Sugar Cookie", CookieQuantity = 48 }]
                },
                new Order
                {
                    Title = "Kevin - Office Cookies",
                    OrderDateTime = w1Thu.AddHours(15),
                    CustName = "Kevin Park", CustPhone = "555-333-4455",
                    Details = "Office cookie order — choc chip and oatmeal raisin",
                    OrderType = "Pickup",
                    TotalCost = 54.00, DepositAmount = 54.00, DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-17), PaidInFull = true,
                    Cookies = [
                        new Cookie { CookieType = "Chocolate Chip", CookieQuantity = 24 },
                        new Cookie { CookieType = "Oatmeal Raisin", CookieQuantity = 24 }
                    ]
                },
                new Order
                {
                    Title = "Ryan - Gingerbread Cookies",
                    OrderDateTime = w1Fri.AddHours(10),
                    CustName = "Ryan Miller", CustPhone = "555-777-8899",
                    Details = "3 dozen gingerbread cookies, iced",
                    OrderType = "Pickup",
                    TotalCost = 48.00, DepositAmount = 48.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-16), PaidInFull = true,
                    Cookies = [new Cookie { CookieType = "Gingerbread", CookieQuantity = 36 }]
                },
                new Order
                {
                    Title = "Jason - Birthday Cake",
                    OrderDateTime = w1Fri.AddHours(12),
                    CustName = "Jason Taylor", CustPhone = "555-999-0011",
                    Details = "Birthday cake, funfetti with rainbow sprinkles",
                    OrderType = "Pickup",
                    TotalCost = 65.00, DepositAmount = 33.00, DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-22),
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla", IcingFlavor = "Vanilla Buttercream", SplitTier = false }]
                },
                new Order
                {
                    Title = "Brandon - Pumpkin Cookies",
                    OrderDateTime = w1Fri.AddHours(14),
                    CustName = "Brandon Thomas", CustPhone = "555-221-3344",
                    Details = "2 dozen pumpkin cookies",
                    OrderType = "Pickup",
                    TotalCost = 32.00, DepositAmount = 32.00, DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-15), PaidInFull = true,
                    Cookies = [new Cookie { CookieType = "Pumpkin", CookieQuantity = 24 }]
                },
                new Order
                {
                    Title = "Daniel - Assorted Cookies",
                    OrderDateTime = w1Sat.AddHours(12),
                    CustName = "Daniel Martin", CustPhone = "555-665-7788",
                    Details = "4 dozen assorted cookies",
                    OrderType = "Pickup",
                    TotalCost = 64.00, DepositAmount = 32.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-21),
                    Cookies = [
                        new Cookie { CookieType = "Chocolate Chip", CookieQuantity = 24 },
                        new Cookie { CookieType = "Sugar Cookie",   CookieQuantity = 24 }
                    ]
                },

                // ══════════════════════════════════════════════════════════════
                // CURRENT WEEK (w2) — OrderDateTimes near today (mix of archived + active)
                // ══════════════════════════════════════════════════════════════

                new Order
                {
                    Title = "Emily - Birthday Cake",
                    OrderDateTime = w2Tue.AddHours(10),
                    CustName = "Emily Johnson", CustPhone = "555-234-5678", CustEmail = "emily.j@email.com",
                    Details = "Birthday cake, pink floral theme",
                    OrderType = "Pickup",
                    TotalCost = 95.00, DepositAmount = 50.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-14), ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla", FillingFlavor = "Strawberry", IcingFlavor = "Buttercream", SplitTier = false }]
                },
                new Order
                {
                    Title = "David - Graduation Cake",
                    OrderDateTime = w2Tue.AddHours(16),
                    CustName = "David Chen", CustPhone = "555-456-7890",
                    Details = "Graduation cake, navy and gold",
                    OrderType = "Pickup",
                    TotalCost = 75.00, DepositAmount = 40.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-7),
                    Cakes = [new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Red Velvet", FillingFlavor = "Cream Cheese", IcingFlavor = "Cream Cheese", SplitTier = false }]
                },
                new Order
                {
                    Title = "Priya - Pupcakes",
                    OrderDateTime = w2Wed.AddHours(9),
                    CustName = "Priya Patel", CustPhone = "555-567-8901", CustEmail = "priya.p@email.com",
                    Details = "Pupcake dozen for Biscuit's 3rd birthday!",
                    OrderType = "Pickup",
                    TotalCost = 36.00, DepositAmount = 36.00, DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-10), PaidInFull = true,
                    Pupcakes = [new Pupcake { PupcakeSize = "Regular", PupcakeQuantity = 12 }]
                },
                new Order
                {
                    Title = "Rachel - Retirement Cake",
                    OrderDateTime = w2Wed.AddHours(11),
                    CustName = "Rachel Thompson", CustPhone = "555-678-9012", CustEmail = "rachel.t@email.com",
                    Details = "Retirement party — sheet cake, golf theme",
                    OrderType = "Pickup",
                    TotalCost = 65.00, DepositAmount = 33.00, DepositPaymentMethod = "Check",
                    DateOrderPlaced = today.AddDays(-21), ContractSent = true,
                    Cakes = [new Cake { TierSize = "Half Sheet", NumTierLayers = 1, CakeShape = "Round", CakeFlavor = "White", FillingFlavor = "None", IcingFlavor = "Vanilla", SplitTier = false }]
                },
                new Order
                {
                    Title = "Carlos - Office Birthday",
                    OrderDateTime = w2Wed.AddHours(17),
                    CustName = "Carlos Reyes", CustPhone = "555-901-2345",
                    Details = "Office birthday, chocolate sheet cake",
                    OrderType = "Pickup",
                    TotalCost = 55.00, DepositAmount = 55.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-6), PaidInFull = true,
                    Cakes = [new Cake { TierSize = "Quarter Sheet", NumTierLayers = 1, CakeShape = "Round", CakeFlavor = "Chocolate", FillingFlavor = "None", IcingFlavor = "Chocolate Buttercream", SplitTier = false }]
                },
                new Order
                {
                    Title = "Andrew - Anniversary Cake",
                    OrderDateTime = w2Thu.AddHours(10),
                    CustName = "Andrew Kim", CustPhone = "555-123-4567",
                    Details = "Anniversary cake, heart shape, red velvet",
                    OrderType = "Pickup",
                    TotalCost = 68.00, DepositAmount = 35.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-11), ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Heart", CakeFlavor = "Red Velvet", FillingFlavor = "Cream Cheese", IcingFlavor = "Cream Cheese", SplitTier = false }]
                },
                new Order
                {
                    Title = "Brittany - Pup Birthday",
                    OrderDateTime = w2Thu.AddHours(11).AddMinutes(30),
                    CustName = "Brittany Evans", CustPhone = "555-345-6780", CustEmail = "bev@email.com",
                    Details = "Puppy birthday — 2 doz regular pupcakes + 6 cake pupcakes",
                    OrderType = "Pickup",
                    TotalCost = 58.00, DepositAmount = 30.00, DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-9),
                    Pupcakes = [
                        new Pupcake { PupcakeSize = "Regular", PupcakeQuantity = 24 },
                        new Pupcake { PupcakeSize = "Cake",    PupcakeQuantity = 6  }
                    ]
                },
                new Order
                {
                    Title = "Michael - Promotion Cake",
                    OrderDateTime = w2Thu.AddHours(12),
                    CustName = "Michael Scott", CustPhone = "555-867-5309", CustEmail = "mscott@dundermifflin.com",
                    Details = "Promotion congrats cake, chocolate",
                    OrderType = "Delivery", DeliveryLocation = "1725 Slough Ave, Scranton",
                    TotalCost = 90.00, DepositAmount = 45.00, DepositPaymentMethod = "Credit Card",
                    DateOrderPlaced = today.AddDays(-16), ContractSent = true,
                    Cakes = [new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Square", CakeFlavor = "Chocolate", FillingFlavor = "Chocolate Mousse", IcingFlavor = "Dark Chocolate Ganache", SplitTier = false }]
                },
                new Order
                {
                    Title = "Megan - Work Birthday",
                    OrderDateTime = w2Fri.AddHours(11),
                    CustName = "Megan Wilson", CustPhone = "555-888-9900", CustEmail = "megan.w@email.com",
                    Details = "Work birthday — 2 doz cupcakes, mixed flavors",
                    OrderType = "Pickup",
                    TotalCost = 48.00, DepositAmount = 48.00, DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-4), PaidInFull = true,
                    Cupcakes = [
                        new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 12, CupcakeFlavor = "Chocolate", FillingFlavor = "None", IcingFlavor = "Chocolate Buttercream" },
                        new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 12, CupcakeFlavor = "Vanilla",   FillingFlavor = "None", IcingFlavor = "Vanilla Buttercream"   }
                    ]
                },
                new Order
                {
                    Title = "Katie - Kids Birthday Cake",
                    OrderDateTime = w2Sat.AddHours(10),
                    CustName = "Katie Harris", CustPhone = "555-554-6677",
                    Details = "Kids' birthday cake, rainbow theme",
                    OrderType = "Pickup",
                    TotalCost = 70.00, DepositAmount = 35.00, DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-13), ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla", IcingFlavor = "Buttercream", SplitTier = false }]
                },
                new Order
                {
                    Title = "Chris - Farewell Cupcakes",
                    OrderDateTime = w2Sat.AddHours(14),
                    CustName = "Christopher White", CustPhone = "555-443-5566",
                    Details = "Farewell party cupcakes, 18 regular",
                    OrderType = "Pickup",
                    TotalCost = 36.00, DepositAmount = 18.00, DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-6),
                    Cupcakes = [new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 18, CupcakeFlavor = "Red Velvet", FillingFlavor = "None", IcingFlavor = "Cream Cheese" }]
                },

                // ══════════════════════════════════════════════════════════════
                // NEXT WEEK (w3) — all OrderDateTimes in the future → Active
                // ══════════════════════════════════════════════════════════════

                new Order
                {
                    Title = "Sophia - Wedding Shower",
                    OrderDateTime = w3Tue.AddHours(14),
                    CustName = "Sophia Martinez", CustPhone = "555-345-6789", CustEmail = "sophia.m@email.com",
                    Details = "Wedding shower cupcakes, white & gold",
                    OrderType = "Delivery", DeliveryLocation = "456 Oak Ave, Suite 2",
                    TotalCost = 110.00, DepositAmount = 55.00, DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-20), ContractSent = true,
                    Cupcakes = [
                        new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 24, CupcakeFlavor = "Lemon",     FillingFlavor = "Lemon Curd", IcingFlavor = "Vanilla Buttercream" },
                        new Cupcake { CupcakeSize = "Mini",    CupcakeQuantity = 24, CupcakeFlavor = "Champagne", FillingFlavor = "None",       IcingFlavor = "Gold Shimmer"        }
                    ]
                },
                new Order
                {
                    Title = "Hannah - Baby Shower",
                    OrderDateTime = w3Wed.AddHours(15),
                    CustName = "Hannah Lee", CustPhone = "555-890-1234", CustEmail = "hannah.l@email.com",
                    Details = "Baby shower — it's a girl! Pink and white",
                    OrderType = "Delivery", DeliveryLocation = "321 Maple Dr",
                    TotalCost = 85.00, DepositAmount = 45.00, DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-18), ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla", IcingFlavor = "Pink Buttercream", SplitTier = true, Flavor2 = "Vanilla" }]
                },
                new Order
                {
                    Title = "Lauren - Engagement Party",
                    OrderDateTime = w3Wed.AddHours(13),
                    CustName = "Lauren Webb", CustPhone = "555-456-7891",
                    Details = "Engagement party — mini cupcake tower, 60 pcs",
                    OrderType = "Delivery", DeliveryLocation = "88 Willow Way",
                    TotalCost = 145.00, DepositAmount = 75.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-25), ContractSent = true,
                    Cupcakes = [
                        new Cupcake { CupcakeSize = "Mini", CupcakeQuantity = 30, CupcakeFlavor = "Champagne", FillingFlavor = "None",     IcingFlavor = "Blush Buttercream" },
                        new Cupcake { CupcakeSize = "Mini", CupcakeQuantity = 30, CupcakeFlavor = "Lemon",     FillingFlavor = "Raspberry", IcingFlavor = "White Chocolate"   }
                    ]
                },
                new Order
                {
                    Title = "Nicole - Wedding Cake",
                    OrderDateTime = w3Thu.AddHours(9),
                    CustName = "Nicole Foster", CustPhone = "555-012-3456", CustEmail = "nicole.f@email.com",
                    Details = "3-tier wedding cake, white & gold",
                    OrderType = "Delivery", DeliveryLocation = "The Grand Ballroom, 100 Main St",
                    SecondaryName = "Wedding Coordinator", SecondaryPhone = "555-111-9999",
                    TotalCost = 420.00, DepositAmount = 210.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-45), ContractSent = true, DayOfTextSent = true,
                    Cakes = [
                        new Cake { TierSize = "12\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Champagne", FillingFlavor = "Raspberry",  IcingFlavor = "White Chocolate",    SplitTier = false },
                        new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla",   FillingFlavor = "Vanilla",    IcingFlavor = "Vanilla Buttercream", SplitTier = false },
                        new Cake { TierSize = "8\"",  NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Lemon",     FillingFlavor = "Lemon Curd", IcingFlavor = "Blush Buttercream",  SplitTier = false }
                    ]
                },
                new Order
                {
                    Title = "Tyler - Graduation Cake",
                    OrderDateTime = w3Thu.AddHours(17),
                    CustName = "Tyler Davis", CustPhone = "555-555-6677",
                    Details = "Graduation cake, navy blue and white",
                    OrderType = "Delivery", DeliveryLocation = "900 Campus Blvd",
                    TotalCost = 88.00, DepositAmount = 44.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-19), ContractSent = true,
                    Cakes = [new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Chocolate", FillingFlavor = "Chocolate Ganache", IcingFlavor = "Chocolate Buttercream", SplitTier = false }]
                },
                new Order
                {
                    Title = "Jessica - Quinceañera",
                    OrderDateTime = w3Fri.AddHours(9),
                    CustName = "Jessica Brown", CustPhone = "555-666-7788", CustEmail = "jess.b@email.com",
                    Details = "Quinceañera cake — 3 tiers, lavender and silver",
                    OrderType = "Delivery", DeliveryLocation = "El Paraiso Banquet Hall, 200 Fiesta Rd",
                    SecondaryName = "Gloria Brown", SecondaryPhone = "555-666-0001",
                    TotalCost = 280.00, DepositAmount = 140.00, DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-30), ContractSent = true, DayOfTextSent = true,
                    Cakes = [
                        new Cake { TierSize = "12\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla",    FillingFlavor = "Raspberry",  IcingFlavor = "Lavender Buttercream", SplitTier = false },
                        new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Strawberry", FillingFlavor = "Strawberry", IcingFlavor = "Lavender Buttercream", SplitTier = false },
                        new Cake { TierSize = "8\"",  NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Champagne",  FillingFlavor = "None",       IcingFlavor = "Gold Shimmer",         SplitTier = false }
                    ]
                },
                new Order
                {
                    Title = "Sarah - Sweet 16",
                    OrderDateTime = w3Fri.AddHours(14),
                    CustName = "Sarah Johnson", CustPhone = "555-222-3344",
                    Details = "Sweet 16 cake, 2 tier, purple and silver",
                    OrderType = "Pickup",
                    TotalCost = 130.00, DepositAmount = 65.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-12), ContractSent = true,
                    Cakes = [
                        new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla",  FillingFlavor = "Raspberry", IcingFlavor = "Lavender Buttercream", SplitTier = false },
                        new Cake { TierSize = "8\"",  NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla",   IcingFlavor = "Lavender Buttercream", SplitTier = false }
                    ]
                },
                new Order
                {
                    Title = "Amanda - Baby Shower Cupcakes",
                    OrderDateTime = w3Fri.AddHours(16),
                    CustName = "Amanda White", CustPhone = "555-444-5566", CustEmail = "amanda.w@email.com",
                    Details = "Baby shower cupcakes — gender neutral, yellow and white",
                    OrderType = "Pickup",
                    TotalCost = 72.00, DepositAmount = 36.00, DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-10),
                    Cupcakes = [new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 36, CupcakeFlavor = "Vanilla", FillingFlavor = "Vanilla", IcingFlavor = "Vanilla Buttercream" }]
                },
                new Order
                {
                    Title = "Ashley - Dog Birthday",
                    OrderDateTime = w3Fri.AddHours(13),
                    CustName = "Ashley Anderson", CustPhone = "555-110-2233", CustEmail = "ashley.a@email.com",
                    Details = "Dog birthday — pupcake dozen",
                    OrderType = "Delivery", DeliveryLocation = "17 Bark Ave",
                    TotalCost = 36.00, DepositAmount = 36.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-5), PaidInFull = true,
                    Pupcakes = [new Pupcake { PupcakeSize = "Regular", PupcakeQuantity = 12 }]
                },
                new Order
                {
                    Title = "Stephanie - Bachelorette Cake",
                    OrderDateTime = w3Sat.AddHours(15),
                    CustName = "Stephanie Jackson", CustPhone = "555-332-4455", CustEmail = "steph.j@email.com",
                    Details = "Bachelorette party — 3 tier cake, blush pink",
                    OrderType = "Delivery", DeliveryLocation = "Suite 5, 600 Party Blvd",
                    TotalCost = 185.00, DepositAmount = 95.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-22), ContractSent = true,
                    Cakes = [
                        new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Strawberry", FillingFlavor = "Strawberry", IcingFlavor = "Blush Buttercream", SplitTier = false },
                        new Cake { TierSize = "8\"",  NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla",    FillingFlavor = "Raspberry",  IcingFlavor = "Blush Buttercream", SplitTier = false },
                        new Cake { TierSize = "6\"",  NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Lemon",      FillingFlavor = "Lemon Curd", IcingFlavor = "White Chocolate",   SplitTier = false }
                    ]
                },
                new Order
                {
                    Title = "Heather - Bridal Shower",
                    OrderDateTime = w3Sat.AddHours(14),
                    CustName = "Heather Thompson", CustPhone = "555-776-8899", CustEmail = "heather.t@email.com",
                    Details = "Bridal shower cupcakes, 2 doz mini, rose gold",
                    OrderType = "Pickup",
                    TotalCost = 52.00, DepositAmount = 26.00, DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-15), ContractSent = true,
                    Cupcakes = [new Cupcake { CupcakeSize = "Mini", CupcakeQuantity = 24, CupcakeFlavor = "Champagne", FillingFlavor = "None", IcingFlavor = "Blush Buttercream" }]
                },

                // ══════════════════════════════════════════════════════════════
                // SPECIAL STATUS ORDERS (all w3 dates — future)
                // ══════════════════════════════════════════════════════════════

                // Ready for Pickup
                new Order
                {
                    Title = "Lisa - Birthday Cake",
                    OrderDateTime = w3Tue.AddHours(11),
                    CustName = "Lisa Rodriguez", CustPhone = "555-201-3004", CustEmail = "lisa.r@email.com",
                    Details = "Birthday cake — baked and ready for pickup",
                    OrderType = "Pickup",
                    IsReadyForPickup = true,
                    TotalCost = 65.00, DepositAmount = 65.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-10), PaidInFull = true, ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Chocolate", FillingFlavor = "Chocolate Ganache", IcingFlavor = "Chocolate Buttercream", SplitTier = false }]
                },

                // Incomplete — has title, name, contact, date, type but no items
                new Order
                {
                    Title = "Pat - Anniversary Inquiry",
                    OrderDateTime = w3Wed.AddHours(14),
                    CustName = "Pat Wilson", CustPhone = "555-100-2003", CustEmail = "pat.w@email.com",
                    Details = "Inquired about anniversary cake — no items added yet",
                    OrderType = "Pickup",
                    DateOrderPlaced = today.AddDays(-1)
                    // No items → incomplete
                },

                // Incomplete — has title and name but missing both phone and email
                new Order
                {
                    Title = "Sam - Custom Design",
                    OrderDateTime = w3Fri.AddHours(16),
                    CustName = "Sam Rivera",
                    CustPhone = null,  // no phone
                    CustEmail = null,  // no email → contact condition triggers
                    Details = "Custom design — waiting on contact info from customer",
                    OrderType = "Delivery", DeliveryLocation = "TBD",
                    DateOrderPlaced = today.AddDays(-2)
                    // Missing phone + email → incomplete
                },

                // Cancelled — future date
                new Order
                {
                    Title = "Tom - Birthday Party",
                    OrderDateTime = w3Thu.AddHours(15),
                    CustName = "Tom Bradley", CustPhone = "555-301-4005", CustEmail = "tom.b@email.com",
                    Details = "Birthday party — cancelled by customer",
                    OrderType = "Pickup",
                    CancelledFlag = true,
                    CancellationReason = "Customer cancelled — event called off last minute",
                    CancelledAt = today.AddDays(-3),
                    TotalCost = 75.00, DepositAmount = 40.00, DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-18), ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla", IcingFlavor = "Buttercream", SplitTier = false }]
                },

                // Cancelled — tasting appointment, future date
                new Order
                {
                    Title = "Diana - Tasting",
                    OrderDateTime = w3Sat.AddHours(13),
                    CustName = "Diana Moore", CustPhone = "555-401-5006",
                    Details = "Tasting appointment — never confirmed flavors",
                    OrderType = "Tasting",
                    CancelledFlag = true,
                    CancellationReason = "No response after 3 follow-up attempts",
                    CancelledAt = today.AddDays(-1),
                    DateOrderPlaced = today.AddDays(-12)
                }
            );

            context.SaveChanges();
        }
    }
}
