using Microsoft.AspNetCore.Mvc;
using Course_management.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using Course_management.Dto;

namespace Course_management.Controllers
{
    [Route("api/documentation")]
    [ApiController]
    public class DocumentationController : ControllerBase
    {
        private readonly string _docsPath;

        public DocumentationController()
        {
            // Point to the frontend's content directory relative to the backend project
            _docsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "techfrontio-frontend", "src", "content", "docs");
            
            if (!Directory.Exists(_docsPath))
            {
                Directory.CreateDirectory(_docsPath);
            }
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            try 
            {
                var files = Directory.GetFiles(_docsPath, "*.md");
                var pages = new List<DocumentationPage>();

                var i = 1;
                foreach (var file in files)
                {
                    var content = System.IO.File.ReadAllText(file);
                    var page = ParseMarkdownFile(content, Path.GetFileNameWithoutExtension(file));
                    page.Id = i++;
                    pages.Add(page);
                }

                return Ok(ApiResponse<List<DocumentationPage>>.Success(pages.OrderBy(p => p.Order).ToList(), "Documentation pages retrieved", 200));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Error($"Failed to retrieve docs: {ex.Message}", 400));
            }
        }

        [HttpGet("{slug}")]
        public IActionResult GetBySlug(string slug)
        {
            var filePath = Path.Combine(_docsPath, $"{slug}.md");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(ApiResponse.Error("Documentation page not found", 404));
            }

            var content = System.IO.File.ReadAllText(filePath);
            var page = ParseMarkdownFile(content, slug);
            page.Id = 1;
            return Ok(ApiResponse<DocumentationPage>.Success(page, "Documentation page retrieved", 200));
        }

        [HttpPost]
        public IActionResult Create([FromBody] DocumentationPage page)
        {
            try 
            {
                if (string.IsNullOrEmpty(page.Slug)) 
                    page.Slug = Regex.Replace(page.Title.ToLower(), @"[^a-z0-9]+", "-").Trim('-');

                var filePath = Path.Combine(_docsPath, $"{page.Slug}.md");
                var content = GenerateMarkdownWithFrontmatter(page);
                System.IO.File.WriteAllText(filePath, content);
                return Ok(ApiResponse<DocumentationPage>.Success(page, "Documentation created and stored on frontend source", 201));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Error($"Failed to create doc: {ex.Message}", 400));
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] DocumentationPage page)
        {
            try 
            {
                var filePath = Path.Combine(_docsPath, $"{page.Slug}.md");
                var content = GenerateMarkdownWithFrontmatter(page);
                System.IO.File.WriteAllText(filePath, content);
                return Ok(ApiResponse<DocumentationPage>.Success(page, "Documentation updated", 200));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Error($"Failed to update doc: {ex.Message}", 400));
            }
        }

        [HttpDelete("{slug}")]
        public IActionResult Delete(string slug)
        {
            try 
            {
                var filePath = Path.Combine(_docsPath, $"{slug}.md");
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return Ok(ApiResponse<object>.Success(null, "Documentation deleted", 200));
                }
                return NotFound(ApiResponse.Error("Not found", 404));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Error($"Failed to delete doc: {ex.Message}", 400));
            }
        }

        private DocumentationPage ParseMarkdownFile(string rawContent, string slug)
        {
            var page = new DocumentationPage { Slug = slug, Content = rawContent, Title = slug, Category = "General", Order = 0 };
            var match = Regex.Match(rawContent, @"^---\r?\n([\s\S]+?)\r?\n---");
            
            if (match.Success)
            {
                var yaml = match.Groups[1].Value;
                page.Content = rawContent.Substring(match.Length).Trim();
                
                var lines = yaml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 2)
                    {
                        var key = parts[0].Trim().ToLower();
                        var value = string.Join(":", parts.Skip(1)).Trim();
                        
                        switch (key)
                        {
                            case "title": page.Title = value; break;
                            case "category": page.Category = value; break;
                            case "order": int.TryParse(value, out int order); page.Order = order; break;
                            case "updatedat": DateTime.TryParse(value, out DateTime dt); page.UpdatedAt = dt; break;
                        }
                    }
                }
            }
            return page;
        }

        private string GenerateMarkdownWithFrontmatter(DocumentationPage page)
        {
            return $@"---
title: {page.Title}
slug: {page.Slug}
category: {page.Category}
order: {page.Order}
updatedAt: {DateTime.UtcNow:yyyy-MM-dd}
---

{page.Content}";
        }
    }
}
