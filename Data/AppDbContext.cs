using System;
using System.Collections.Generic;
using EduBridge.Models;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Center> Centers { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Homework> Homeworks { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwAttendanceSummary> VwAttendanceSummaries { get; set; }

    public virtual DbSet<VwClassOverview> VwClassOverviews { get; set; }

    public virtual DbSet<VwStudentOverview> VwStudentOverviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261C45619F5E");

            entity.ToTable("Attendance");

            entity.HasIndex(e => e.LessonId, "IX_Attendance_LessonId");

            entity.HasIndex(e => e.StudentId, "IX_Attendance_StudentId");

            entity.HasIndex(e => new { e.LessonId, e.StudentId }, "UQ_Attendance").IsUnique();

            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Lesson).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Lessons");

            entity.HasOne(d => d.Student).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Students");
        });

        modelBuilder.Entity<Center>(entity =>
        {
            entity.HasKey(e => e.CenterId).HasName("PK__Centers__398FC7F73E32AD2B");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CenterName).HasMaxLength(150);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.Centers)
                .HasForeignKey(d => d.OwnerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Centers_Users");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C0260CB969");

            entity.HasIndex(e => e.CenterId, "IX_Classes_CenterId");

            entity.HasIndex(e => e.ClassCode, "IX_Classes_ClassCode");

            entity.HasIndex(e => e.CourseId, "IX_Classes_CourseId");

            entity.HasIndex(e => e.TeacherId, "IX_Classes_TeacherId");

            entity.HasIndex(e => e.ClassCode, "UQ__Classes__2ECD4A557BAAF4A4").IsUnique();

            entity.Property(e => e.ClassCode).HasMaxLength(30);
            entity.Property(e => e.ClassName).HasMaxLength(150);
            entity.Property(e => e.MaxStudents).HasDefaultValue(20);
            entity.Property(e => e.Room).HasMaxLength(50);
            entity.Property(e => e.ScheduleText).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Center).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Centers");

            entity.HasOne(d => d.Course).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Courses");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Teachers");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Courses__C92D71A7312A9D7F");

            entity.Property(e => e.CourseName).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DurationWeeks).HasDefaultValue(12);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.TuitionFee).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Center).WithMany(p => p.Courses)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Courses_Centers");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Enrollme__7F68771BF29B79D2");

            entity.HasIndex(e => e.ClassId, "IX_Enrollments_ClassId");

            entity.HasIndex(e => e.StudentId, "IX_Enrollments_StudentId");

            entity.HasIndex(e => new { e.StudentId, e.ClassId }, "UQ_Enrollments").IsUnique();

            entity.Property(e => e.EnrollDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Đang học");

            entity.HasOne(d => d.Class).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Classes");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Students");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradeId).HasName("PK__Grades__54F87A57F4090168");

            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.GradeName).HasMaxLength(100);
            entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Class).WithMany(p => p.Grades)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grades_Classes");

            entity.HasOne(d => d.Student).WithMany(p => p.Grades)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grades_Students");
        });

        modelBuilder.Entity<Homework>(entity =>
        {
            entity.HasKey(e => e.HomeworkId).HasName("PK__Homework__FDE46A72E1E474B0");

            entity.ToTable("Homework");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Lesson).WithMany(p => p.Homeworks)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Homework_Lessons");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonId).HasName("PK__Lessons__B084ACD01DD907D6");

            entity.HasIndex(e => new { e.ClassId, e.LessonDate }, "IX_Lessons_ClassId_LessonDate");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.LessonTitle).HasMaxLength(200);

            entity.HasOne(d => d.Class).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lessons_Classes");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Messages__C87C0C9C37F031CA");

            entity.HasIndex(e => new { e.ReceiverUserId, e.IsRead }, "IX_Messages_Receiver_IsRead");

            entity.HasIndex(e => new { e.SenderUserId, e.ReceiverUserId }, "IX_Messages_SenderReceiver");

            entity.Property(e => e.SentAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.ReceiverUser).WithMany(p => p.MessageReceiverUsers)
                .HasForeignKey(d => d.ReceiverUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Receiver");

            entity.HasOne(d => d.SenderUser).WithMany(p => p.MessageSenderUsers)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Sender");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12786C6BE4");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_Notifications_User_Read");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A388E3B2F");

            entity.HasIndex(e => e.RoleCode, "UQ__Roles__D62CB59CC022D552").IsUnique();

            entity.Property(e => e.RoleCode).HasMaxLength(20);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52B990550F83F");

            entity.HasIndex(e => e.CenterId, "IX_Students_CenterId");

            entity.HasIndex(e => e.ParentUserId, "IX_Students_ParentUserId");

            entity.HasIndex(e => e.StudentCode, "IX_Students_StudentCode");

            entity.HasIndex(e => e.StudentCode, "UQ__Students__1FC8860421D6BBDD").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.StudentCode).HasMaxLength(30);

            entity.HasOne(d => d.Center).WithMany(p => p.Students)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Students_Centers");

            entity.HasOne(d => d.ParentUser).WithMany(p => p.Students)
                .HasForeignKey(d => d.ParentUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Students_Users");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherId).HasName("PK__Teachers__EDF2596449654B6C");

            entity.HasIndex(e => e.CenterId, "IX_Teachers_CenterId");

            entity.HasIndex(e => e.UserId, "IX_Teachers_UserId");

            entity.HasIndex(e => e.UserId, "UQ__Teachers__1788CC4DD5305503").IsUnique();

            entity.Property(e => e.Specialization).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Center).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teachers_Centers");

            entity.HasOne(d => d.User).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teachers_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CB36B485D");

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => new { e.RoleId, e.Status }, "IX_Users_RoleId_Status");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105346CC3F558").IsUnique();

            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.EmailConfirmed).HasDefaultValue(true);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<VwAttendanceSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AttendanceSummary");

            entity.Property(e => e.ClassName).HasMaxLength(150);
            entity.Property(e => e.LessonTitle).HasMaxLength(200);
        });

        modelBuilder.Entity<VwClassOverview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ClassOverview");

            entity.Property(e => e.CenterName).HasMaxLength(150);
            entity.Property(e => e.ClassCode).HasMaxLength(30);
            entity.Property(e => e.ClassName).HasMaxLength(150);
            entity.Property(e => e.CourseName).HasMaxLength(150);
            entity.Property(e => e.Room).HasMaxLength(50);
            entity.Property(e => e.ScheduleText).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.TeacherName).HasMaxLength(100);
        });

        modelBuilder.Entity<VwStudentOverview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StudentOverview");

            entity.Property(e => e.CenterName).HasMaxLength(150);
            entity.Property(e => e.ParentName).HasMaxLength(100);
            entity.Property(e => e.ParentPhone).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.StudentCode).HasMaxLength(30);
            entity.Property(e => e.StudentName).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
