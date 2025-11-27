using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Course_management.Data;
using Course_management.Models;
using Course_management.Dto;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System;
using Course_management.Interfaces;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Course_management.Controllers
{
    [Route("api/payments")]
    [ApiController]
    [Authorize] // All payment endpoints require authentication
    public class PaymentsController : ControllerBase
    {
        private readonly IPaystackService _paystackService;
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public PaymentsController(IPaystackService paystackService, DataContext context, UserManager<User> userManager)
        {
            _paystackService = paystackService;
            _context = context;
            _userManager = userManager;
        }

        // GET: api/payments - Get all payments (Admin only)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                .ToListAsync();

            var paymentDtos = payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserName = p.User?.FullName,
                CourseId = p.CourseId,
                CourseTitle = p.Course?.Title,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                Status = p.Status,
                ReceiptUrl = p.ReceiptUrl
            }).ToList();

            return Ok(ApiResponse<List<PaymentDto>>.Success(paymentDtos, "Payments retrieved successfully", 200));
        }

        // GET api/payments/{id} - Get payment by ID (Admin or payment owner)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound(ApiResponse.Error("Payment not found", 404));

            // Check if user is admin or payment owner
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && payment.UserId != userId)
                return Forbid();

            var paymentDto = new PaymentDto
            {
                Id = payment.Id,
                UserId = payment.UserId,
                UserName = payment.User?.FullName,
                CourseId = payment.CourseId,
                CourseTitle = payment.Course?.Title,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                Status = payment.Status,
                ReceiptUrl = payment.ReceiptUrl
            };

            return Ok(ApiResponse<PaymentDto>.Success(paymentDto, "Payment retrieved successfully", 200));
        }

        // GET api/payments/user - Get current user's payments
        [HttpGet("user")]
        public async Task<IActionResult> GetUserPayments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            var payments = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var paymentDtos = payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
                CourseId = p.CourseId,
                CourseTitle = p.Course?.Title,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                Status = p.Status,
                ReceiptUrl = p.ReceiptUrl
            }).ToList();

            return Ok(ApiResponse<List<PaymentDto>>.Success(paymentDtos, "User payments retrieved successfully", 200));
        }

        // GET api/payments/course/{courseId} - Get payments for a specific course (Admin or Tutor)
        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Admin,Tutor")]
        public async Task<IActionResult> GetCoursePayments(int courseId)
        {
            // Check if course exists
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            // If tutor, check if they own the course
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isTutor = User.IsInRole("Tutor");

            if (isTutor && !isAdmin && course.TutorId != userId)
                return Forbid();

            var payments = await _context.Payments
                .Include(p => p.User)
                .Where(p => p.CourseId == courseId)
                .ToListAsync();

            var paymentDtos = payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserName = p.User?.FullName,
                CourseId = p.CourseId,
                CourseTitle = course.Title,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                Status = p.Status,
                ReceiptUrl = p.ReceiptUrl
            }).ToList();

            return Ok(ApiResponse<List<PaymentDto>>.Success(paymentDtos, "Course payments retrieved successfully", 200));
        }

        // POST api/payments - Create a new payment
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateDto paymentDto)
        {
            if (paymentDto == null)
                return BadRequest(ApiResponse.Error("Payment data is required", 400));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Check if course exists
            var course = await _context.Courses.FindAsync(paymentDto.CourseId);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            // Check if user is already enrolled in the course
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == paymentDto.CourseId);

            if (existingEnrollment != null)
                return BadRequest(ApiResponse.Error("You are already enrolled in this course", 400));

            // Create payment record
            var payment = new Payment
            {
                UserId = userId,
                CourseId = paymentDto.CourseId,
                Amount = paymentDto.Amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = paymentDto.PaymentMethod,
                TransactionId = paymentDto.TransactionId,
                Status = "Completed" // Assuming payment is successful immediately for simplicity
            };

            _context.Payments.Add(payment);

            // Create enrollment record
            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = paymentDto.CourseId
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Return payment details
            var createdPaymentDto = new PaymentDto
            {
                Id = payment.Id,
                UserId = payment.UserId,
                CourseId = payment.CourseId,
                CourseTitle = course.Title,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                Status = payment.Status,
                ReceiptUrl = payment.ReceiptUrl
            };

            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, 
                ApiResponse<PaymentDto>.Success(createdPaymentDto, "Payment processed and enrollment created successfully", 201));
        }

        // PUT api/payments/{id} - Update payment status (Admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePayment(int id, [FromBody] PaymentUpdateDto paymentDto)
        {
            if (paymentDto == null)
                return BadRequest(ApiResponse.Error("Payment update data is required", 400));

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound(ApiResponse.Error("Payment not found", 404));

            // Update payment properties
            payment.Status = paymentDto.Status ?? payment.Status;
            payment.ReceiptUrl = paymentDto.ReceiptUrl ?? payment.ReceiptUrl;
            payment.Notes = paymentDto.Notes ?? payment.Notes;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Payment updated successfully", 200));
        }

        // DELETE api/payments/{id} - Delete a payment (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound(ApiResponse.Error("Payment not found", 404));

            // Check if there's an enrollment associated with this payment
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == payment.UserId && e.CourseId == payment.CourseId);

            // Remove the enrollment if it exists
            if (enrollment != null)
            {
                // Also delete any reviews by this user for this course
                var reviews = await _context.Reviews
                    .Where(r => r.UserId == payment.UserId && r.CourseId == payment.CourseId)
                    .ToListAsync();

                _context.Reviews.RemoveRange(reviews);
                _context.Enrollments.Remove(enrollment);
            }

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Payment and related data deleted successfully", 200));
        }

        [HttpPost("initiate")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiationDto paymentDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var course = await _context.Courses.Include(c => c.Tutor).FirstOrDefaultAsync(c => c.Id == paymentDto.CourseId);
        
            if (course == null) return NotFound(ApiResponse.Error("Course not found", 404));
        
            var amountInKobo = (int)(course.Price * 100);
        
            var transactionRequest = new TransactionInitializeRequestDto
            {
                Email = user.Email,
                Amount = amountInKobo.ToString(),
                Callback_url = paymentDto.CallbackUrl,
                Subaccount = course.Tutor.PaystackSubaccountId,
                Metadata = new Dictionary<string, object>
                {
                    { "course_id", course.Id },
                    { "user_id", userId }
                }
            };
        
            var transactionResponse = await _paystackService.InitializeTransaction(transactionRequest);
        
            var payment = new Payment
            {
                UserId = userId,
                CourseId = course.Id,
                Amount = course.Price,
                TransactionId = transactionResponse.Data.Reference,
                Status = "Pending",
                PaymentMethod = "Paystack"
            };
        
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
        
            return Ok(ApiResponse<TransactionInitializeResponseDto.TransactionData>.Success(transactionResponse.Data, "Payment initiated successfully", 200));
        }

        [HttpPost("webhook")]
        [AllowAnonymous] // Webhooks are not authenticated
        public async Task<IActionResult> PaystackWebhook()
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["x-paystack-signature"].ToString();
        
            if (!_paystackService.VerifySignature(signature, requestBody))
            {
                return Unauthorized(ApiResponse.Error("Invalid signature", 401));
            }
        
            var webhookEvent = JsonDocument.Parse(requestBody).RootElement;
            var eventType = webhookEvent.GetProperty("event").GetString();
        
            if (eventType == "charge.success")
            {
                var reference = webhookEvent.GetProperty("data").GetProperty("reference").GetString();
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
        
                if (payment != null && payment.Status == "Pending")
                {
                    var verification = await _paystackService.VerifyTransaction(reference);
                    if (verification.Data.Status == "success")
                    {
                        payment.Status = "Completed";
        
                        var enrollment = new Enrollment
                        {
                            UserId = payment.UserId,
                            CourseId = payment.CourseId,
                            EnrollmentDate = System.DateTime.UtcNow
                        };
                        _context.Enrollments.Add(enrollment);
        
                        await _context.SaveChangesAsync();
                    }
                }
            }
        
            return Ok();
        }

        public class PaymentInitiationDto
        {
            public int CourseId { get; set; }
            public string CallbackUrl { get; set; }
        }
    }
}
