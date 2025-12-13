using AnalysisService.Domain.ValueObjects;

namespace AnalysisService.Domain.Entities
{
    public class AnalysisReport
    {
        public AnalysisReportId Id { get; }
        public FileId FileId { get; }
        public bool IsPlagiarized { get; }
        public SimilarityPercentage SimilarityPercentage { get; }
        public AnalysisDate AnalysisDate { get; }
        public string? ReportFilePath { get; }

        private AnalysisReport(
            AnalysisReportId id,
            FileId fileId,
            bool isPlagiarized,
            SimilarityPercentage similarityPercentage,
            AnalysisDate analysisDate,
            string? reportFilePath)
        {
            Id = id;
            FileId = fileId;
            IsPlagiarized = isPlagiarized;
            SimilarityPercentage = similarityPercentage;
            AnalysisDate = analysisDate;
            ReportFilePath = reportFilePath;
        }

        public static AnalysisReport Create(
            FileId fileId,
            bool isPlagiarized,
            SimilarityPercentage similarityPercentage,
            string? reportFilePath = null)
        {
            return new AnalysisReport(
                AnalysisReportId.New(),
                fileId,
                isPlagiarized,
                similarityPercentage,
                AnalysisDate.Now(),
                reportFilePath);
        }

        public static AnalysisReport Restore(
            AnalysisReportId id,
            FileId fileId,
            bool isPlagiarized,
            SimilarityPercentage similarityPercentage,
            AnalysisDate analysisDate,
            string? reportFilePath = null)
        {
            return new AnalysisReport(
                id,
                fileId,
                isPlagiarized,
                similarityPercentage,
                analysisDate,
                reportFilePath);
        }
    }
}

