using Microsoft.AspNetCore.Authentication;
using SmartX.API.Services.PredictiveEngine;
using SmartX.Application;
using SmartX.Application.Services.Mesh.Monitoring;
using SmartX.Application.Services.PredictiveEngine;
using SmartX.Application.Services.Sensors.Interfaces;
using SmartX.Domain.Interfaces;
using SmartX.Infrastructure;
using SmartX.Infrastructure.Authentication.Firebase;
using SmartX.Shared.Mapping;


namespace SmartX.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen()
                .AddApplication()
                .AddInfrastructure(builder.Configuration)
                .AddSmartXMapping()
                .AddControllers();

            builder.Services.AddScoped<IPredictiveWarningService, LivePredictiveWarningService>();

            builder.Services.AddScoped<IPredictiveSuggestionProvider, PredictiveSuggestionProvider>();


            // Configure Mapster
            MappingConfiguration.Configure();

            // Register mapper
            builder.Services.AddScoped<IMapper, MapsterObjectMapper>();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Firebase";
                    options.DefaultChallengeScheme = "Firebase";
                })
                .AddScheme<AuthenticationSchemeOptions, FirebaseAuthHandler>(
                    "Firebase",
                    options => { });

            builder.Services.AddScoped<IFirebaseUserService, FirebaseAuthService>();

            builder.Services.AddAuthorization();

            builder.Services.AddHttpContextAccessor();

            var firebaseProjectId =
                builder.Configuration["Firebase:ProjectId"]
                ?? throw new InvalidOperationException(
                    "Firebase ProjectId is not configured.");

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.Configure<MeshMonitoringOptions>(

            builder.Configuration.GetSection("MeshMonitoring"));

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var sensorRepository =
                    services.GetRequiredService<ISensorRepository>();

                Console.WriteLine(
                    $"Sensor repository: {sensorRepository.GetType().FullName}");

                var commandService =
                    services.GetRequiredService<ISensorCommandService>();

                Console.WriteLine(
                    $"Command service: {commandService.GetType().FullName}");
            }
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