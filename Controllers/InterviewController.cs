using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Course_management.Data;
using Course_management.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Course_management.Dto; // Assuming Dtos are here or I might need to create them if not using Models directly
using Microsoft.AspNetCore.Authorization;

namespace Course_management.Controllers
{
    [Route("api/interview")]
    [ApiController]
    public class InterviewController : ControllerBase
    {
        private readonly DataContext _context;

        public InterviewController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.InterviewQuestions
                .Select(q => q.Category)
                .Distinct()
                .ToListAsync();
            return Ok(ApiResponse<List<string>>.Success(categories, "Categories retrieved successfully", 200));
        }

        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions([FromQuery] string? category, [FromQuery] string? difficulty)
        {
            var query = _context.InterviewQuestions.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(q => q.Category == category);
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                query = query.Where(q => q.Difficulty == difficulty);
            }

            var questions = await query.ToListAsync();
            return Ok(ApiResponse<List<InterviewQuestion>>.Success(questions, "Questions retrieved successfully", 200));
        }

        [HttpGet("questions/{id}")]
        public async Task<IActionResult> GetQuestion(int id)
        {
            var question = await _context.InterviewQuestions.FindAsync(id);

            if (question == null)
            {
                return NotFound(ApiResponse.Error("Question not found", 404));
            }

            return Ok(ApiResponse<InterviewQuestion>.Success(question, "Question retrieved successfully", 200));
        }
    }
}
