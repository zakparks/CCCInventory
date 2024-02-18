using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Order
    {
        [Key]
        public int OrderNumber { get; set; }
        public DateTime? OrderDateTime { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? CustName { get; set; }
        public string? CustEmail { get; set; }
        public string? CustPhone { get; set; }
        List<string>? FormFiles { get; set; }
        public string? Details { get; set; }
        public bool? PickupOrDelivery { get; set; }
        public string? SecondaryName { get; set; }
        public string? SecondaryPhone { get; set; }
        public string? InitialContact { get; set; }
        public bool ContractSent { get; set; } = false;
        public bool DayOfTextSent { get; set; } = false;
        public bool ConfirmationTextSent { get; set; } = false;
        public List<Cake>? Cakes { get; set; }
        public List<Cupcake>? Cupcakes { get; set; }
        public List<Pupcake>? Pupcakes { get; set; }
        public List<Cookie>? Cookies { get; set; }
        public double? TotalCost { get; set; }
        public double? DepositAmount { get; set; }
        public string? DepositPaymentMethod { get; set; }
        public DateTime? DepositDateTime { get; set; }
        public string? FinalPaymentMethod { get; set; }
        public DateTime? FinalPaymentDateTime { get; set; }
        public DateTime DateOrderPlaced { get; set; }
        public bool? PaidInFull { get; set; }
        public bool DeleteFlag { get; set; } = false;
    }
}
