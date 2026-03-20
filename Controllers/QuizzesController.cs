using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Course_management.Data;
using Course_management.Dto;
using Course_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Course_management.Controllers
{
    [Route("api/quizzes")]
    [ApiController]
    public class QuizzesController : ControllerBase
    {
        private readonly DataContext _context;

        public QuizzesController(DataContext context)
        {
            _context = context;
        }

        // GET: api/quizzes/course/{courseId}
        [HttpGet("course/{courseId}")]
        [Authorize]
        public async Task<IActionResult> GetQuizzesForCourse(int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Check if user has access to course
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == userId);
            var isTutor = await _context.Courses.AnyAsync(c => c.Id == courseId && c.TutorId == userId);
            var isAdmin = userRole == "Admin";

            if (!isEnrolled && !isTutor && !isAdmin)
                return Forbid(ApiResponse.Error("You are not enrolled in this course", 403).ToString());

            var quizzes = await _context.Quizzes
                .Where(q => q.CourseId == courseId)
                .Include(q => q.Questions)
                .Include(q => q.Submissions.Where(s => s.StudentId == userId))
                .ToListAsync();

            var quizDtos = quizzes.Select(q => new QuizDto
            {
                Id = q.Id,
                Title = q.Title,
                Description = q.Description,
                CourseId = q.CourseId,
                PassingScore = q.PassingScore,
                IsCompleted = q.Submissions.Any(s => s.Passed),
                Score = q.Submissions.OrderByDescending(s => s.Score).FirstOrDefault()?.Score,
                Questions = q.Questions.Select(qq => new QuizQuestionDto
                {
                    Id = qq.Id,
                    QuestionText = qq.QuestionText,
                    Options = JsonSerializer.Deserialize<List<string>>(qq.OptionsJson) ?? new List<string>(),
                    // Hide correct answer unless completed or tutor
                    CorrectOptionIndex = (isTutor || isAdmin) ? qq.CorrectOptionIndex : (int?)null,
                    Explanation = (isTutor || isAdmin) ? qq.Explanation : null
                }).ToList()
            }).ToList();

            return Ok(ApiResponse<List<QuizDto>>.Success(quizDtos, "Quizzes retrieved successfully", 200));
        }

        // GET: api/quizzes/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetQuiz(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
                return NotFound(ApiResponse.Error("Quiz not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == quiz.CourseId && e.UserId == userId);
            var isTutor = await _context.Courses.AnyAsync(c => c.Id == quiz.CourseId && c.TutorId == userId);
            var isAdmin = userRole == "Admin";

            if (!isEnrolled && !isTutor && !isAdmin)
                return Forbid(ApiResponse.Error("You don't have permission to view this quiz", 403).ToString());

            // Check if student has passed
            var passed = await _context.QuizSubmissions.AnyAsync(s => s.QuizId == id && s.StudentId == userId && s.Passed);

            var quizDto = new QuizDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                CourseId = quiz.CourseId,
                PassingScore = quiz.PassingScore,
                IsCompleted = passed,
                Questions = quiz.Questions.Select(qq => new QuizQuestionDto
                {
                    Id = qq.Id,
                    QuestionText = qq.QuestionText,
                    Options = JsonSerializer.Deserialize<List<string>>(qq.OptionsJson) ?? new List<string>(),
                    CorrectOptionIndex = (isTutor || isAdmin || passed) ? qq.CorrectOptionIndex : (int?)null,
                    Explanation = (isTutor || isAdmin || passed) ? qq.Explanation : null
                }).ToList()
            };

            return Ok(ApiResponse<QuizDto>.Success(quizDto, "Quiz retrieved successfully", 200));
        }

        // POST: api/quizzes
        [HttpPost]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizDto createQuizDto)
        {
            var course = await _context.Courses.FindAsync(createQuizDto.CourseId);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (course.TutorId != userId && userRole != "Admin")
                return Forbid(ApiResponse.Error("You don't have permission to create quizzes for this course", 403).ToString());

            var quiz = new Quiz
            {
                Title = createQuizDto.Title,
                Description = createQuizDto.Description,
                CourseId = createQuizDto.CourseId,
                PassingScore = createQuizDto.PassingScore,
                Questions = createQuizDto.Questions.Select(q => new QuizQuestion
                {
                    QuestionText = q.QuestionText,
                    OptionsJson = JsonSerializer.Serialize(q.Options),
                    CorrectOptionIndex = q.CorrectOptionIndex,
                    Explanation = q.Explanation
                }).ToList()
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<Quiz>.Success(quiz, "Quiz created successfully", 201));
        }

        // POST: api/quizzes/{id}/submit
        [HttpPost("{id}/submit")]
        [Authorize]
        public async Task<IActionResult> SubmitQuiz(int id, [FromBody] SubmitQuizDto submission)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
                return NotFound(ApiResponse.Error("Quiz not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Validate enrollment
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == quiz.CourseId && e.UserId == userId);
            if (!isEnrolled)
                return Forbid(ApiResponse.Error("You are not enrolled in this course", 403).ToString());

            int correctCount = 0;
            var correctAnswers = new Dictionary<int, int>();
            var explanations = new Dictionary<int, string>();

            foreach (var question in quiz.Questions)
            {
                correctAnswers[question.Id] = question.CorrectOptionIndex;
                explanations[question.Id] = question.Explanation;

                if (submission.Answers.TryGetValue(question.Id, out int selectedOption))
                {
                    if (selectedOption == question.CorrectOptionIndex)
                    {
                        correctCount++;
                    }
                }
            }

            int score = (int)((double)correctCount / quiz.Questions.Count * 100);
            bool passed = score >= quiz.PassingScore;

            var quizSubmission = new QuizSubmission
            {
                QuizId = id,
                StudentId = userId,
                Score = score,
                Passed = passed,
                AnswersJson = JsonSerializer.Serialize(submission.Answers)
            };

            _context.QuizSubmissions.Add(quizSubmission);
            await _context.SaveChangesAsync();

            var result = new QuizResultDto
            {
                Score = score,
                Passed = passed,
                PassingScore = quiz.PassingScore,
                CorrectAnswers = correctAnswers,
                Explanations = explanations
            };

            return Ok(ApiResponse<QuizResultDto>.Success(result, "Quiz submitted successfully", 200));
        }
        
        // DELETE: api/quizzes/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
                return NotFound(ApiResponse.Error("Quiz not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            
            // Check ownership
            var course = await _context.Courses.FindAsync(quiz.CourseId);
            if (course.TutorId != userId && userRole != "Admin")
                return Forbid(ApiResponse.Error("You don't have permission to delete this quiz", 403).ToString());

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Quiz deleted successfully", 200));
        }
    }
}
