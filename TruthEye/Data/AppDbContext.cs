using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using TruthEye.Entities;
using TruthEye.Entities.Data;

namespace TruthEye.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        #region DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<SuperAdmin> SuperAdmins { get; set; }
        public DbSet<Proctor> Proctors { get; set; }
        public DbSet<Support> Supports { get; set; }
        public DbSet<University> Universities { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<MCQQuestion> MCQQuestions { get; set; }
        public DbSet<WrittenQuestion> WrittenQuestions { get; set; }
        public DbSet<ExamSession> ExamSessions { get; set; }
        public DbSet<AILog> AILogs { get; set; }
        public DbSet<AIConfig> AIConfigs { get; set; }
        public DbSet<ProctorSession> ProctorSessions { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<PerformanceReport> PerformanceReports { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<MCQOption> MCQOptions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<LoginLockout> LoginLockouts { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region User Configuration
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Student>("Student")
                .HasValue<Doctor>("Doctor")
                .HasValue<Admin>("Admin")
                .HasValue<SuperAdmin>("SuperAdmin")
                .HasValue<Proctor>("Proctor")
                .HasValue<Support>("Support");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.NationalId)
                .IsUnique();
            #endregion

            #region Question Configuration
            modelBuilder.Entity<Question>()
                .HasDiscriminator<string>("QuestionType")
                .HasValue<MCQQuestion>("MCQ")
                .HasValue<WrittenQuestion>("Written");
            #endregion

            #region Student Relationships
            modelBuilder.Entity<Student>()
                .HasMany(s => s.RegisteredCourses)
                .WithMany(c => c.RegisteredStudents)
                .UsingEntity(j => j.ToTable("StudentCourses"));

            modelBuilder.Entity<Student>()
                .HasMany(s => s.ExamSessions)
                .WithOne(es => es.Student)
                .HasForeignKey(es => es.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Student>()
                .HasMany(s => s.PerformanceReports)
                .WithOne(pr => pr.Student)
                .HasForeignKey(pr => pr.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            // Student University/Faculty/Department relationships
            modelBuilder.Entity<Student>()
                .HasOne(s => s.University)
                .WithMany()
                .HasForeignKey(s => s.UniversityId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Faculty)
                .WithMany()
                .HasForeignKey(s => s.FacultyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Department)
                .WithMany()
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Doctor Relationships
            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.CreatedExams)
                .WithOne(e => e.Doctor)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.TaughtCourses)
                .WithMany(c => c.Instructors)
                .UsingEntity(j => j.ToTable("DoctorCourses"));

            // Doctor University/Faculty/Department relationships
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.University)
                .WithMany()
                .HasForeignKey(d => d.UniversityId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Faculty)
                .WithMany()
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Admin Relationships
            modelBuilder.Entity<SuperAdmin>()
                .HasMany(a => a.ConfiguredAISettings)
                .WithOne()
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.University)
                .WithMany()
                .HasForeignKey(a => a.UniversityId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.Faculty)
                .WithMany()
                .HasForeignKey(a => a.FacultyId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Proctor Relationships
            modelBuilder.Entity<Proctor>()
                .HasMany(p => p.AssignedExams)
                .WithMany(e => e.Proctors)
                .UsingEntity(j => j.ToTable("ProctorExams"));

            modelBuilder.Entity<Proctor>()
                .HasMany(p => p.ProctorSessions)
                .WithOne(ps => ps.Proctor)
                .HasForeignKey(ps => ps.ProctorId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Support Relationships
            modelBuilder.Entity<Support>()
                .HasMany(s => s.SupportTickets)
                .WithOne(st => st.AssignedToSupport)
                .HasForeignKey(st => st.AssignedToSupportId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Faculty / Department / Course Hierarchy
            modelBuilder.Entity<Faculty>()
                .HasOne(f => f.University)
                .WithMany(u => u.Faculties)
                .HasForeignKey(f => f.UniversityId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Faculty)
                .WithMany(f => f.Departments)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Department)
                .WithMany(d => d.Courses)
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Exam / Question / Session / Answer
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.Property(e => e.PassingScore)
                    .HasDefaultValue(50);

                entity.Property(e => e.TotalMarks)
                    .HasDefaultValue(0);

                entity.Property(e => e.RandomizeQuestions)
                    .HasDefaultValue(false);

                entity.Property(e => e.RandomizeOptions)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Exams)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExamSession>()
                .HasOne(es => es.Exam)
                .WithMany(e => e.ExamSessions)
                .HasForeignKey(es => es.ExamId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProctorSession>()
                .HasOne(ps => ps.Exam)
                .WithMany()
                .HasForeignKey(ps => ps.ExamId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.ExamSession)
                .WithMany(es => es.Answers)
                .HasForeignKey(a => a.ExamSessionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MCQOption>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region SupportTicket Relationships 
            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.User)
                .WithMany()
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Notification Relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region AI Logs
            modelBuilder.Entity<AILog>()
                .HasOne(ai => ai.ExamSession)
                .WithMany(es => es.AILogs)
                .HasForeignKey(ai => ai.ExamSessionId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Chat Messages
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(cm => cm.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion

            #region Indexes
            modelBuilder.Entity<ExamSession>()
                .HasIndex(es => new { es.StudentId, es.ExamId });

            modelBuilder.Entity<AILog>()
                .HasIndex(ai => new { ai.ExamSessionId, ai.Timestamp });

            modelBuilder.Entity<SupportTicket>()
                .HasIndex(st => st.Status);

            modelBuilder.Entity<LoginLockout>()
                .HasIndex(l => l.Key)
                .IsUnique();
            #endregion

            #region Seed Data
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Student" },
                new Role { Id = 2, Name = "Doctor" },
                new Role { Id = 3, Name = "Admin" },
                new Role { Id = 4, Name = "Proctor" },
                new Role { Id = 5, Name = "Support" },
                new Role { Id = 6, Name = "SuperAdmin" }
            );
            #endregion
        }
    }
}