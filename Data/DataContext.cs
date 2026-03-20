using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Course_management.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Course_management.Data
{
    public class DataContext : IdentityDbContext<User>
    {
        public DataContext(DbContextOptions<DataContext> options): base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CourseProgress> CourseProgresses { get; set; }
        public DbSet<CourseTask> CourseTasks { get; set; }
        public DbSet<TaskSubmission> TaskSubmissions { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<CourseContent> CourseContents { get; set; }
        public DbSet<CourseSection> CourseSections { get; set; }
        
        // New DbSets for dashboard features
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<LearningStreak> LearningStreaks { get; set; }
        public DbSet<StudentTask> StudentTasks { get; set; }
        public DbSet<InterviewQuestion> InterviewQuestions { get; set; }
        public DbSet<DocumentationPage> DocumentationPages { get; set; }
        
        // Quiz DbSets
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizSubmission> QuizSubmissions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Course)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Course)
                .WithMany()
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for SQL Server
            builder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            // Configure Course.Price precision for SQL Server compatibility
            builder.Entity<Course>()
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            // Configure Identity tables for SQL Server compatibility
            builder.Entity<IdentityRole>()
                .Property(r => r.Id)
                .HasMaxLength(450);

            builder.Entity<User>()
                .Property(u => u.Id)
                .HasMaxLength(450);

            // Configure CourseProgress relationships
            builder.Entity<CourseProgress>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CourseProgresses)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CourseProgress>()
                .HasOne(cp => cp.Course)
                .WithMany(c => c.CourseProgresses)
                .HasForeignKey(cp => cp.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Quiz>()
                .HasOne(q => q.Course)
                .WithMany()
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuizSubmission>()
                .HasOne(qs => qs.Quiz)
                .WithMany(q => q.Submissions)
                .HasForeignKey(qs => qs.QuizId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QuizSubmission>()
                .HasOne(qs => qs.Student)
                .WithMany()
                .HasForeignKey(qs => qs.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure CourseTask relationships
            builder.Entity<CourseTask>()
                .HasOne(ct => ct.Course)
                .WithMany(c => c.CourseTasks)
                .HasForeignKey(ct => ct.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CourseTask>()
                .HasOne(ct => ct.CreatedByTutor)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(ct => ct.CreatedByTutorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TaskSubmission relationships
            builder.Entity<TaskSubmission>()
                .HasOne(ts => ts.Task)
                .WithMany(ct => ct.TaskSubmissions)
                .HasForeignKey(ts => ts.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskSubmission>()
                .HasOne(ts => ts.Student)
                .WithMany(u => u.TaskSubmissions)
                .HasForeignKey(ts => ts.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskSubmission>()
                .HasOne(ts => ts.GradedByTutor)
                .WithMany(u => u.GradedSubmissions)
                .HasForeignKey(ts => ts.GradedByTutorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure TaskAttachment relationships
            builder.Entity<TaskAttachment>()
                .HasOne(ta => ta.TaskSubmission)
                .WithMany(ts => ts.Attachments)
                .HasForeignKey(ta => ta.TaskSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskAttachment>()
                .HasOne(ta => ta.UploadedByUser)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(ta => ta.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for database compatibility
            builder.Entity<CourseProgress>()
                .Property(cp => cp.ProgressPercentage)
                .HasColumnType("decimal(5,2)");

            // Configure PasswordResetToken relationships
            builder.Entity<PasswordResetToken>()
                .HasOne(prt => prt.User)
                .WithMany()
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Certificate relationships
            builder.Entity<Certificate>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.StudentId)  // This was missing
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Certificate>()
                .HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

           

            // Configure LearningStreak relationships
            builder.Entity<LearningStreak>()
                .HasOne(ls => ls.Student)
                .WithOne(u => u.LearningStreak)
                .HasForeignKey<LearningStreak>(ls => ls.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure StudentTask relationships
            builder.Entity<StudentTask>()
                .HasOne(st => st.Student)
                .WithMany(u => u.StudentTasks)
                .HasForeignKey(st => st.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure enum conversions
            builder.Entity<StudentTask>()
                .Property(st => st.Priority)
                .HasConversion<int>();

            builder.Entity<StudentTask>()
                .Property(st => st.Category)
                .HasConversion<int>();

            // Configure CourseSection relationships
            builder.Entity<CourseSection>()
                .HasOne(s => s.Course)
                .WithMany(c => c.CourseSections)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CourseContent>()
                .HasOne(cc => cc.Section)
                .WithMany(s => s.Contents)
                .HasForeignKey(cc => cc.SectionId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        public static async Task SeedData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure roles
            var roles = new[] { "Admin", "Tutor", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed users
            if (!userManager.Users.Any())
            {
                var admin = new User { UserName = "admin@demo.com", Email = "admin@demo.com", FullName = "Admin User", Role = "Admin", EmailConfirmed = true };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");

                var tutor = new User { UserName = "tutor@demo.com", Email = "tutor@demo.com", FullName = "Tutor User", Role = "Tutor", EmailConfirmed = true };
                await userManager.CreateAsync(tutor, "Tutor123!");
                await userManager.AddToRoleAsync(tutor, "Tutor");

                var student = new User { UserName = "student@demo.com", Email = "student@demo.com", FullName = "Student User", Role = "Student", EmailConfirmed = true };
                await userManager.CreateAsync(student, "Student123!");
                await userManager.AddToRoleAsync(student, "Student");

                // Seed courses
                var course1 = new Course { Title = "C# Basics", Description = "Learn the basics of C#.", TutorId = tutor.Id, Price = 0, Status = CourseStatus.Approved };
                var course2 = new Course { Title = "ASP.NET Core", Description = "Build web APIs with ASP.NET Core.", TutorId = tutor.Id, Price = 0, Status = CourseStatus.Approved };
                var course3 = new Course { Title = "Entity Framework Core", Description = "Master data access with EF Core.", TutorId = tutor.Id, Price = 0, Status = CourseStatus.Approved };
                context.Courses.AddRange(course1, course2, course3);
                await context.SaveChangesAsync();

                // Seed enrollments
                var enrollment1 = new Enrollment { UserId = student.Id, CourseId = course1.Id };
                var enrollment2 = new Enrollment { UserId = student.Id, CourseId = course2.Id };
                var enrollment3 = new Enrollment { UserId = student.Id, CourseId = course3.Id };
                context.Enrollments.AddRange(enrollment1, enrollment2, enrollment3);
                await context.SaveChangesAsync();

                // Seed reviews
                var review1 = new Review { Content = "Great course!", Rating = 5, UserId = student.Id, CourseId = course1.Id };
                var review2 = new Review { Content = "Very helpful.", Rating = 4, UserId = student.Id, CourseId = course2.Id };
                var review3 = new Review { Content = "Clear explanations.", Rating = 5, UserId = student.Id, CourseId = course3.Id };
                context.Reviews.AddRange(review1, review2, review3);
                await context.SaveChangesAsync();
            }

            // Seed Interview Questions if they don't exist
            if (!context.InterviewQuestions.Any())
            {
                var questions = new[]
                {
                    new InterviewQuestion 
                    { 
                        Category = "React", 
                        Difficulty = "Easy", 
                        QuestionText = "What is the Virtual DOM?", 
                        AnswerText = "The Virtual DOM is a lightweight copy of the actual DOM. React uses it to improve performance by updating only the changed parts of the actual DOM.", 
                        CreatedAt = DateTime.UtcNow 
                    },
                    new InterviewQuestion 
                    { 
                        Category = "React", 
                        Difficulty = "Medium", 
                        QuestionText = "Explain the useEffect hook.", 
                        AnswerText = "useEffect is a hook that lets you perform side effects in function components. It serves the same purpose as componentDidMount, componentDidUpdate, and componentWillUnmount in React classes.", 
                        CreatedAt = DateTime.UtcNow 
                    },
                    new InterviewQuestion 
                    { 
                        Category = "C#", 
                        Difficulty = "Easy", 
                        QuestionText = "What is the difference between value types and reference types?", 
                        AnswerText = "Value types hold the data directly (e.g., int, struct), while reference types hold a reference to the data's memory address (e.g., class, string).", 
                        CreatedAt = DateTime.UtcNow 
                    },
                    new InterviewQuestion 
                    { 
                        Category = "C#", 
                        Difficulty = "Hard", 
                        QuestionText = "What is Dependency Injection?", 
                        AnswerText = "Dependency Injection is a design pattern used to implement IoC. It allows the creation of dependent objects outside of a class and provides those objects to a class through different ways.", 
                        CreatedAt = DateTime.UtcNow 
                    }
                };
                context.InterviewQuestions.AddRange(questions);
                await context.SaveChangesAsync();
            }

            // Seed Documentation Pages if they don't exist
            if (!context.DocumentationPages.Any())
            {
                var docs = new[]
                {
                    new DocumentationPage 
                    { 
                        Title = "Introduction", 
                        Slug = "introduction", 
                        Category = "Getting Started", 
                        Content = "# Introduction\n\nWelcome to the Techfrontio Learning Platform. This platform helps you master technical skills through courses, quizzes, and tasks.", 
                        Order = 1, 
                        UpdatedAt = DateTime.UtcNow 
                    },
                    new DocumentationPage 
                    { 
                        Title = "Installation", 
                        Slug = "installation", 
                        Category = "Getting Started", 
                        Content = "# Installation\n\nTo get started, ensure you have the following installed:\n- Node.js\n- .NET SDK\n- Docker", 
                        Order = 2, 
                        UpdatedAt = DateTime.UtcNow 
                    },
                    new DocumentationPage 
                    { 
                        Title = "Course Structure", 
                        Slug = "course-structure", 
                        Category = "Guides", 
                        Content = "# Course Structure\n\nCourses are divided into modules and lessons. Each lesson may contain video content, reading materials, and quizzes.", 
                        Order = 3, 
                        UpdatedAt = DateTime.UtcNow 
                    }
                };
                context.DocumentationPages.AddRange(docs);
                await context.SaveChangesAsync();
            }
        }
    }
}
