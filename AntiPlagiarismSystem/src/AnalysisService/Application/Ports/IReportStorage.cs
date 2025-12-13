namespace AnalysisService.Application.Ports
{
    public interface IReportStorage
    {
        string SaveReport(string reportContent, Guid reportId);
        string? GetReport(string reportFilePath);
        bool TryDelete(string reportFilePath);
    }
}

