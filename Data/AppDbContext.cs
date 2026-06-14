using System;
using System.Collections.Generic;
using EduBridge.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Data;

public partial class AppDbContext : DbContext, IDataProtectionKeyContext
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

    public virtual DbSet<CenterUser> CenterUsers { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public virtual DbSet<ClassCodeCounter> ClassCodeCounters { get; set; }

    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<EnrollmentHistory> EnrollmentHistories { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Homework> Homeworks { get; set; }

    public virtual DbSet<HomeworkSubmission> HomeworkSubmissions { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceCodeCounter> InvoiceCodeCounters { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudyShift> StudyShifts { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherCodeCounter> TeacherCodeCounters { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwAttendanceSummary> VwAttendanceSummaries { get; set; }

    public virtual DbSet<VwClassOverview> VwClassOverviews { get; set; }

    public virtual DbSet<VwRevenueByPayment> VwRevenueByPayments { get; set; }

    public virtual DbSet<VwRoomOverview> VwRoomOverviews { get; set; }

    public virtual DbSet<VwStudentOverview> VwStudentOverviews { get; set; }

    public virtual DbSet<VwStudyShiftOverview> VwStudyShiftOverviews { get; set; }

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

        modelBuilder.Entity<CenterUser>(entity =>
        {
            entity.HasIndex(e => new { e.CenterId, e.UserType, e.Status }, "IX_CenterUsers_Center_UserType_Status");

            entity.HasIndex(e => new { e.UserId, e.UserType }, "IX_CenterUsers_UserId_UserType");

            entity.HasIndex(e => new { e.UserId, e.UserType, e.Status }, "IX_CenterUsers_UserId_UserType_Status");

            entity.HasIndex(e => new { e.CenterId, e.UserId, e.UserType }, "UX_CenterUsers_Center_User_Type").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.UserType).HasMaxLength(20);

            entity.HasOne(d => d.Center).WithMany(p => p.CenterUsers)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CenterUsers_Centers");

            entity.HasOne(d => d.User).WithMany(p => p.CenterUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CenterUsers_Users");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C0260CB969");

            entity.HasIndex(e => e.CenterId, "IX_Classes_CenterId");

            entity.HasIndex(e => new { e.CenterId, e.ClassCode }, "IX_Classes_CenterId_ClassCode");

            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.Status, e.ClassId }, "IX_Classes_Center_IsDeleted_Status_ClassId").IsDescending(false, false, false, true);

            entity.HasIndex(e => new { e.CenterId, e.RoomId, e.IsDeleted, e.Status, e.ClassId }, "IX_Classes_Center_Room_Active");

            entity.HasIndex(e => new { e.CenterId, e.RoomId, e.Status, e.ClassId }, "IX_Classes_Center_Room_Status");

            entity.HasIndex(e => new { e.CenterId, e.TeacherId, e.IsDeleted, e.Status, e.ClassId }, "IX_Classes_Center_Teacher_Active");

            entity.HasIndex(e => new { e.CenterId, e.TeacherId, e.Status, e.ClassId }, "IX_Classes_Center_Teacher_Status");

            entity.HasIndex(e => e.ClassCode, "IX_Classes_ClassCode");

            entity.HasIndex(e => e.CourseId, "IX_Classes_CourseId");

            entity.HasIndex(e => e.DeletedByUserId, "IX_Classes_DeletedByUserId").HasFilter("([DeletedByUserId] IS NOT NULL)");

            entity.HasIndex(e => e.RoomId, "IX_Classes_RoomId");

            entity.HasIndex(e => e.TeacherId, "IX_Classes_TeacherId");

            entity.HasIndex(e => e.ClassCode, "UQ_Classes_ClassCode").IsUnique();

            entity.Property(e => e.ClassCode).HasMaxLength(30);
            entity.Property(e => e.ClassName).HasMaxLength(150);
            entity.Property(e => e.ClosedAt).HasPrecision(0);
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.Room).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.ScheduleText).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.TuitionFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Center).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Centers");

            entity.HasOne(d => d.ClosedByUser).WithMany(p => p.ClassClosedByUsers)
                .HasForeignKey(d => d.ClosedByUserId)
                .HasConstraintName("FK_Classes_ClosedByUser");

            entity.HasOne(d => d.Course).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Courses");

            entity.HasOne(d => d.DeletedByUser).WithMany(p => p.ClassDeletedByUsers)
                .HasForeignKey(d => d.DeletedByUserId)
                .HasConstraintName("FK_Classes_DeletedByUser");

            entity.HasOne(d => d.RoomNavigation).WithMany(p => p.Classes)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Rooms");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Teachers");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.ClassUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK_Classes_UpdatedByUser");
        });

        modelBuilder.Entity<ClassCodeCounter>(entity =>
        {
            entity.HasKey(e => new { e.CenterId, e.YearMonth });

            entity.Property(e => e.YearMonth)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Center).WithMany(p => p.ClassCodeCounters)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassCodeCounters_Centers");
        });

        modelBuilder.Entity<ClassSchedule>(entity =>
        {
            entity.HasKey(e => e.ClassScheduleId).HasName("PK__ClassSch__6A8D56FE6DDE5B24");

            entity.HasIndex(e => e.ClassId, "IX_ClassSchedules_ClassId");

            entity.HasIndex(e => new { e.ClassId, e.DayOfWeek, e.StartTime, e.EndTime }, "UQ_ClassSchedules_Class_Day_Time").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.StartTime).HasPrecision(0);

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSchedules)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassSchedules_Classes");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Courses__C92D71A7312A9D7F");

            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.CourseCode }, "IX_Courses_CenterId_IsDeleted_CourseCode");

            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.Status, e.CourseId }, "IX_Courses_CenterId_IsDeleted_Status_CourseId").IsDescending(false, false, false, true);

            entity.HasIndex(e => new { e.CenterId, e.CourseCode }, "UX_Courses_CenterId_CourseCode_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.CourseCode).HasMaxLength(30);
            entity.Property(e => e.CourseName).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.TotalSessions).HasDefaultValue(24);
            entity.Property(e => e.TuitionFee).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Center).WithMany(p => p.Courses)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Courses_Centers");

            entity.HasOne(d => d.DeletedByUser).WithMany(p => p.Courses)
                .HasForeignKey(d => d.DeletedByUserId)
                .HasConstraintName("FK_Courses_DeletedByUser");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Enrollme__7F68771BF29B79D2");

            entity.HasIndex(e => e.ClassId, "IX_Enrollments_ClassId");

            entity.HasIndex(e => new { e.ClassId, e.Status, e.StudentId }, "IX_Enrollments_ClassId_Status_StudentId");

            entity.HasIndex(e => e.StudentId, "IX_Enrollments_StudentId");

            entity.HasIndex(e => new { e.StudentId, e.Status, e.ClassId }, "IX_Enrollments_StudentId_Status_ClassId");

            entity.HasIndex(e => new { e.StudentId, e.ClassId }, "UQ_Enrollments").IsUnique();

            entity.Property(e => e.EnrollDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Đang học");
            entity.Property(e => e.StatusChangedAt).HasPrecision(0);

            entity.HasOne(d => d.Class).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Classes");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Students");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK_Enrollments_UpdatedByUser");
        });

        modelBuilder.Entity<EnrollmentHistory>(entity =>
        {
            entity.HasIndex(e => new { e.EnrollmentId, e.ChangedAt }, "IX_EnrollmentHistories_EnrollmentId_ChangedAt").IsDescending(false, true);

            entity.Property(e => e.ChangedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NewStatus).HasMaxLength(20);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OldStatus).HasMaxLength(20);

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.EnrollmentHistories)
                .HasForeignKey(d => d.ChangedByUserId)
                .HasConstraintName("FK_EnrollmentHistories_ChangedByUser");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.EnrollmentHistories)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EnrollmentHistories_Enrollments");
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
            entity.Property(e => e.AttachmentUrl).HasMaxLength(500);

            entity.HasOne(d => d.Lesson).WithMany(p => p.Homeworks)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Homework_Lessons");
        });

        modelBuilder.Entity<HomeworkSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("PK__Homework__449EE1252C96FA8F");

            entity.HasIndex(e => e.HomeworkId, "IX_HomeworkSubmissions_HomeworkId");

            entity.HasIndex(e => e.StudentId, "IX_HomeworkSubmissions_StudentId");

            entity.HasIndex(e => new { e.HomeworkId, e.StudentId }, "UQ_HomeworkSubmissions").IsUnique();

            entity.Property(e => e.Feedback).HasMaxLength(500);
            entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Submitted");
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.SubmissionFileUrl).HasMaxLength(500);

            entity.HasOne(d => d.Homework).WithMany(p => p.HomeworkSubmissions)
                .HasForeignKey(d => d.HomeworkId)
                .HasConstraintName("FK_HomeworkSubmissions_Homework");

            entity.HasOne(d => d.Student).WithMany(p => p.HomeworkSubmissions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HomeworkSubmissions_Students");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAB514CC5E3C");

            entity.HasIndex(e => new { e.CenterId, e.Status }, "IX_Invoices_CenterId_Status");

            entity.HasIndex(e => e.ClassId, "IX_Invoices_ClassId");

            entity.HasIndex(e => e.Status, "IX_Invoices_Status");

            entity.HasIndex(e => e.StudentId, "IX_Invoices_StudentId");

            entity.HasIndex(e => new { e.CenterId, e.InvoiceCode }, "UX_Invoices_CenterId_InvoiceCode").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountNote).HasMaxLength(500);
            entity.Property(e => e.FinalAmount)
                .HasComputedColumnSql("([Amount]-[DiscountAmount])", true)
                .HasColumnType("decimal(19, 2)");
            entity.Property(e => e.InvoiceCode).HasMaxLength(30);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Unpaid");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Center).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Centers");

            entity.HasOne(d => d.Class).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Classes");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Users");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.EnrollmentId)
                .HasConstraintName("FK_Invoices_Enrollments");

            entity.HasOne(d => d.Student).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Students");
        });

        modelBuilder.Entity<InvoiceCodeCounter>(entity =>
        {
            entity.HasKey(e => new { e.CenterId, e.YearMonth }).HasName("PK__InvoiceC__E1FAC998524F52AC");

            entity.Property(e => e.YearMonth)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Center).WithMany(p => p.InvoiceCodeCounters)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceCodeCounters_Centers");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonId).HasName("PK__Lessons__B084ACD01DD907D6");

            entity.HasIndex(e => new { e.ClassId, e.LessonDate }, "IX_Lessons_ClassId_LessonDate");

            entity.HasIndex(e => new { e.ClassId, e.Status, e.LessonDate }, "IX_Lessons_ClassId_Status_LessonDate");

            entity.HasIndex(e => new { e.LessonDate, e.StartTime, e.EndTime, e.Status, e.ClassId }, "IX_Lessons_Date_Time_Status_Class").HasFilter("([StartTime] IS NOT NULL AND [EndTime] IS NOT NULL)");

            entity.HasIndex(e => new { e.ClassId, e.SessionNumber }, "UX_Lessons_ClassId_SessionNumber")
                .IsUnique()
                .HasFilter("([SessionNumber] IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.LessonTitle).HasMaxLength(200);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Scheduled");

            entity.HasOne(d => d.Class).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lessons_Classes");

            entity.HasOne(d => d.ClassSchedule).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.ClassScheduleId)
                .HasConstraintName("FK_Lessons_ClassSchedules");
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

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A3855CF874E");

            entity.HasIndex(e => new { e.CenterId, e.Status }, "IX_Payments_CenterId_Status");

            entity.HasIndex(e => e.InvoiceId, "IX_Payments_InvoiceId");

            entity.HasIndex(e => e.PaidAt, "IX_Payments_PaidAt");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.PaidAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PaymentMethod).HasMaxLength(30);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Confirmed");
            entity.Property(e => e.TransactionReference).HasMaxLength(100);

            entity.HasOne(d => d.Center).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Centers");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Invoices");

            entity.HasOne(d => d.ReceivedByUser).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReceivedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Users");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__Receipts__CC08C420369ED396");

            entity.HasIndex(e => new { e.CenterId, e.ReceiptNumber }, "UX_Receipts_CenterId_ReceiptNumber").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ClassName).HasMaxLength(150);
            entity.Property(e => e.CourseName).HasMaxLength(150);
            entity.Property(e => e.IssuedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(30);
            entity.Property(e => e.ReceiptNumber).HasMaxLength(30);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.StudentName).HasMaxLength(100);
            entity.Property(e => e.VoidReason).HasMaxLength(500);
            entity.Property(e => e.VoidedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Center).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Receipts_Centers");

            entity.HasOne(d => d.IssuedByUser).WithMany(p => p.ReceiptIssuedByUsers)
                .HasForeignKey(d => d.IssuedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Receipts_IssuedByUser");

            entity.HasOne(d => d.Payment).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Receipts_Payments");

            entity.HasOne(d => d.VoidedByUser).WithMany(p => p.ReceiptVoidedByUsers)
                .HasForeignKey(d => d.VoidedByUserId)
                .HasConstraintName("FK_Receipts_VoidedByUser");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A388E3B2F");

            entity.HasIndex(e => e.RoleCode, "UQ__Roles__D62CB59CC022D552").IsUnique();

            entity.Property(e => e.RoleCode).HasMaxLength(20);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__32863939AF21C496");

            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.RoomCode }, "IX_Rooms_CenterId_IsDeleted_RoomCode");

            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.Status, e.RoomId }, "IX_Rooms_CenterId_IsDeleted_Status_RoomId").IsDescending(false, false, false, true);

            entity.HasIndex(e => new { e.CenterId, e.Status }, "IX_Rooms_CenterId_Status");

            entity.HasIndex(e => new { e.CenterId, e.RoomCode }, "UX_Rooms_CenterId_RoomCode_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Location).HasMaxLength(150);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RoomCode).HasMaxLength(30);
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Center).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_Centers");

            entity.HasOne(d => d.DeletedByUser).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.DeletedByUserId)
                .HasConstraintName("FK_Rooms_DeletedByUser");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52B990550F83F");

            entity.HasIndex(e => e.CenterId, "IX_Students_CenterId");

            entity.HasIndex(e => new { e.CenterId, e.FullName }, "IX_Students_CenterId_FullName");

            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.Status, e.StudentId }, "IX_Students_CenterId_IsDeleted_Status_StudentId").IsDescending(false, false, false, true);

            entity.HasIndex(e => new { e.CenterId, e.ParentUserId }, "IX_Students_CenterId_ParentUserId");

            entity.HasIndex(e => new { e.CenterId, e.Status, e.StudentId }, "IX_Students_CenterId_Status_StudentId").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.CenterId, e.StudentCode }, "IX_Students_CenterId_StudentCode");

            entity.HasIndex(e => e.Email, "IX_Students_Email").HasFilter("([Email] IS NOT NULL)");

            entity.HasIndex(e => e.NormalizedPhoneNumber, "IX_Students_NormalizedPhoneNumber").HasFilter("([NormalizedPhoneNumber] IS NOT NULL)");

            entity.HasIndex(e => e.ParentUserId, "IX_Students_ParentUserId");

            entity.HasIndex(e => new { e.ParentUserId, e.IsDeleted }, "IX_Students_ParentUserId_IsDeleted");

            entity.HasIndex(e => e.StudentCode, "IX_Students_StudentCode");

            entity.HasIndex(e => new { e.CenterId, e.StudentCode }, "UX_Students_CenterId_StudentCode_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Ethnicity).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Hometown).HasMaxLength(150);
            entity.Property(e => e.IdentityIssuedPlace).HasMaxLength(150);
            entity.Property(e => e.IdentityNumber).HasMaxLength(20);
            entity.Property(e => e.NormalizedPhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PermanentAddress).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PlaceOfBirth).HasMaxLength(150);
            entity.Property(e => e.Religion).HasMaxLength(50);
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
                .HasConstraintName("FK_Students_Users");
        });

        modelBuilder.Entity<StudyShift>(entity =>
        {
            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.ShiftCode, e.ShiftName }, "IX_StudyShifts_CenterId_IsDeleted_Search");

            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.Status, e.StudyShiftId }, "IX_StudyShifts_CenterId_IsDeleted_Status").IsDescending(false, false, false, true);

            entity.HasIndex(e => new { e.CenterId, e.ShiftCode }, "UX_StudyShifts_CenterId_ShiftCode_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.ShiftCode).HasMaxLength(30);
            entity.Property(e => e.ShiftName).HasMaxLength(100);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Center).WithMany(p => p.StudyShifts)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudyShifts_Centers");

            entity.HasOne(d => d.DeletedByUser).WithMany(p => p.StudyShifts)
                .HasForeignKey(d => d.DeletedByUserId)
                .HasConstraintName("FK_StudyShifts_DeletedByUser");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherId).HasName("PK__Teachers__EDF2596449654B6C");

            entity.HasIndex(e => e.CenterId, "IX_Teachers_CenterId");

            entity.HasIndex(e => new { e.CenterId, e.IsDeleted, e.Status }, "IX_Teachers_CenterId_IsDeleted_Status");

            entity.HasIndex(e => new { e.CenterId, e.TeacherCode }, "IX_Teachers_CenterId_TeacherCode");

            entity.HasIndex(e => e.UserId, "IX_Teachers_UserId");

            entity.HasIndex(e => new { e.CenterId, e.TeacherCode }, "UQ_Teachers_CenterId_TeacherCode")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.UserId, "UQ__Teachers__1788CC4DD5305503").IsUnique();

            entity.Property(e => e.Specialization).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.TeacherCode).HasMaxLength(30);

            entity.HasOne(d => d.Center).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teachers_Centers");

            entity.HasOne(d => d.User).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teachers_Users");
        });

        modelBuilder.Entity<TeacherCodeCounter>(entity =>
        {
            entity.HasKey(e => e.CenterId).HasName("PK__TeacherC__398FC7F7E07FB32A");

            entity.Property(e => e.CenterId).ValueGeneratedNever();

            entity.HasOne(d => d.Center).WithOne(p => p.TeacherCodeCounter)
                .HasForeignKey<TeacherCodeCounter>(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherCodeCounters_Centers");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CB36B485D");

            entity.HasIndex(e => new { e.IsDeleted, e.Status, e.RoleId }, "IX_Users_IsDeleted_Status_RoleId");

            entity.HasIndex(e => e.NormalizedPhoneNumber, "IX_Users_NormalizedPhoneNumber");

            entity.HasIndex(e => new { e.RoleId, e.Status }, "IX_Users_RoleId_Status");

            entity.HasIndex(e => e.Email, "UX_Users_Email_NotNull")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL AND [IsDeleted]=(0))");

            entity.HasIndex(e => e.IdentityNumber, "UX_Users_IdentityNumber_NotNull")
                .IsUnique()
                .HasFilter("([IdentityNumber] IS NOT NULL AND [IsDeleted]=(0))");

            entity.HasIndex(e => e.NormalizedPhoneNumber, "UX_Users_NormalizedPhoneNumber_NotNull")
                .IsUnique()
                .HasFilter("([NormalizedPhoneNumber] IS NOT NULL AND [IsDeleted]=(0))");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CurrentAddress).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.EmailConfirmed).HasDefaultValue(true);
            entity.Property(e => e.Ethnicity).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Hometown).HasMaxLength(150);
            entity.Property(e => e.IdentityIssuedPlace).HasMaxLength(150);
            entity.Property(e => e.IdentityNumber).HasMaxLength(20);
            entity.Property(e => e.NormalizedPhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PermanentAddress).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PlaceOfBirth).HasMaxLength(150);
            entity.Property(e => e.Religion).HasMaxLength(50);
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

        modelBuilder.Entity<VwRevenueByPayment>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RevenueByPayment");

            entity.Property(e => e.RevenueAmount).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwRoomOverview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RoomOverview");

            entity.Property(e => e.LatestClassName).HasMaxLength(150);
            entity.Property(e => e.LatestScheduleText).HasMaxLength(255);
            entity.Property(e => e.Location).HasMaxLength(150);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RoomCode).HasMaxLength(30);
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<VwStudentOverview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StudentOverview");

            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CenterName).HasMaxLength(150);
            entity.Property(e => e.CurrentClassCode).HasMaxLength(30);
            entity.Property(e => e.CurrentClassName).HasMaxLength(150);
            entity.Property(e => e.CurrentCourseName).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.ParentEmail).HasMaxLength(150);
            entity.Property(e => e.ParentName).HasMaxLength(100);
            entity.Property(e => e.ParentPhone).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.StudentCode).HasMaxLength(30);
            entity.Property(e => e.StudentEmail).HasMaxLength(150);
            entity.Property(e => e.StudentName).HasMaxLength(100);
            entity.Property(e => e.StudentPhone).HasMaxLength(20);
        });

        modelBuilder.Entity<VwStudyShiftOverview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StudyShiftOverview");

            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.ShiftCode).HasMaxLength(30);
            entity.Property(e => e.ShiftName).HasMaxLength(100);
            entity.Property(e => e.StartTime).HasPrecision(0);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
