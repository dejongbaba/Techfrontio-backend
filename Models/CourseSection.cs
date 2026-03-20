using System.ComponentModel.DataAnnotations;

namespace Course_management.Models
{
    public class CourseSection
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public int Order { get; set; }
        
        public int CourseId { get; set; }
        public Course Course { get; set; }
        
        public ICollection<CourseContent> Contents { get; set; } = new List<CourseContent>();
    }
}
