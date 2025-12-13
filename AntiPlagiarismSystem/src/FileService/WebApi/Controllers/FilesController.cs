using FileService.Application.DTOs;
using FileService.Application.UserCases.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FileService.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController(
        IUploadFileUseCase uploadFileUseCase,
        IGetFileUseCase getFileUseCase,
        IGetFileMetadataUseCase getFileMetadataUseCase,
        IGetAssignmentFilesUseCase getAssignmentFilesUseCase) : Controller
    {
        private readonly IUploadFileUseCase _uploadFileUseCase = uploadFileUseCase;
        private readonly IGetFileUseCase _getFileUseCase = getFileUseCase;
        private readonly IGetFileMetadataUseCase _getFileMetadataUseCase = getFileMetadataUseCase;
        private readonly IGetAssignmentFilesUseCase _getAssignmentFilesUseCase = getAssignmentFilesUseCase;

        [HttpPost("upload")]
        public IActionResult UploadFile([FromForm] UploadFileApiRequest request)
        {
            try
            {
                // Конвертируем IFormFile в byte[]
                byte[] fileData;
                using (MemoryStream memoryStream = new())
                {
                    request.File.CopyTo(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                UploadFileRequest uploadRequest = new()
                {
                    FileName = request.File.FileName,
                    FileData = fileData,
                    StudentName = request.StudentName,
                    StudentSurname = request.StudentSurname,
                    GroupNumber = request.GroupNumber,
                    AssignmentId = request.AssignmentId
                };

                UploadFileResponse result = _uploadFileUseCase.Execute(uploadRequest);

                return Ok(new
                {
                    Success = true,
                    Data = result,
                    Message = "File uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    ex.Message
                });
            }
        }

        [HttpPost("upload-with-analysis")]
        public IActionResult UploadWithAnalysis([FromForm] UploadFileApiRequest request)
        {
            try
            {
                byte[] fileData;
                using (MemoryStream ms = new())
                {
                    request.File.CopyTo(ms);
                    fileData = ms.ToArray();
                }

                UploadFileRequest uploadRequest = new()
                {
                    FileName = request.File.FileName,
                    FileData = fileData,
                    StudentName = request.StudentName,
                    StudentSurname = request.StudentSurname,
                    GroupNumber = request.GroupNumber,
                    AssignmentId = request.AssignmentId
                };

                UploadFileResponse uploadResult = _uploadFileUseCase.Execute(uploadRequest);

                using HttpClient httpClient = new() { Timeout = TimeSpan.FromMinutes(5) };
                
                var analysisRequest = new { fileId = uploadResult.FileId };
                string jsonRequest = JsonSerializer.Serialize(analysisRequest);
                StringContent jsonContent = new(jsonRequest, Encoding.UTF8, "application/json");

                HttpRequestMessage httpRequest = new(HttpMethod.Post, "http://analysis-service:8080/api/analysis/analyze")
                {
                    Content = jsonContent
                };

                HttpResponseMessage analysisResponse = httpClient.Send(httpRequest);

                if (!analysisResponse.IsSuccessStatusCode)
                {
                    string errorContent = analysisResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return StatusCode(502, new
                    {
                        error = "Analysis service unavailable",
                        fileId = uploadResult.FileId,
                        message = "File uploaded successfully. Analysis will be performed later.",
                        details = errorContent
                    });
                }

                string responseContent = analysisResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                AnalyzeFileResponseDto? analysisResult = JsonSerializer.Deserialize<AnalyzeFileResponseDto>(responseContent);
                
                if (analysisResult == null)
                {
                    return StatusCode(502, new
                    {
                        error = "Analysis service returned invalid response",
                        fileId = uploadResult.FileId,
                        message = "File uploaded successfully. Analysis will be performed later."
                    });
                }
                
                // 5. Возвращаем результат
                return Ok(new UploadWithAnalysisResponse
                {
                    FileId = uploadResult.FileId,
                    FileIdentifier = uploadResult.FileIdentifier,
                    IsPlagiarized = analysisResult.IsPlagiarized,
                    SimilarityPercentage = analysisResult.SimilarityPercentage,
                    AnalysisDate = analysisResult.AnalysisDate
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{fileId}")]
        public IActionResult GetFile(Guid fileId, [FromQuery] bool includeMetadata = true)
        {
            try
            {
                GetFileRequest request = new()
                {
                    FileId = fileId,
                    IncludeMetadata = includeMetadata
                };

                GetFileResponse result = _getFileUseCase.Execute(request);

                return File(result.FileData, "application/octet-stream", result.FileName);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
        }

        [HttpGet("{fileId}/metadata")]
        public IActionResult GetFileMetadata(Guid fileId)
        {
            try
            {
                GetFileMetadataRequest request = new() { FileId = fileId };
                GetFileMetadataResponse result = _getFileMetadataUseCase.Execute(request);

                return !result.FileExists ? NotFound(new { Message = "File not found" }) : Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
        }

        [HttpGet("assignment/{assignmentId}")]
        public IActionResult GetAssignmentFiles(Guid assignmentId)
        {
            try
            {
                List<AssignmentFileInfo> files = _getAssignmentFilesUseCase.Execute(assignmentId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "FileService"
            });
        }
    }
    public class UploadFileApiRequest
    {
        public required IFormFile File { get; set; }
        public required string StudentName { get; set; }
        public required string StudentSurname { get; set; }
        public int GroupNumber { get; set; }
        public Guid AssignmentId { get; set; }
    }

    public class AnalyzeFileResponseDto
    {
        public Guid ReportId { get; set; }
        public Guid FileId { get; set; }
        public bool IsPlagiarized { get; set; }
        public double SimilarityPercentage { get; set; }
        public DateTime AnalysisDate { get; set; }
    }

    public class UploadWithAnalysisResponse
    {
        public Guid FileId { get; set; }
        public string FileIdentifier { get; set; } = string.Empty;
        public bool IsPlagiarized { get; set; }
        public double SimilarityPercentage { get; set; }
        public DateTime AnalysisDate { get; set; }
    }
}
