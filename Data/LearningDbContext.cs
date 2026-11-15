using EFCore03.Entities;
using Microsoft.EntityFrameworkCore;

namespace OnlineLearning.Data;

public class LearningDbContext : DbContext
{
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Server=.;Database=OnlineLearningDb;Trusted_Connection=True;TrustServerCertificate=True;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);

        // Step 2: Add this call for the second migration ("SeedInitialData")
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Instructor>().HasData(
            new Instructor { Id = 1, FullName = "Dr. Tarek Mansour", Email = "tarek@learn.io", Bio = "Backend Architect", JoinedDate = new DateTime(2023, 1, 10) },
            new Instructor { Id = 2, FullName = "Lina Adel", Email = "lina@learn.io", Bio = "Cloud & DevOps Engineer", JoinedDate = new DateTime(2023, 5, 15) }
        );

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Programming", Description = "Software Engineering", ParentCategoryId = null },
            new Category { Id = 2, Name = "Backend", Description = "Server-side technologies", ParentCategoryId = 1 },
            new Category { Id = 3, Name = "ASP.NET", Description = ".NET Web Development", ParentCategoryId = 2 },
            new Category { Id = 4, Name = "Design", Description = "UI/UX & Product Design", ParentCategoryId = null }
        );

        modelBuilder.Entity<Course>().HasData(
            new Course { Id = 1, Title = "EF Core Mastery", Description = "Deep dive into EF Core", Price = 49.99m, Level = CourseLevel.Intermediate, IsPublished = true, PublishedDate = new DateTime(2024, 2, 1), InstructorId = 1, CategoryId = 3 },
            new Course { Id = 2, Title = "C# Fundamentals", Description = "Start coding in C#", Price = 29.99m, Level = CourseLevel.Beginner, IsPublished = true, PublishedDate = new DateTime(2024, 1, 15), InstructorId = 1, CategoryId = 1 },
            new Course { Id = 3, Title = "Microservices Architecture", Description = "Distributed systems", Price = 79.99m, Level = CourseLevel.Advanced, IsPublished = false, PublishedDate = null, InstructorId = 2, CategoryId = 2 }
        );

        modelBuilder.Entity<Student>().HasData(
            new Student { Id = 1, FullName = "Ali Samir", Email = "ali@student.io", DateOfBirth = new DateTime(2001, 4, 12) },
            new Student { Id = 2, FullName = "Nourhan Youssef", Email = "nourhan@student.io", DateOfBirth = new DateTime(2000, 9, 20) }
        );

        modelBuilder.Entity<Enrollment>().HasData(
            new Enrollment { StudentId = 1, CourseId = 1, EnrollmentDate = new DateTime(2024, 3, 1), Progress = 40, Status = EnrollmentStatus.Active },
            new Enrollment { StudentId = 2, CourseId = 1, EnrollmentDate = new DateTime(2024, 3, 2), Progress = 100, Status = EnrollmentStatus.Completed, FinalGrade = 95.50m }
        );
    }
}