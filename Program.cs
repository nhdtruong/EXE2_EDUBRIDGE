using EduBridge.Data;
using EduBridge.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

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

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString)
            );

            builder.Services.AddMemoryCache();

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

            var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];

            if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
            {
                builder.Services
                    .AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
                    .SetApplicationName("EduBridge");
            }

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/");
                options.Conventions.AllowAnonymousToPage("/Login");
                options.Conventions.AllowAnonymousToPage("/AccessDenied");
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

            builder.Services.AddSignalR();

            var app = builder.Build();

            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapRazorPages();
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}
