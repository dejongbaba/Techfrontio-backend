using System;

namespace Course_management.Dto
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string ReceiptUrl { get; set; }
    }

    public class PaymentCreateDto
    {
        public int CourseId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
    }

    public class PaymentUpdateDto
    {
        public string Status { get; set; }
        public string ReceiptUrl { get; set; }
        public string Notes { get; set; }
    }
}