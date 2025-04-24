// Payment.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RetailCycleShopAPI.models.enums;

namespace RetailCycleShopAPI.module
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int PaymentType { get; set; }
        //public string? TransactionId { get; set; } // Added this
        public decimal Amount { get; set; }
        public int Status { get; set; }
        public string ReceiptUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? OrderId { get; set; }

        [JsonIgnore]
        public Order? Order { get; set; }
    }
}