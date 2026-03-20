using System.Collections.Generic;

namespace Course_management.Dto
{
    public class QuizDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CourseId { get; set; }
        public int PassingScore { get; set; }
        public List<QuizQuestionDto> Questions { get; set; }
        public bool IsCompleted { get; set; }
        public int? Score { get; set; }
    }

    public class QuizQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        // We don't send CorrectOptionIndex to student unless it's a review
        public int? CorrectOptionIndex { get; set; }
        public string Explanation { get; set; }
    }

    public class CreateQuizDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int CourseId { get; set; }
        public int PassingScore { get; set; }
        public List<CreateQuizQuestionDto> Questions { get; set; }
    }

    public class CreateQuizQuestionDto
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public int CorrectOptionIndex { get; set; }
        public string Explanation { get; set; }
    }

    public class SubmitQuizDto
    {
        public Dictionary<int, int> Answers { get; set; } // QuestionId -> OptionIndex
    }

    public class QuizResultDto
    {
        public int Score { get; set; }
        public bool Passed { get; set; }
        public int PassingScore { get; set; }
        public Dictionary<int, int> CorrectAnswers { get; set; } // QuestionId -> CorrectOptionIndex
        public Dictionary<int, string> Explanations { get; set; } // QuestionId -> Explanation
    }
}
