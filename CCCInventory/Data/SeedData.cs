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

            // Distribute 30 orders across the upcoming Tue–Sat (closed Sun & Mon).
            // Compute the next Tuesday from today (or next week's if today IS Tuesday).
            int daysUntilTuesday = ((int)DayOfWeek.Tuesday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilTuesday == 0) daysUntilTuesday = 7;
            var tue = today.AddDays(daysUntilTuesday);
            var wed = tue.AddDays(1);
            var thu = tue.AddDays(2);
            var fri = tue.AddDays(3);
            var sat = tue.AddDays(4);

            context.Orders.AddRange(

                // ── Tuesday (4 orders) ────────────────────────────────────────
                new Order
                {
                    OrderDateTime = tue.AddHours(10),
                    CustName = "Emily Johnson",
                    CustPhone = "555-234-5678",
                    CustEmail = "emily.j@email.com",
                    Details = "Birthday cake, pink floral theme",
                    PickupOrDelivery = false,
                    TotalCost = 95.00,
                    DepositAmount = 50.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-14),
                    ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla", FillingFlavor = "Strawberry", IcingFlavor = "Buttercream", SplitTier = false }]
                },
                new Order
                {
                    OrderDateTime = tue.AddHours(12),
                    CustName = "Marcus Williams",
                    CustPhone = "555-876-5432",
                    Details = "3 dozen chocolate chip cookies",
                    PickupOrDelivery = false,
                    TotalCost = 42.00,
                    DepositAmount = 42.00,
                    DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-5),
                    PaidInFull = true,
                    Cookies = [new Cookie { CookieType = "Chocolate Chip", CookieQuantity = 36 }]
                },
                new Order
                {
                    OrderDateTime = tue.AddHours(14),
                    CustName = "Sophia Martinez",
                    CustPhone = "555-345-6789",
                    CustEmail = "sophia.m@email.com",
                    Details = "Wedding shower cupcakes, white & gold",
                    PickupOrDelivery = true,
                    DeliveryLocation = "456 Oak Ave, Suite 2",
                    TotalCost = 110.00,
                    DepositAmount = 55.00,
                    DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-20),
                    ContractSent = true,
                    Cupcakes = [
                        new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 24, CupcakeFlavor = "Lemon", FillingFlavor = "Lemon Curd", IcingFlavor = "Vanilla Buttercream" },
                        new Cupcake { CupcakeSize = "Mini", CupcakeQuantity = 24, CupcakeFlavor = "Champagne", FillingFlavor = "None", IcingFlavor = "Gold Shimmer" }
                    ]
                },
                new Order
                {
                    OrderDateTime = tue.AddHours(16),
                    CustName = "David Chen",
                    CustPhone = "555-456-7890",
                    Details = "Graduation cake, navy and gold",
                    PickupOrDelivery = false,
                    TotalCost = 75.00,
                    DepositAmount = 40.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-7),
                    Cakes = [new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Red Velvet", FillingFlavor = "Cream Cheese", IcingFlavor = "Cream Cheese", SplitTier = false }]
                },

                // ── Wednesday (5 orders) ──────────────────────────────────────
                new Order
                {
                    OrderDateTime = wed.AddHours(9),
                    CustName = "Priya Patel",
                    CustPhone = "555-567-8901",
                    CustEmail = "priya.p@email.com",
                    Details = "Pupcake dozen for Biscuit's 3rd birthday!",
                    PickupOrDelivery = false,
                    TotalCost = 36.00,
                    DepositAmount = 36.00,
                    DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-3),
                    PaidInFull = true,
                    Pupcakes = [new Pupcake { PupcakeSize = "Regular", PupcakeQuantity = 12 }]
                },
                new Order
                {
                    OrderDateTime = wed.AddHours(11),
                    CustName = "Rachel Thompson",
                    CustPhone = "555-678-9012",
                    CustEmail = "rachel.t@email.com",
                    Details = "Retirement party — sheet cake, golf theme",
                    PickupOrDelivery = false,
                    TotalCost = 65.00,
                    DepositAmount = 33.00,
                    DepositPaymentMethod = "Check",
                    DateOrderPlaced = today.AddDays(-21),
                    ContractSent = true,
                    Cakes = [new Cake { TierSize = "Half Sheet", NumTierLayers = 1, CakeShape = "Round", CakeFlavor = "White", FillingFlavor = "None", IcingFlavor = "Vanilla", SplitTier = false }]
                },
                new Order
                {
                    OrderDateTime = wed.AddHours(13),
                    CustName = "James O'Brien",
                    CustPhone = "555-789-0123",
                    Details = "2 dozen M&M cookies + 1 dozen peanut butter",
                    PickupOrDelivery = false,
                    TotalCost = 48.00,
                    DepositAmount = 48.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-4),
                    PaidInFull = true,
                    Cookies = [
                        new Cookie { CookieType = "M&M", CookieQuantity = 24 },
                        new Cookie { CookieType = "Peanut Butter", CookieQuantity = 12 }
                    ]
                },
                new Order
                {
                    OrderDateTime = wed.AddHours(15),
                    CustName = "Hannah Lee",
                    CustPhone = "555-890-1234",
                    CustEmail = "hannah.l@email.com",
                    Details = "Baby shower — it's a girl! Pink and white",
                    PickupOrDelivery = true,
                    DeliveryLocation = "321 Maple Dr",
                    TotalCost = 85.00,
                    DepositAmount = 45.00,
                    DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-18),
                    ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla", IcingFlavor = "Pink Buttercream", SplitTier = true }]
                },
                new Order
                {
                    OrderDateTime = wed.AddHours(17),
                    CustName = "Carlos Reyes",
                    CustPhone = "555-901-2345",
                    Details = "Office birthday, chocolate sheet cake",
                    PickupOrDelivery = false,
                    TotalCost = 55.00,
                    DepositAmount = 55.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-6),
                    PaidInFull = true,
                    Cakes = [new Cake { TierSize = "Quarter Sheet", NumTierLayers = 1, CakeShape = "Round", CakeFlavor = "Chocolate", FillingFlavor = "None", IcingFlavor = "Chocolate Buttercream", SplitTier = false }]
                },

                // ── Thursday (10 orders) ──────────────────────────────────────
                new Order
                {
                    OrderDateTime = thu.AddHours(9),
                    CustName = "Nicole Foster",
                    CustPhone = "555-012-3456",
                    CustEmail = "nicole.f@email.com",
                    Details = "3-tier wedding cake, white & gold",
                    PickupOrDelivery = true,
                    DeliveryLocation = "The Grand Ballroom, 100 Main St",
                    SecondaryName = "Wedding Coordinator",
                    SecondaryPhone = "555-111-9999",
                    TotalCost = 420.00,
                    DepositAmount = 210.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-45),
                    ContractSent = true,
                    DayOfTextSent = true,
                    Cakes = [
                        new Cake { TierSize = "12\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Champagne", FillingFlavor = "Raspberry", IcingFlavor = "White Chocolate", SplitTier = false },
                        new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla", FillingFlavor = "Vanilla", IcingFlavor = "Vanilla Buttercream", SplitTier = false },
                        new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Lemon", FillingFlavor = "Lemon Curd", IcingFlavor = "Blush Buttercream", SplitTier = false }
                    ]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(10),
                    CustName = "Andrew Kim",
                    CustPhone = "555-123-4567",
                    Details = "Anniversary cake, heart shape, red velvet",
                    PickupOrDelivery = false,
                    TotalCost = 68.00,
                    DepositAmount = 35.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-11),
                    ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Heart", CakeFlavor = "Red Velvet", FillingFlavor = "Cream Cheese", IcingFlavor = "Cream Cheese", SplitTier = false }]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(11),
                    CustName = "Tiffany Brooks",
                    CustPhone = "555-234-5679",
                    Details = "4 dozen sugar cookies, fall theme",
                    PickupOrDelivery = false,
                    TotalCost = 64.00,
                    DepositAmount = 32.00,
                    DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-8),
                    Cookies = [new Cookie { CookieType = "Sugar Cookie", CookieQuantity = 48 }]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(11).AddMinutes(30),
                    CustName = "Brittany Evans",
                    CustPhone = "555-345-6780",
                    CustEmail = "bev@email.com",
                    Details = "Puppy birthday — 2 doz regular pupcakes + 6 cake pupcakes",
                    PickupOrDelivery = false,
                    TotalCost = 58.00,
                    DepositAmount = 30.00,
                    DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-9),
                    Pupcakes = [
                        new Pupcake { PupcakeSize = "Regular", PupcakeQuantity = 24 },
                        new Pupcake { PupcakeSize = "Cake", PupcakeQuantity = 6 }
                    ]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(12),
                    CustName = "Michael Scott",
                    CustPhone = "555-867-5309",
                    CustEmail = "mscott@dundermifflin.com",
                    Details = "Promotion congrats cake, chocolate",
                    PickupOrDelivery = true,
                    DeliveryLocation = "1725 Slough Ave, Scranton",
                    TotalCost = 90.00,
                    DepositAmount = 45.00,
                    DepositPaymentMethod = "Credit Card",
                    DateOrderPlaced = today.AddDays(-16),
                    ContractSent = true,
                    Cakes = [new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Square", CakeFlavor = "Chocolate", FillingFlavor = "Chocolate Mousse", IcingFlavor = "Dark Chocolate Ganache", SplitTier = false }]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(13),
                    CustName = "Lauren Webb",
                    CustPhone = "555-456-7891",
                    Details = "Engagement party — mini cupcake tower, 60 pcs",
                    PickupOrDelivery = true,
                    DeliveryLocation = "88 Willow Way",
                    TotalCost = 145.00,
                    DepositAmount = 75.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-25),
                    ContractSent = true,
                    Cupcakes = [
                        new Cupcake { CupcakeSize = "Mini", CupcakeQuantity = 30, CupcakeFlavor = "Champagne", FillingFlavor = "None", IcingFlavor = "Blush Buttercream" },
                        new Cupcake { CupcakeSize = "Mini", CupcakeQuantity = 30, CupcakeFlavor = "Lemon", FillingFlavor = "Raspberry", IcingFlavor = "White Chocolate" }
                    ]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(14),
                    CustName = "Sarah Johnson",
                    CustPhone = "555-222-3344",
                    Details = "Sweet 16 cake, 2 tier, purple and silver",
                    PickupOrDelivery = false,
                    TotalCost = 130.00,
                    DepositAmount = 65.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-12),
                    ContractSent = true,
                    Cakes = [
                        new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla", FillingFlavor = "Raspberry", IcingFlavor = "Lavender Buttercream", SplitTier = false },
                        new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla", IcingFlavor = "Lavender Buttercream", SplitTier = false }
                    ]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(15),
                    CustName = "Kevin Park",
                    CustPhone = "555-333-4455",
                    Details = "Office cookie order — choc chip and oatmeal raisin",
                    PickupOrDelivery = false,
                    TotalCost = 54.00,
                    DepositAmount = 54.00,
                    DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-3),
                    PaidInFull = true,
                    Cookies = [
                        new Cookie { CookieType = "Chocolate Chip", CookieQuantity = 24 },
                        new Cookie { CookieType = "Oatmeal Raisin", CookieQuantity = 24 }
                    ]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(16),
                    CustName = "Amanda White",
                    CustPhone = "555-444-5566",
                    CustEmail = "amanda.w@email.com",
                    Details = "Baby shower cupcakes — gender neutral, yellow and white",
                    PickupOrDelivery = false,
                    TotalCost = 72.00,
                    DepositAmount = 36.00,
                    DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-10),
                    Cupcakes = [new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 36, CupcakeFlavor = "Vanilla", FillingFlavor = "Vanilla", IcingFlavor = "Vanilla Buttercream" }]
                },
                new Order
                {
                    OrderDateTime = thu.AddHours(17),
                    CustName = "Tyler Davis",
                    CustPhone = "555-555-6677",
                    Details = "Graduation cake, navy blue and white",
                    PickupOrDelivery = true,
                    DeliveryLocation = "900 Campus Blvd",
                    TotalCost = 88.00,
                    DepositAmount = 44.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-19),
                    ContractSent = true,
                    Cakes = [new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Chocolate", FillingFlavor = "Chocolate Ganache", IcingFlavor = "Chocolate Buttercream", SplitTier = false }]
                },

                // ── Friday (8 orders) ─────────────────────────────────────────
                new Order
                {
                    OrderDateTime = fri.AddHours(9),
                    CustName = "Jessica Brown",
                    CustPhone = "555-666-7788",
                    CustEmail = "jess.b@email.com",
                    Details = "Quinceañera cake — 3 tiers, lavender and silver",
                    PickupOrDelivery = true,
                    DeliveryLocation = "El Paraiso Banquet Hall, 200 Fiesta Rd",
                    SecondaryName = "Gloria Brown",
                    SecondaryPhone = "555-666-0001",
                    TotalCost = 280.00,
                    DepositAmount = 140.00,
                    DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-30),
                    ContractSent = true,
                    DayOfTextSent = true,
                    Cakes = [
                        new Cake { TierSize = "12\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla", FillingFlavor = "Raspberry", IcingFlavor = "Lavender Buttercream", SplitTier = false },
                        new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Strawberry", FillingFlavor = "Strawberry", IcingFlavor = "Lavender Buttercream", SplitTier = false },
                        new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Champagne", FillingFlavor = "None", IcingFlavor = "Gold Shimmer", SplitTier = false }
                    ]
                },
                new Order
                {
                    OrderDateTime = fri.AddHours(10),
                    CustName = "Ryan Miller",
                    CustPhone = "555-777-8899",
                    Details = "3 dozen gingerbread cookies, iced",
                    PickupOrDelivery = false,
                    TotalCost = 48.00,
                    DepositAmount = 48.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-2),
                    PaidInFull = true,
                    Cookies = [new Cookie { CookieType = "Gingerbread", CookieQuantity = 36 }]
                },
                new Order
                {
                    OrderDateTime = fri.AddHours(11),
                    CustName = "Megan Wilson",
                    CustPhone = "555-888-9900",
                    CustEmail = "megan.w@email.com",
                    Details = "Work birthday — 2 doz cupcakes, mixed flavors",
                    PickupOrDelivery = false,
                    TotalCost = 48.00,
                    DepositAmount = 48.00,
                    DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-4),
                    PaidInFull = true,
                    Cupcakes = [
                        new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 12, CupcakeFlavor = "Chocolate", FillingFlavor = "None", IcingFlavor = "Chocolate Buttercream" },
                        new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 12, CupcakeFlavor = "Vanilla", FillingFlavor = "None", IcingFlavor = "Vanilla Buttercream" }
                    ]
                },
                new Order
                {
                    OrderDateTime = fri.AddHours(12),
                    CustName = "Jason Taylor",
                    CustPhone = "555-999-0011",
                    Details = "Birthday cake, funfetti with rainbow sprinkles",
                    PickupOrDelivery = false,
                    TotalCost = 65.00,
                    DepositAmount = 33.00,
                    DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-8),
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla", IcingFlavor = "Vanilla Buttercream", SplitTier = false }]
                },
                new Order
                {
                    OrderDateTime = fri.AddHours(13),
                    CustName = "Ashley Anderson",
                    CustPhone = "555-110-2233",
                    CustEmail = "ashley.a@email.com",
                    Details = "Dog birthday — pupcake dozen",
                    PickupOrDelivery = true,
                    DeliveryLocation = "17 Bark Ave",
                    TotalCost = 36.00,
                    DepositAmount = 36.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-5),
                    PaidInFull = true,
                    Pupcakes = [new Pupcake { PupcakeSize = "Regular", PupcakeQuantity = 12 }]
                },
                new Order
                {
                    OrderDateTime = fri.AddHours(14),
                    CustName = "Brandon Thomas",
                    CustPhone = "555-221-3344",
                    Details = "2 dozen pumpkin cookies",
                    PickupOrDelivery = false,
                    TotalCost = 32.00,
                    DepositAmount = 32.00,
                    DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-1),
                    PaidInFull = true,
                    Cookies = [new Cookie { CookieType = "Pumpkin", CookieQuantity = 24 }]
                },
                new Order
                {
                    OrderDateTime = fri.AddHours(15),
                    CustName = "Stephanie Jackson",
                    CustPhone = "555-332-4455",
                    CustEmail = "steph.j@email.com",
                    Details = "Bachelorette party — 3 tier cake, blush pink",
                    PickupOrDelivery = true,
                    DeliveryLocation = "Suite 5, 600 Party Blvd",
                    TotalCost = 185.00,
                    DepositAmount = 95.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-22),
                    ContractSent = true,
                    Cakes = [
                        new Cake { TierSize = "10\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Strawberry", FillingFlavor = "Strawberry", IcingFlavor = "Blush Buttercream", SplitTier = false },
                        new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Vanilla", FillingFlavor = "Raspberry", IcingFlavor = "Blush Buttercream", SplitTier = false },
                        new Cake { TierSize = "6\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Lemon", FillingFlavor = "Lemon Curd", IcingFlavor = "White Chocolate", SplitTier = false }
                    ]
                },
                new Order
                {
                    OrderDateTime = fri.AddHours(17),
                    CustName = "Christopher White",
                    CustPhone = "555-443-5566",
                    Details = "Farewell party cupcakes, 18 regular",
                    PickupOrDelivery = false,
                    TotalCost = 36.00,
                    DepositAmount = 18.00,
                    DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-6),
                    Cupcakes = [new Cupcake { CupcakeSize = "Regular", CupcakeQuantity = 18, CupcakeFlavor = "Red Velvet", FillingFlavor = "None", IcingFlavor = "Cream Cheese" }]
                },

                // ── Saturday (3 orders) ───────────────────────────────────────
                new Order
                {
                    OrderDateTime = sat.AddHours(10),
                    CustName = "Katie Harris",
                    CustPhone = "555-554-6677",
                    Details = "Kids' birthday cake, rainbow theme",
                    PickupOrDelivery = false,
                    TotalCost = 70.00,
                    DepositAmount = 35.00,
                    DepositPaymentMethod = "Cash",
                    DateOrderPlaced = today.AddDays(-13),
                    ContractSent = true,
                    Cakes = [new Cake { TierSize = "8\"", NumTierLayers = 2, CakeShape = "Round", CakeFlavor = "Funfetti", FillingFlavor = "Vanilla", IcingFlavor = "Buttercream", SplitTier = false }]
                },
                new Order
                {
                    OrderDateTime = sat.AddHours(12),
                    CustName = "Daniel Martin",
                    CustPhone = "555-665-7788",
                    Details = "4 dozen assorted cookies",
                    PickupOrDelivery = false,
                    TotalCost = 64.00,
                    DepositAmount = 32.00,
                    DepositPaymentMethod = "Venmo",
                    DateOrderPlaced = today.AddDays(-7),
                    Cookies = [
                        new Cookie { CookieType = "Chocolate Chip", CookieQuantity = 24 },
                        new Cookie { CookieType = "Sugar Cookie", CookieQuantity = 24 }
                    ]
                },
                new Order
                {
                    OrderDateTime = sat.AddHours(14),
                    CustName = "Heather Thompson",
                    CustPhone = "555-776-8899",
                    CustEmail = "heather.t@email.com",
                    Details = "Bridal shower cupcakes, 2 doz mini, rose gold",
                    PickupOrDelivery = false,
                    TotalCost = 52.00,
                    DepositAmount = 26.00,
                    DepositPaymentMethod = "Zelle",
                    DateOrderPlaced = today.AddDays(-15),
                    ContractSent = true,
                    Cupcakes = [new Cupcake { CupcakeSize = "Mini", CupcakeQuantity = 24, CupcakeFlavor = "Champagne", FillingFlavor = "None", IcingFlavor = "Blush Buttercream" }]
                }
            );

            context.SaveChanges();
        }
    }
}
