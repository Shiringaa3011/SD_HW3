using AnalysisService.Application.Ports;

namespace AnalysisService.Infrastructure.Storage
{
    public class LocalReportStorage : IReportStorage
    {
        private readonly string _storagePath;

        public LocalReportStorage(string storagePath)
        {
            _storagePath = storagePath;
            if (!Directory.Exists(_storagePath))
            {
                _ = Directory.CreateDirectory(_storagePath);
            }
        }

        public string SaveReport(string reportContent, Guid reportId)
        {
            if (!Directory.Exists(_storagePath))
            {
                throw new DirectoryNotFoundException("Directory not found");
            }
            string fileName = $"report_{reportId}.txt";
            string filePath = Path.Combine(_storagePath, fileName);

            File.WriteAllText(filePath, reportContent, System.Text.Encoding.UTF8);

            return filePath;
        }

        public string? GetReport(string reportFilePath)
        {
            return !File.Exists(reportFilePath) ? null : File.ReadAllText(reportFilePath, System.Text.Encoding.UTF8);
        }

        public bool TryDelete(string reportFilePath)
        {
            try
            {
                if (File.Exists(reportFilePath))
                {
                    File.Delete(reportFilePath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

