using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CCCInventory
{
    // Wedding-only fields for an Order (1:1, keyed by OrderNumber). Everything that already
    // lives on Order (date/time, customer, items, pricing, delivery address) stays there so the
    // bake sheet, order lists, and incomplete detection are unaffected.
    public class WeddingDetails
    {
        [Key]
        public int Id { get; set; }
        public int OrderNumber { get; set; }
        [JsonIgnore]
        public Order? Order { get; set; }

        // Event
        public DateTime? EventDate { get; set; }
        public string? ReceptionLocation { get; set; }
        public bool? CeremonySameLocation { get; set; }
        public string? CeremonyTime { get; set; }        // "HH:mm"
        public string? ReceptionTime { get; set; }       // "HH:mm"

        // Contacts (Bride/Groom #1 is Order.CustName / CustPhone / CustEmail)
        public string? Partner2Name { get; set; }
        public string? Partner2Phone { get; set; }
        public string? DayOfContactTitle { get; set; }   // name/phone are Order.SecondaryName / SecondaryPhone
        public string? VenueContactName { get; set; }
        public string? VenueContactPhone { get; set; }

        // Contract
        public DateTime? ContractReturnByDate { get; set; }

        // Display / adornments
        public string? CakeBoardColor { get; set; }      // White / Gold / Silver / Black
        public bool? CakeTopper { get; set; }
        public bool? HasFlowers { get; set; }
        public string? FlowerType { get; set; }          // Live / Fake / Buttercream / N/A
        public string? FlowersProvidedBy { get; set; }   // Florist / Customer / CCC / N/A
        public string? FloristName { get; set; }
        public string? FloristPhone { get; set; }
        public string? FloristDeliveryTime { get; set; } // "HH:mm"

        // Delivery / pickup (start time + date are Order.OrderDateTime)
        public string? DeliveryWindowEnd { get; set; }   // "HH:mm"
        public string? PickupPersonName { get; set; }
        public string? PickupPersonPhone { get; set; }

        // Cake description
        public string? MainCakeFlavorDescription { get; set; }
        public string? MainCakeDesignDescription { get; set; }
        public string? KitchenCakeFlavorDescription { get; set; }
        public string? CupcakeFlavorDescription { get; set; }
        public string? CupcakeDesignDescription { get; set; }
        public int? TotalServings { get; set; }

        // Generated Google Doc contract (latest)
        public string? ContractDocId { get; set; }
        public string? ContractDocUrl { get; set; }
        public string? ContractDocName { get; set; }
        public DateTime? ContractGeneratedAt { get; set; }
    }
}
