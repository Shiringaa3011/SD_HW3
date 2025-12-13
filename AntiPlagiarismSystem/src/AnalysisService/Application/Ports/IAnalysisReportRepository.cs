using AnalysisService.Domain.Entities;
using AnalysisService.Domain.ValueObjects;

namespace AnalysisService.Application.Ports
{
    public interface IAnalysisReportRepository
    {
        void Save(AnalysisReport report);
        AnalysisReport? FindById(AnalysisReportId id);
        AnalysisReport? FindByFileId(FileId fileId);
        List<AnalysisReport> FindByAssignmentId(Guid assignmentId);
    }
}

