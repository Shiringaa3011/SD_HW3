using FileService.Application.Ports;
using FileService.Application.UserCases.Implementations;
using FileService.Application.UserCases.Interfaces;
using FileService.Infrastructure.Persistence;
using FileService.Infrastructure.Storage;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileService.WebApi
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

            Console.WriteLine($"File Service запущен");
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

            _ = services.AddEndpointsApiExplorer();
            _ = services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "File Service", Version = "v1" }));

            _ = services.AddScoped<IUploadFileUseCase, UploadFileUseCase>();
            _ = services.AddScoped<IGetFileUseCase, GetFileUseCase>();
            _ = services.AddScoped<IGetFileMetadataUseCase, GetFileMetadataUseCase>();
            _ = services.AddScoped<IGetAssignmentFilesUseCase, GetAssignmentFilesUseCase>();

            string storagePath = configuration["FileStorage:LocalPath"] ?? "./uploads";
            _ = services.AddSingleton<IFileStorage>(sp => new LocalFileStorage(storagePath));

            string? connectionString = configuration.GetConnectionString("PostgreSQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException( "Value cannot be empty or whitespace.");
            }
            _ = services.AddSingleton<IFileMetadataRepository>(sp =>
                new PostgreSqlFileMetadataRepository(connectionString));

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
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "File Service API v1");
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
                service = "file-service",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });

            // Root endpoint
            _ = app.MapGet("/", () => "File Service is running! Use /swagger for API documentation.");
        }
    }

}
