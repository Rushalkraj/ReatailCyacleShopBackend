using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using RetailCycleShopAPI.models.enums;
using RetailCycleShopAPI.Models;

namespace RetailCycleShopAPI.module
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        [EnumDataType(typeof(OrderStatus))]
        public int Status { get; set; } 
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public int ShippingAddressId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? PaymentId { get; set; }
        public int? EmployeeId { get; set; }

        public Customer Customer { get; set; } = null!;
        public Address ShippingAddress { get; set; } = null!;
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Payment? Payment { get; set; }
    }

}