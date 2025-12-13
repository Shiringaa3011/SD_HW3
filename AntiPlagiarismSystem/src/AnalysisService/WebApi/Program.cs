using AnalysisService.Application.Ports;
using AnalysisService.Application.UserCases.Implementations;
using AnalysisService.Application.UserCases.Interfaces;
using AnalysisService.Infrastructure.Clients;
using AnalysisService.Infrastructure.Persistence;
using AnalysisService.Infrastructure.Storage;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnalysisService.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Конфигурация
            _ = builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Регистрация сервисов
            ConfigureServices(builder.Services, builder.Configuration);

            WebApplication app = builder.Build();

            // Конфигурация middleware
            ConfigureMiddleware(app);

            Console.WriteLine($"Analysis Service запущен");
            Console.WriteLine($"Swagger: http://localhost:8080/swagger");

            app.Run("http://*:8080");
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            _ = services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // Swagger
            _ = services.AddEndpointsApiExplorer();
            _ = services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Analysis Service", Version = "v1" }));

            // Регистрация Use Cases
            _ = services.AddScoped<IAnalyzeFileUseCase, AnalyzeFileUseCase>();
            _ = services.AddScoped<IGetReportUseCase, GetReportUseCase>();
            _ = services.AddScoped<IGetAssignmentReportsUseCase, GetAssignmentReportsUseCase>();

            // Регистрация портов (инфраструктура)
            string fileServiceUrl = configuration["FileService:BaseUrl"] ?? "http://file-service:8080";
            _ = services.AddSingleton<IFileServiceClient>(sp => new FileServiceClient(fileServiceUrl));

            string? connectionString = configuration.GetConnectionString("PostgreSQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be empty or whitespace.");
            }
            _ = services.AddSingleton<IAnalysisReportRepository>(sp =>
                new PostgreSqlAnalysisReportRepository(connectionString));

            string reportsPath = configuration["ReportStorage:LocalPath"] ?? "./reports";
            _ = services.AddSingleton<IReportStorage>(sp => new LocalReportStorage(reportsPath));

            // Логирование
            _ = services.AddLogging(logging =>
            {
                _ = logging.AddConsole();
                _ = logging.AddDebug();
            });

            _ = services.AddCors(options => options.AddPolicy("AllowAll", policy => _ = policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()));
        }

        private static void ConfigureMiddleware(WebApplication app)
        {
            _ = app.UseRouting();

            _ = app.UseExceptionHandler(appError => appError.Run(context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    Message = "Internal server error",
                    RequestId = context.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                string json = JsonSerializer.Serialize(errorResponse);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                context.Response.Body.Write(bytes, 0, bytes.Length);
                context.Response.Body.Flush();

                return Task.CompletedTask;
            }));

            if (app.Environment.IsDevelopment())
            {
                _ = app.UseSwagger();
                _ = app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Analysis Service API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            _ = app.UseHttpsRedirection();
            _ = app.UseCors("AllowAll");
            _ = app.UseAuthorization();
            _ = app.MapControllers();

            // Health check endpoint
            _ = app.MapGet("/health", () => new
            {
                status = "healthy",
                service = "analysis-service",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });

            // Root endpoint
            _ = app.MapGet("/", () => "Analysis Service is running! Use /swagger for API documentation.");
        }
    }
}
