using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Course_management.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string PaymentMethod { get; set; } // e.g., "Credit Card", "PayPal", etc.

        [Required]
        public string TransactionId { get; set; } // External payment processor transaction ID

        [Required]
        public string Status { get; set; } // "Pending", "Completed", "Failed", "Refunded"

        public string? ReceiptUrl { get; set; } // URL to payment receipt if available

        public string? Notes { get; set; } // Any additional notes about the payment
    }
}