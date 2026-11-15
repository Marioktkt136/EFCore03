using EFCore03.Entities;
using Microsoft.EntityFrameworkCore;
using OnlineLearning.Data;

namespace OnlineLearning;

class Program
{
    static void Main()
    {
        using var context = new LearningDbContext();

        Console.WriteLine("=== CREATE OPERATIONS ===");
        int newInstructorId = AddInstructor(context, "Khaled Nabil", "khaled@learn.io", "Database Specialist");
        int newCourseId = AddCourse(context, "SQL Performance Tuning", 59.99m, CourseLevel.Intermediate, newInstructorId, categoryId: 3);
        var newStudentIds = AddStudents(context);

        if (newStudentIds.Count > 0)
        {
            // Attempt enrolling into an unpublished course (should refuse)
            EnrollStudent(context, newStudentIds[0], newCourseId);
            // Enroll into seeded published course Id = 1
            EnrollStudent(context, newStudentIds[0], courseId: 1);
            EnrollStudent(context, newStudentIds[1], courseId: 1);
            // Attempt duplicate enrollment (should refuse)
            EnrollStudent(context, newStudentIds[0], courseId: 1);
        }

        Console.WriteLine("\n=== READ OPERATIONS ===");
        FindCourse(context, id: 1, title: "C# Fundamentals");
        FindCourse(context, id: 999, title: "Non Existent Course");
        FilterCourses(context, minPrice: 25m, maxPrice: 65m);
        PrintSubCategories(context, parentId: 1);
        PrintCourseRoster(context, courseId: 1);

        Console.WriteLine("\n=== UPDATE OPERATIONS ===");
        ChangeCoursePrice(context, courseId: 2, newPrice: 34.99m);
        UpdateProgress(context, studentId: 1, courseId: 1, newProgress: 150); // invalid
        UpdateProgress(context, studentId: 1, courseId: 1, newProgress: 100); // completes
        PublishCourse(context, courseId: newCourseId);

        Console.WriteLine("\n=== DELETE OPERATIONS ===");
        if (newStudentIds.Count > 0)
            UnenrollStudent(context, newStudentIds[0], courseId: 1);

        DeleteCategoryWithExceptionDemo(context, categoryId: 1); // has sub-categories -> fails
        DeleteCategorySafely(context, categoryId: 1);            // checks first -> friendly message

        DeleteCourse(context, courseId: 1); // has enrollments -> cascades
    }

    // ---------------- CREATE ----------------

    static int AddInstructor(LearningDbContext ctx, string fullName, string email, string? bio)
    {
        var instructor = new Instructor
        {
            FullName = fullName,
            Email = email,
            Bio = bio,
            JoinedDate = DateTime.UtcNow
        };
        ctx.Instructors.Add(instructor);
        ctx.SaveChanges();
        Console.WriteLine($"Added Instructor '{instructor.FullName}' with Id = {instructor.Id}");
        return instructor.Id;
    }

    static int AddCourse(LearningDbContext ctx, string title, decimal price, CourseLevel level, int instructorId, int categoryId)
    {
        bool instructorExists = ctx.Instructors.Any(i => i.Id == instructorId);
        bool categoryExists = ctx.Categories.Any(c => c.Id == categoryId);

        if (!instructorExists || !categoryExists)
        {
            Console.WriteLine($"Cannot add course: Instructor({instructorId}) exists={instructorExists}, Category({categoryId}) exists={categoryExists}.");
            return 0;
        }

        var course = new Course
        {
            Title = title,
            Price = price,
            Level = level,
            IsPublished = false,
            PublishedDate = null,
            InstructorId = instructorId,
            CategoryId = categoryId
        };
        ctx.Courses.Add(course);
        ctx.SaveChanges();
        Console.WriteLine($"Added Course '{course.Title}' with Id = {course.Id}");
        return course.Id;
    }

    static List<int> AddStudents(LearningDbContext ctx)
    {
        var students = new List<Student>
        {
            new() { FullName = "Mina George", Email = $"mina.{Guid.NewGuid():N}@s.io", DateOfBirth = new DateTime(2002, 1, 5) },
            new() { FullName = "Salma Hany", Email = $"salma.{Guid.NewGuid():N}@s.io", DateOfBirth = new DateTime(2001, 7, 19) },
            new() { FullName = "Youssef Karim", Email = $"youssef.{Guid.NewGuid():N}@s.io", DateOfBirth = new DateTime(2003, 11, 3) }
        };

        ctx.Students.AddRange(students);
        ctx.SaveChanges();

        var ids = students.Select(s => s.Id).ToList();
        Console.WriteLine($"Added 3 students in bulk. Generated Ids: {string.Join(", ", ids)}");
        return ids;
    }

    static void EnrollStudent(LearningDbContext ctx, int studentId, int courseId)
    {
        var student = ctx.Students.Find(studentId);
        if (student is null)
        {
            Console.WriteLine($"Enrollment refused: Student {studentId} does not exist.");
            return;
        }

        var course = ctx.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"Enrollment refused: Course {courseId} does not exist.");
            return;
        }

        if (!course.IsPublished)
        {
            Console.WriteLine($"Enrollment refused: Course '{course.Title}' is not published yet.");
            return;
        }

        bool alreadyEnrolled = ctx.Enrollments.Any(e => e.StudentId == studentId && e.CourseId == courseId);
        if (alreadyEnrolled)
        {
            Console.WriteLine($"Enrollment refused: Student {studentId} is already enrolled in Course {courseId}.");
            return;
        }

        ctx.Enrollments.Add(new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            EnrollmentDate = DateTime.UtcNow,
            Progress = 0,
            Status = EnrollmentStatus.Active
        });
        ctx.SaveChanges();
        Console.WriteLine($"Enrolled Student {studentId} in Course {courseId}.");
    }

    // ---------------- READ ----------------

    static void FindCourse(LearningDbContext ctx, int id, string title)
    {
        var byId = ctx.Courses.Find(id);
        Console.WriteLine(byId is null ? $"Find({id}): not found" : $"Find({id}): {byId.Title} (${byId.Price})");

        var byTitle = ctx.Courses.FirstOrDefault(c => c.Title == title);
        Console.WriteLine(byTitle is null ? $"FirstOrDefault('{title}'): not found" : $"FirstOrDefault('{title}'): Id={byTitle.Id}");
    }

    static void FilterCourses(LearningDbContext ctx, decimal minPrice, decimal maxPrice)
    {
        var courses = ctx.Courses
            .Where(c => c.Price >= minPrice && c.Price <= maxPrice)
            .OrderByDescending(c => c.Price)
            .Select(c => new { c.Title, c.Level, c.Price })
            .ToList();

        Console.WriteLine($"Courses between {minPrice:C} and {maxPrice:C}:");
        foreach (var c in courses)
            Console.WriteLine($"  - {c.Title} | {c.Level} | {c.Price:C}");
    }

    static void PrintSubCategories(LearningDbContext ctx, int parentId)
    {
        var subs = ctx.Categories
            .Where(c => c.ParentCategoryId == parentId)
            .ToList();

        Console.WriteLine($"Direct sub-categories of Category {parentId}:");
        foreach (var s in subs)
            Console.WriteLine($"  - [{s.Id}] {s.Name}");

        var topLevel = ctx.Categories
            .Where(c => c.ParentCategoryId == null)
            .ToList();

        Console.WriteLine("Top-level categories:");
        foreach (var t in topLevel)
            Console.WriteLine($"  - [{t.Id}] {t.Name}");
    }

    static void PrintCourseRoster(LearningDbContext ctx, int courseId)
    {
        var studentIds = ctx.Enrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => e.StudentId)
            .ToList();

        Console.WriteLine($"Course {courseId} has {studentIds.Count} enrolled student(s):");

        if (studentIds.Count == 0) return;

        var studentNames = ctx.Students
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => s.FullName)
            .ToList();

        foreach (var name in studentNames)
            Console.WriteLine($"  - {name}");
    }

    // ---------------- UPDATE ----------------

    static void ChangeCoursePrice(LearningDbContext ctx, int courseId, decimal newPrice)
    {
        var course = ctx.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"Course {courseId} not found.");
            return;
        }
        course.Price = newPrice;
        ctx.SaveChanges();
        Console.WriteLine($"Updated '{course.Title}' price to {course.Price:C}.");
    }

    static void UpdateProgress(LearningDbContext ctx, int studentId, int courseId, int newProgress)
    {
        if (newProgress < 0 || newProgress > 100)
        {
            Console.WriteLine($"Invalid progress ({newProgress}). Value must be between 0 and 100.");
            return;
        }

        var enrollment = ctx.Enrollments.Find(studentId, courseId);
        if (enrollment is null)
        {
            Console.WriteLine($"Enrollment ({studentId}, {courseId}) not found.");
            return;
        }

        enrollment.Progress = newProgress;
        if (newProgress == 100)
            enrollment.Status = EnrollmentStatus.Completed;

        ctx.SaveChanges();
        Console.WriteLine($"Updated progress for ({studentId}, {courseId}) to {enrollment.Progress}% (Status: {enrollment.Status}).");
    }

    static void PublishCourse(LearningDbContext ctx, int courseId)
    {
        var course = ctx.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"Course {courseId} not found.");
            return;
        }

        course.IsPublished = true;
        course.PublishedDate = DateTime.UtcNow;
        ctx.SaveChanges();
        Console.WriteLine($"Published '{course.Title}' at {course.PublishedDate:u}.");
    }

    // ---------------- DELETE ----------------

    static void UnenrollStudent(LearningDbContext ctx, int studentId, int courseId)
    {
        var enrollment = ctx.Enrollments.Find(studentId, courseId);
        if (enrollment is null)
        {
            Console.WriteLine($"Enrollment ({studentId}, {courseId}) not found.");
            return;
        }

        ctx.Enrollments.Remove(enrollment);
        ctx.SaveChanges();
        Console.WriteLine($"Unenrolled Student {studentId} from Course {courseId}.");
    }

    static void DeleteCategoryWithExceptionDemo(LearningDbContext ctx, int categoryId)
    {
        var category = ctx.Categories.Find(categoryId);
        if (category is null)
        {
            Console.WriteLine($"Category {categoryId} not found.");
            return;
        }

        try
        {
            ctx.Categories.Remove(category);
            ctx.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Caught DbUpdateException: {ex.InnerException?.Message ?? ex.Message}");
            // Reset entity state so subsequent SaveChanges() calls on this context do not retry the failed delete
            ctx.Entry(category).State = EntityState.Unchanged;
        }
    }

    static void DeleteCategorySafely(LearningDbContext ctx, int categoryId)
    {
        var category = ctx.Categories.Find(categoryId);
        if (category is null)
        {
            Console.WriteLine($"Category {categoryId} not found.");
            return;
        }

        bool hasSubCategories = ctx.Categories.Any(c => c.ParentCategoryId == categoryId);
        if (hasSubCategories)
        {
            Console.WriteLine($"Cannot delete category '{category.Name}' because it still has sub-categories.");
            return;
        }

        ctx.Categories.Remove(category);
        ctx.SaveChanges();
        Console.WriteLine($"Deleted category '{category.Name}'.");
    }

    static void DeleteCourse(LearningDbContext ctx, int courseId)
    {
        var course = ctx.Courses.Find(courseId);
        if (course is null)
        {
            Console.WriteLine($"Course {courseId} not found.");
            return;
        }

        int beforeCount = ctx.Enrollments.Count(e => e.CourseId == courseId);
        Console.WriteLine($"Enrollments for Course {courseId} BEFORE delete: {beforeCount}");

        ctx.Courses.Remove(course);
        ctx.SaveChanges();

        int afterCount = ctx.Enrollments.Count(e => e.CourseId == courseId);
        Console.WriteLine($"Enrollments for Course {courseId} AFTER delete: {afterCount}");

        // Explanation:
        // Enrollment has a required foreign key (CourseId) to Course configured with
        // DeleteBehavior.Cascade. When the parent Course row is deleted in SQL Server,
        // the database automatically cascades the deletion and removes all child
        // Enrollment rows referencing that CourseId.
    }
}