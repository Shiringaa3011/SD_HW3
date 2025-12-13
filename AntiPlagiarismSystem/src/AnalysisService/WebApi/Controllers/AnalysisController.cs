using AnalysisService.Application.DTOs;
using AnalysisService.Application.UserCases.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalysisService.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController(
        IAnalyzeFileUseCase analyzeFileUseCase,
        IGetReportUseCase getReportUseCase,
        IGetAssignmentReportsUseCase getAssignmentReportsUseCase) : ControllerBase
    {
        private readonly IAnalyzeFileUseCase _analyzeFileUseCase = analyzeFileUseCase;
        private readonly IGetReportUseCase _getReportUseCase = getReportUseCase;
        private readonly IGetAssignmentReportsUseCase _getAssignmentReportsUseCase = getAssignmentReportsUseCase;

        [HttpPost("analyze")]
        public IActionResult Analyze([FromBody] AnalyzeFileRequest request)
        {
            try
            {
                AnalyzeFileResponse result = _analyzeFileUseCase.Execute(request);
                return Ok(result);
            }
            catch (Domain.Exceptions.AnalysisException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    ex.ErrorType,
                    ex.ErrorCode,
                    ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    ex.Message
                });
            }
        }

        [HttpGet("reports/{reportId}")]
        public IActionResult GetReport(Guid reportId)
        {
            try
            {
                if (reportId == Guid.Empty)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        ErrorType = "Validation",
                        ErrorCode = "EMPTY_REPORT_ID",
                        Message = "Report ID cannot be empty"
                    });
                }

                GetReportRequest request = new() { ReportId = reportId };
                GetReportResponse? result = _getReportUseCase.Execute(request);

                return result == null
                    ? NotFound(new
                    {
                        Success = false,
                        Message = "Report not found"
                    })
                    : Ok(result);
            }
            catch (Domain.Exceptions.AnalysisException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    ex.ErrorType,
                    ex.ErrorCode,
                    ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    ex.Message
                });
            }
        }

        [HttpGet("works/{assignmentId}/reports")]
        public IActionResult GetAssignmentReports(Guid assignmentId)
        {
            try
            {
                if (assignmentId == Guid.Empty)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        ErrorType = "Validation",
                        ErrorCode = "EMPTY_ASSIGNMENT_ID",
                        Message = "Assignment ID cannot be empty"
                    });
                }

                GetAssignmentReportsRequest request = new() { AssignmentId = assignmentId };
                GetAssignmentReportsResponse result = _getAssignmentReportsUseCase.Execute(request);

                return Ok(result);
            }
            catch (Domain.Exceptions.AnalysisException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    ex.ErrorType,
                    ex.ErrorCode,
                    ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    ex.Message
                });
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "AnalysisService"
            });
        }
    }
}
