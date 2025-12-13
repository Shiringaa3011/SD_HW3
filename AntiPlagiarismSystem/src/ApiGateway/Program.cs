using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            _ = builder.Services.Configure<IISServerOptions>(options => options.AllowSynchronousIO = true);
            _ = builder.Services.Configure<KestrelServerOptions>(options => options.AllowSynchronousIO = true);

            // Swagger
            _ = builder.Services.AddEndpointsApiExplorer();
            _ = builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
            {
                Title = "AntiPlagiarism Gateway API",
                Version = "v1",
                Description = "Gateway для системы проверки плагиата"
            }));

            _ = builder.Services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
                options.MultipartHeadersLengthLimit = int.MaxValue;
                options.MultipartBoundaryLengthLimit = int.MaxValue;
                options.MemoryBufferThreshold = 1024 * 1024; // 1MB
            });

            WebApplication app = builder.Build();

            // Swagger
            _ = app.UseSwagger();
            _ = app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway v1");
                c.RoutePrefix = "swagger";
            });

            //ENDPOINTS

            // 1. Gateway endpoints
            _ = app.MapGet("/", () => "AntiPlagiarism Gateway API v1.0");

            _ = app.MapGet("/health", () =>
            {
                var response = new { status = "healthy", service = "gateway", timestamp = DateTime.UtcNow, version = "1.0.0" };
                return Results.Json(response);
            });

            // 2. Health checks
            _ = app.MapGet("/health/files", () =>
            {
                try
                {
                    using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
                    HttpResponseMessage response = client.GetAsync("http://file-service:8080/health").GetAwaiter().GetResult();
                    string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return Results.Text(content, "application/json");
                }
                catch (Exception ex)
                {
                    return Results.Text(JsonSerializer.Serialize(new { error = "FileService недоступен", details = ex.Message }),
                        "application/json", Encoding.UTF8, 503);
                }
            });

            _ = app.MapGet("/health/analysis", () =>
            {
                try
                {
                    using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
                    HttpResponseMessage response = client.GetAsync("http://analysis-service:8080/health").GetAwaiter().GetResult();
                    string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return Results.Text(content, "application/json");
                }
                catch (Exception ex)
                {
                    return Results.Text(JsonSerializer.Serialize(new { error = "AnalysisService недоступен", details = ex.Message }),
                        "application/json", Encoding.UTF8, 503);
                }
            });

            _ = app.MapPost("/api/files/upload-with-analysis",
                async (
                    IFormFile file,
                    string studentName,
                    string studentSurname,
                    int groupNumber,
                    Guid assignmentId) =>
                {
                    MultipartFormDataContent formData = new() {
                        { new StreamContent(file.OpenReadStream()), "file", file.FileName },
                        { new StringContent(studentName), "studentName" },
                        { new StringContent(studentSurname), "studentSurname" },
                        { new StringContent(groupNumber.ToString()), "groupNumber" },
                        { new StringContent(assignmentId.ToString()), "assignmentId" }
                    };
                    using HttpClient client = new();
                    HttpResponseMessage response = await client.PostAsync("http://file-service:8080/api/files/upload-with-analysis", formData);
                    return Results.Text(await response.Content.ReadAsStringAsync(), "application/json");
                })
                .DisableAntiforgery()
                .Accepts<IFormFile>("multipart/form-data")
                .Produces(200);
            // 4. Прокси для остальных запросов FileService
            _ = app.Map("/api/files/{**rest}", HandleFileServiceRequest);
            _ = app.Map("/api/Files/{**rest}", HandleFileServiceRequest);

            // 5. Прокси для AnalysisService
            _ = app.Map("/api/analysis/{**rest}", HandleAnalysisServiceRequest);

            // 6. Специальные маршруты
            _ = app.MapGet("/reports/{workId}", (string workId) =>
            {
                if (!Guid.TryParse(workId, out Guid parsedWorkId))
                {
                    return Results.BadRequest(new { error = "Invalid ID format", workId });
                }

                try
                {
                    using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(5) };
                    HttpResponseMessage response = client.GetAsync($"http://analysis-service:8080/api/analysis/works/{parsedWorkId}/reports").GetAwaiter().GetResult();
                    string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return Results.Text(content, "application/json", Encoding.UTF8, (int)response.StatusCode);
                }
                catch (Exception ex)
                {
                    return Results.Text(JsonSerializer.Serialize(new { error = "AnalysisService error", message = ex.Message }),
                        "application/json", Encoding.UTF8, 502);
                }
            });
;
            Console.WriteLine("Gateway запущен!");
            Console.WriteLine("Swagger UI: http://localhost:5000/swagger");

            app.Run();
        }

        // ОБРАБОТЧИКИ ПРОКСИ

        private static IResult HandleFileServiceRequest(HttpContext context)
        {
            // Для всех остальных запросов FileService кроме upload-with-analysis
            return context.Request.Path.Value?.Contains("upload-with-analysis", StringComparison.OrdinalIgnoreCase) == true
                ? Results.Json(new { error = "Use /api/files/upload-with-analysis endpoint" }, statusCode: 400)
                : HandleProxyRequest(context, "http://file-service:8080", "FileService");
        }

        private static IResult HandleAnalysisServiceRequest(HttpContext context)
        {
            return HandleProxyRequest(context, "http://analysis-service:8080", "AnalysisService");
        }

        private static IResult HandleProxyRequest(HttpContext context, string baseUrl, string serviceName)
        {
            try
            {
                IHttpBodyControlFeature? syncIOFeature = context.Features.Get<IHttpBodyControlFeature>();
                if (syncIOFeature != null)
                {
                    syncIOFeature.AllowSynchronousIO = true;
                }

                HttpRequestMessage request = CreateProxyRequest(context.Request, baseUrl);

                using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(10) };
                HttpResponseMessage response = client.Send(request);

                Stream responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                byte[] responseBytes = ReadStreamToBytes(responseStream);

                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                {
                    if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                }

                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
                {
                    if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                }

                context.Response.StatusCode = (int)response.StatusCode;
                context.Response.ContentLength = responseBytes.Length;
                context.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                context.Response.Body.Flush();

                return Results.Empty;
            }
            catch (Exception ex)
            {
                var error = new
                {
                    error = $"{serviceName} error",
                    message = ex.Message,
                    path = context.Request.Path
                };

                string json = JsonSerializer.Serialize(error);
                return Results.Text(json, "application/json", Encoding.UTF8, 502);
            }
        }

        private static byte[] ReadStreamToBytes(Stream stream)
        {
            using MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        // СОЗДАНИЕ ПРОКСИ ЗАПРОСА

        private static HttpRequestMessage CreateProxyRequest(HttpRequest originalRequest, string targetBaseUrl)
        {
            HttpRequestMessage request = new()
            {
                // Метод
                Method = new HttpMethod(originalRequest.Method)
            };

            // URL
            string requestPath = originalRequest.Path.Value ?? "";
            string targetPath = requestPath;

            // Нормализуем путь для обработки (регистронезависимо)
            string normalizedPath = requestPath.ToLowerInvariant();

            if (normalizedPath.StartsWith("/api/files"))
            {
                int index = requestPath.IndexOf("/api/files", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    targetPath = requestPath[(index + "/api/files".Length)..];
                }
            }
            else if (normalizedPath.StartsWith("/api/analysis"))
            {
                int index = requestPath.IndexOf("/api/analysis", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    targetPath = requestPath[(index + "/api/analysis".Length)..];
                }
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = "/";
            }

            string targetUrl = targetBaseUrl.TrimEnd('/') + targetPath + originalRequest.QueryString;
            request.RequestUri = new Uri(targetUrl);

            foreach (KeyValuePair<string, StringValues> header in originalRequest.Headers)
            {
                if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    _ = request.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
                }
            }

            // Тело запроса
            if (originalRequest.Body != null && originalRequest.Body.CanRead &&
                (originalRequest.Method == "POST" ||
                 originalRequest.Method == "PUT" ||
                 originalRequest.Method == "PATCH"))
            {
                // Пропускаем multipart/form-data - для него отдельный обработчик
                if (originalRequest.ContentType?.Contains("multipart/form-data") == true)
                {
                    // Ничего не делаем - multipart обрабатывается отдельно
                }
                else if (originalRequest.HasFormContentType)
                {
                    IHttpBodyControlFeature? syncIOFeature = originalRequest.HttpContext.Features.Get<IHttpBodyControlFeature>();
                    if (syncIOFeature != null)
                    {
                        syncIOFeature.AllowSynchronousIO = true;
                    }

                    IFormCollection form = originalRequest.ReadFormAsync().GetAwaiter().GetResult();
                    List<KeyValuePair<string, string>> formData = [];

                    foreach (KeyValuePair<string, StringValues> field in form)
                    {
                        foreach (string? value in field.Value)
                        {
                            formData.Add(new KeyValuePair<string, string>(field.Key, value ?? ""));
                        }
                    }

                    request.Content = new FormUrlEncodedContent(formData);
                }
                else if (originalRequest.ContentType?.Contains("application/json") == true)
                {
                    IHttpBodyControlFeature? syncIOFeature = originalRequest.HttpContext.Features.Get<IHttpBodyControlFeature>();
                    if (syncIOFeature != null)
                    {
                        syncIOFeature.AllowSynchronousIO = true;
                    }

                    using StreamReader reader = new(originalRequest.Body, Encoding.UTF8, leaveOpen: true);
                    string json = reader.ReadToEnd();
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }
                else
                {
                    using MemoryStream ms = new();
                    originalRequest.Body.CopyTo(ms);
                    ms.Position = 0;
                    request.Content = new ByteArrayContent(ms.ToArray());

                    if (!string.IsNullOrEmpty(originalRequest.ContentType))
                    {
                        _ = request.Content.Headers.TryAddWithoutValidation("Content-Type", originalRequest.ContentType);
                    }
                }
            }

            return request;
        }
    }
}