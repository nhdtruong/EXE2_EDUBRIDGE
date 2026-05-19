using EduBridge.Data;
using Microsoft.EntityFrameworkCore;
using EduBridge.Hubs;

namespace EduBridge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Entity Framework Core ApplicationDbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add services to the container.
            builder.Services.AddRazorPages();
            
            // Add SignalR
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();
            
            // Map SignalR Hub
            app.MapHub<ChatHub>("/chatHub");

            // Seed initial data and auto migrate
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try {
                    db.Database.Migrate();
                    // Seed mock users and conversation if empty
                    if (!db.Users.Any())
                    {
                        var teacher = new Models.User { Name = "Nguyễn Văn A", Email = "teacher@edubridge.com", Role = "Teacher" };
                        var parent1 = new Models.User { Name = "Nguyễn Văn Hùng", Email = "parent1@edubridge.com", Role = "Parent" };
                        var parent2 = new Models.User { Name = "Trần Văn Toàn", Email = "parent2@edubridge.com", Role = "Parent" };
                        var parent3 = new Models.User { Name = "Lê Thị Mai", Email = "parent3@edubridge.com", Role = "Parent" };
                        db.Users.AddRange(teacher, parent1, parent2, parent3);
                        db.SaveChanges();
                        
                        var conv1 = new Models.Conversation {
                            TeacherUserId = teacher.Id, ParentUserId = parent1.Id, StudentName = "Nguyễn Văn Minh",
                            LastMessage = "Con em học bài này hơi khó, có thể giải thích thêm không ạ?", LastMessageTime = DateTime.Now.AddHours(-1), UnreadCount = 2
                        };
                        var conv2 = new Models.Conversation {
                            TeacherUserId = teacher.Id, ParentUserId = parent2.Id, StudentName = "Trần Thị Lan",
                            LastMessage = "Cảm ơn cô đã quan tâm đến con em.", LastMessageTime = DateTime.Now.AddHours(-2), UnreadCount = 0
                        };
                        var conv3 = new Models.Conversation {
                            TeacherUserId = teacher.Id, ParentUserId = parent3.Id, StudentName = "Lê Hoàng Nam",
                            LastMessage = "Con em có thể học bù vào thứ 7 được không ạ?", LastMessageTime = DateTime.Now.AddDays(-1), UnreadCount = 1
                        };
                        db.Conversations.AddRange(conv1, conv2, conv3);
                        db.SaveChanges();

                        var msg1 = new Models.Message { ConversationId = conv1.Id, SenderId = parent1.Id, Content = "Xin chào cô, con em học bài này hơi khó. Có thể giải thích thêm không ạ?", SentAt = conv1.LastMessageTime.AddMinutes(-10) };
                        var msg2 = new Models.Message { ConversationId = conv1.Id, SenderId = teacher.Id, Content = "Chào phụ huynh. Cô sẽ gửi thêm tài liệu và video bài giảng cho em ạ. Phụ huynh có thể cho em xem lại.", SentAt = conv1.LastMessageTime.AddMinutes(-5) };
                        var msg3 = new Models.Message { ConversationId = conv1.Id, SenderId = parent1.Id, Content = "Cảm ơn cô nhiều ạ!", SentAt = conv1.LastMessageTime };
                        db.Messages.AddRange(msg1, msg2, msg3);
                        db.SaveChanges();
                    }
                } catch { }
            }

            app.Run();
        }
    }
}
