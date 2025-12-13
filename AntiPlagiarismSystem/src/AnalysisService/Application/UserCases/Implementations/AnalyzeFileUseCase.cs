using AnalysisService.Application.DTOs;
using AnalysisService.Application.Ports;
using AnalysisService.Application.UserCases.Interfaces;
using AnalysisService.Domain.Entities;
using AnalysisService.Domain.Exceptions;
using AnalysisService.Domain.ValueObjects;
using System.Text;

namespace AnalysisService.Application.UserCases.Implementations
{
    public class AnalyzeFileUseCase(
        IFileServiceClient fileServiceClient,
        IAnalysisReportRepository reportRepository,
        IReportStorage reportStorage) : IAnalyzeFileUseCase
    {
        private readonly IFileServiceClient _fileServiceClient = fileServiceClient ?? throw new ArgumentNullException(nameof(fileServiceClient));
        private readonly IAnalysisReportRepository _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        private readonly IReportStorage _reportStorage = reportStorage ?? throw new ArgumentNullException(nameof(reportStorage));

        public AnalyzeFileResponse Execute(AnalyzeFileRequest request)
        {
            ValidateRequest(request);
            FileMetadata? fileMetadata = _fileServiceClient.GetFileMetadata(request.FileId) ?? throw new AnalysisException("Validation", "FILE_NOT_FOUND", $"File with ID {request.FileId} not found");
            byte[] fileData;
            try
            {
                fileData = _fileServiceClient.GetFileData(request.FileId);
            }
            catch (HttpRequestException ex)
            {
                throw new AnalysisException("FileService", "FILE_READ_ERROR", $"Failed to read file data: {ex.Message}", ex);
            }

            string fileContent = ExtractTextContent(fileData);

            List<AssignmentFileInfo> assignmentFiles = _fileServiceClient.GetAssignmentFiles(fileMetadata.AssignmentId);

            // Проверяем на плагиат: ищем более ранние сдачи с таким же содержимым
            bool isPlagiarized = false;
            double similarityPercentage = 0.0;
            Guid? plagiarizedFromFileId = null;

            foreach (AssignmentFileInfo otherFile in assignmentFiles)
            {
                if (otherFile.FileId == request.FileId)
                {
                    continue;
                }
                if (otherFile.UploadDate >= fileMetadata.UploadDate)
                {
                    continue;
                }
                try
                {
                    byte[] otherFileData = _fileServiceClient.GetFileData(otherFile.FileId);
                    string otherFileContent = ExtractTextContent(otherFileData);

                    if (fileContent.Equals(otherFileContent, StringComparison.OrdinalIgnoreCase))
                    {
                        isPlagiarized = true;
                        similarityPercentage = 100.0;
                        plagiarizedFromFileId = otherFile.FileId;
                        break;
                    }

                    double similarity = CalculateSimilarity(fileContent, otherFileContent);
                    if (similarity > similarityPercentage)
                    {
                        similarityPercentage = similarity;
                        if (similarity >= 80.0) // Порог плагиата - 80%
                        {
                            isPlagiarized = true;
                            plagiarizedFromFileId = otherFile.FileId;
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибки при чтении других файлов
                    continue;
                }
            }

            AnalysisReport report = AnalysisReport.Create(
                new FileId(request.FileId),
                isPlagiarized,
                new SimilarityPercentage(similarityPercentage)
            );

            string reportContent = GenerateReportContent(report, fileMetadata, plagiarizedFromFileId);
            string reportFilePath;
            try
            {
                reportFilePath = _reportStorage.SaveReport(reportContent, report.Id.Id);
            }
            catch (Exception ex)
            {
                throw new AnalysisException("Storage", "SAVE_REPORT_FILE_FAILED", $"Failed to save report file: {ex.Message}", ex);
            }

            report = AnalysisReport.Restore(
                report.Id,
                report.FileId,
                report.IsPlagiarized,
                report.SimilarityPercentage,
                report.AnalysisDate,
                reportFilePath
            );

            try
            {
                _reportRepository.Save(report);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(reportFilePath))
                {
                    _ = _reportStorage.TryDelete(reportFilePath);
                }
                throw new AnalysisException("Database", "SAVE_REPORT_FAILED", $"Failed to save analysis report: {ex.Message}", ex);
            }

            return new AnalyzeFileResponse
            {
                ReportId = report.Id.Id,
                FileId = request.FileId,
                IsPlagiarized = isPlagiarized,
                SimilarityPercentage = similarityPercentage,
                AnalysisDate = report.AnalysisDate.Date
            };
        }

        private void ValidateRequest(AnalyzeFileRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.FileId == Guid.Empty)
            {
                throw new AnalysisException("Validation", "INVALID_FILE_ID", "File ID is required");
            }
        }

        private string ExtractTextContent(byte[] fileData)
        {
            try
            {
                return Encoding.UTF8.GetString(fileData);
            }
            catch
            {
                try
                {
                    return Encoding.GetEncoding("windows-1251").GetString(fileData);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            {
                return 0.0;
            }

            string[] words1 = text1.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
            string[] words2 = text2.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);

            if (words1.Length == 0 || words2.Length == 0)
            {
                return 0.0;
            }

            List<string> normalizedWords1 = [.. words1.Select(w => w.ToLowerInvariant())];
            List<string> normalizedWords2 = [.. words2.Select(w => w.ToLowerInvariant())];

            int commonWords = normalizedWords1.Intersect(normalizedWords2).Count();
            int totalUniqueWords = normalizedWords1.Union(normalizedWords2).Count();

            if (totalUniqueWords == 0)
            {
                return 0.0;
            }

            double similarity = (double)commonWords / totalUniqueWords * 100.0;

            return Math.Min(100.0, Math.Max(0.0, similarity));
        }

        private string GenerateReportContent(AnalysisReport report, FileMetadata fileMetadata, Guid? plagiarizedFromFileId)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine($"Analysis Report ID: {report.Id.Id}");
            _ = sb.AppendLine($"File ID: {report.FileId.Id}");
            _ = sb.AppendLine($"File Name: {fileMetadata.FileName}");
            _ = sb.AppendLine($"Student: {fileMetadata.StudentName} {fileMetadata.StudentSurname}");
            _ = sb.AppendLine($"Group: {fileMetadata.GroupNumber}");
            _ = sb.AppendLine($"Assignment ID: {fileMetadata.AssignmentId}");
            _ = sb.AppendLine($"Analysis Date: {report.AnalysisDate.Date:yyyy-MM-dd HH:mm:ss}");
            _ = sb.AppendLine($"Is Plagiarized: {report.IsPlagiarized}");
            _ = sb.AppendLine($"Similarity Percentage: {report.SimilarityPercentage.Value:F2}%");
            if (plagiarizedFromFileId.HasValue)
            {
                _ = sb.AppendLine($"Plagiarized From File ID: {plagiarizedFromFileId.Value}");
            }
            _ = sb.AppendLine();
            _ = sb.AppendLine("--- End of Report ---");

            return sb.ToString();
        }
    }
}

