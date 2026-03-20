using System;
using System.ComponentModel.DataAnnotations;

namespace Course_management.Models
{
    public class DocumentationPage
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Slug { get; set; } // URL-friendly identifier

        [Required]
        public string Category { get; set; } // e.g., "Getting Started", "API Reference"

        [Required]
        public string Content { get; set; } // Markdown

        public int Order { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
