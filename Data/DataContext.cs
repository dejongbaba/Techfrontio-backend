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
                var course1 = new Course { Title = "C# Basics", Description = "Learn the basics of C#.", TutorId = tutor.Id };
                var course2 = new Course { Title = "ASP.NET Core", Description = "Build web APIs with ASP.NET Core.", TutorId = tutor.Id };
                context.Courses.AddRange(course1, course2);
                await context.SaveChangesAsync();

                // Seed enrollments
                var enrollment1 = new Enrollment { UserId = student.Id, CourseId = course1.Id };
                var enrollment2 = new Enrollment { UserId = student.Id, CourseId = course2.Id };
                context.Enrollments.AddRange(enrollment1, enrollment2);
                await context.SaveChangesAsync();

                // Seed reviews
                var review1 = new Review { Content = "Great course!", Rating = 5, UserId = student.Id, CourseId = course1.Id };
                var review2 = new Review { Content = "Very helpful.", Rating = 4, UserId = student.Id, CourseId = course2.Id };
                context.Reviews.AddRange(review1, review2);
                await context.SaveChangesAsync();
            }
        }
    }
}
