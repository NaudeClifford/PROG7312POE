using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartX.Application;
using SmartX.Infrastructure;
using SmartX.Infrastructure.Authentication.Firebase;

namespace SmartX.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer()
                            .AddSwaggerGen()
                            .AddApplication()
                            .AddInfrastructure(builder.Configuration)
                            .AddControllers();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Firebase";
                    options.DefaultChallengeScheme = "Firebase";
                })
                .AddScheme<AuthenticationSchemeOptions, FirebaseAuthHandler>(
                    "Firebase",
                    options => { });

            builder.Services.AddAuthorization();


            var firebaseProjectId =
                builder.Configuration["Firebase:ProjectId"]
                ?? throw new InvalidOperationException(
                    "Firebase ProjectId is not configured.");


            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                options.Authority =
                $"https://securetoken.google.com/{firebaseProjectId}";

                options.TokenValidationParameters =
                    new TokenValidationParameters
                {
                ValidateIssuer = true,
                ValidIssuer =
                    $"https://securetoken.google.com/{firebaseProjectId}",

                ValidateAudience = true,
                ValidAudience = firebaseProjectId,

                ValidateLifetime = true
                };
            });


            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseSwagger();

            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapGet("/", () => "API Running");

            app.MapGet("/test", () => "Test works");

            app.MapControllers();

            app.Run();
        }
    }
}