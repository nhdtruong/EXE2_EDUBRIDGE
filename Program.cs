using EduBridge.Data;
using EduBridge.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EduBridge.Services.Auth;
using EduBridge.Services.Classes;
using EduBridge.Services.Courses;
using EduBridge.Services.Dashboard;
using EduBridge.Services.Finance;
using EduBridge.Services.Parents;
using EduBridge.Services.Rooms;
using EduBridge.Services.Shifts;
using EduBridge.Services.Students;
using EduBridge.Services.Teachers;
using EduBridge.Services.Settings;
using EduBridge.Services.Storage;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace EduBridge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
            }

            builder.Services.AddScoped<IClassLessonPlanner, ClassLessonPlanner>();
            builder.Services.AddScoped<IClassCreationService, ClassCreationService>();
            builder.Services.AddScoped<IClassManagementService, ClassManagementService>();
            builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();
            builder.Services.AddScoped<IShiftManagementService, ShiftManagementService>();
            builder.Services.AddScoped<IClassEnrollmentService, ClassEnrollmentService>();
            builder.Services.AddScoped<IParentManagementService, ParentManagementService>();
            builder.Services.AddScoped<EduBridge.Services.Teachers.ITeacherManagementService, EduBridge.Services.Teachers.TeacherManagementService>();
            builder.Services.AddScoped<EduBridge.Services.Students.IStudentManagementService, EduBridge.Services.Students.StudentManagementService>();
            builder.Services.AddScoped<EduBridge.Services.Courses.ICourseManagementService, EduBridge.Services.Courses.CourseManagementService>();
            builder.Services.AddScoped<EduBridge.Services.Dashboard.IDashboardService, EduBridge.Services.Dashboard.DashboardService>();
            builder.Services.AddScoped<EduBridge.Services.Auth.IAccountAuthenticationService, EduBridge.Services.Auth.AccountAuthenticationService>();
            builder.Services.AddScoped<EduBridge.Services.Auth.IJwtTokenService, EduBridge.Services.Auth.JwtTokenService>();
            builder.Services.AddScoped<EduBridge.Services.Lectures.ILectureService, EduBridge.Services.Lectures.LectureService>();
            builder.Services.AddScoped<EduBridge.Services.Homeworks.IHomeworkService, EduBridge.Services.Homeworks.HomeworkService>();
            builder.Services.AddScoped<EduBridge.Services.Grades.IGradeService, EduBridge.Services.Grades.GradeService>();
            builder.Services.AddScoped<EduBridge.Services.Attendance.IAttendanceService, EduBridge.Services.Attendance.AttendanceService>();
            builder.Services.AddScoped<EduBridge.Services.Chat.IChatService, EduBridge.Services.Chat.ChatService>();
            builder.Services.AddScoped<EduBridge.Services.Notifications.INotificationService, EduBridge.Services.Notifications.NotificationService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<ITeacherDashboardService, TeacherDashboardService>();
            builder.Services.AddScoped<IInvoiceService, InvoiceService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IReceiptService, ReceiptService>();
            builder.Services.AddScoped<IFinanceSummaryService, FinanceSummaryService>();
            builder.Services.AddScoped<ICenterSettingsService, CenterSettingsService>();
            builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
            builder.Services.AddScoped<EduBridge.Services.ParentApp.IParentAppService, EduBridge.Services.ParentApp.ParentAppService>();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString)
            );

            builder.Services.AddMemoryCache();

            var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
                new[]
                {
                    "http://localhost:8081",
                    "http://127.0.0.1:8081",
                    "http://localhost:19006",
                    "http://127.0.0.1:19006",
                    "http://localhost:19007",
                    "http://127.0.0.1:19007"
                };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AppClients", policy =>
                {
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 5 * 1024 * 1024;
            });

            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<AppDbContext>()
                .SetApplicationName("EduBridge");

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "EduBridge API",
                    Version = "v1",
                    Description = "API phục vụ EduBridge Web, Mobile App và các tích hợp bên ngoài."
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập JWT access token."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/");
                options.Conventions.AllowAnonymousToPage("/Login");
                options.Conventions.AllowAnonymousToPage("/AccessDenied");
                options.Conventions.AllowAnonymousToPage("/NotFound");
                options.Conventions.AuthorizePage("/AdminDashboard", "AdminOnly");
                options.Conventions.AuthorizePage("/AdminClasses", "AdminOnly");
                options.Conventions.AuthorizePage("/AdminStudents", "AdminOnly");
                options.Conventions.AuthorizePage("/AdminTeachers", "AdminOnly");
                options.Conventions.AuthorizePage("/AdminFinance", "AdminOnly");
                options.Conventions.AuthorizePage("/AdminSettings", "AdminOnly");
                options.Conventions.AuthorizeFolder("/Teacher", "TeacherOnly");
            });

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login";
                    options.LogoutPath = "/Logout";
                    options.AccessDeniedPath = "/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;

                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EduBridge",
                        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EduBridgeUsers",
                        IssuerSigningKey = new SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "EduBridge-Development-Only-Replace-On-Server-2026"))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/chatHub"))
                                context.Token = token;
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("OWNER"));

                options.AddPolicy("TeacherOnly", policy =>
                    policy.RequireRole("TEACHER"));

                options.AddPolicy("ParentOnly", policy =>
                    policy.RequireRole("PARENT"));
            });

            var signalRBuilder = builder.Services.AddSignalR();
            var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
                {
                    options.Configuration.ChannelPrefix = "EduBridge_Chat";
                });
            }

            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("LoginRateLimit", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            var app = builder.Build();

            app.UseForwardedHeaders();

            app.UseStatusCodePagesWithReExecute("/NotFound");

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            else
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EduBridge API v1");
                    options.RoutePrefix = "swagger";
                    options.DocumentTitle = "EduBridge API Documentation";
                });
            }

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                await next();
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("AppClients");

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapRazorPages();
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}
