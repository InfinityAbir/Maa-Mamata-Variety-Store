using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        public string CustomerAddress { get; set; } = string.Empty;

        [Required]
        public string CustomerPhone { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; } = "Cash on Delivery";

        public string Status { get; set; } = "Pending";

        // Navigation property
        public virtual List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
