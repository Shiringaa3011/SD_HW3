using AnalysisService.Application.Ports;
using AnalysisService.Domain.Entities;
using AnalysisService.Domain.ValueObjects;
using Npgsql;
using System.Data;

namespace AnalysisService.Infrastructure.Persistence
{
    public class PostgreSqlAnalysisReportRepository(string connectionString) : IAnalysisReportRepository
    {
        private readonly string _connectionString = connectionString;

        public void Save(AnalysisReport report)
        {
            using NpgsqlConnection connection = new(_connectionString);

            try
            {
                connection.Open();

                using NpgsqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    string sql = @"
                    INSERT INTO analysis_reports 
                    (id, file_id, is_plagiarized, similarity_percentage, analysis_date, report_file_path)
                    VALUES 
                    (@id, @fileId, @isPlagiarized, @similarityPercentage, @analysisDate, @reportFilePath)
                    ON CONFLICT (id) DO UPDATE SET
                        is_plagiarized = @isPlagiarized,
                        similarity_percentage = @similarityPercentage,
                        analysis_date = @analysisDate,
                        report_file_path = @reportFilePath;
                ";

                    using NpgsqlCommand command = new(sql, connection, transaction);

                    _ = command.Parameters.AddWithValue("id", report.Id.Id);
                    _ = command.Parameters.AddWithValue("fileId", report.FileId.Id);
                    _ = command.Parameters.AddWithValue("isPlagiarized", report.IsPlagiarized);
                    _ = command.Parameters.AddWithValue("similarityPercentage", report.SimilarityPercentage.Value);
                    _ = command.Parameters.AddWithValue("analysisDate", report.AnalysisDate.Date);
                    _ = command.Parameters.AddWithValue("reportFilePath", (object?)report.ReportFilePath ?? DBNull.Value);

                    int rowsAffected = command.ExecuteNonQuery();
                    
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        throw new DataException($"Failed to save analysis report {report.Id.Id}: No rows affected");
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (NpgsqlException ex)
            {
                throw new DataException($"Database error while saving analysis report {report.Id.Id}", ex);
            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DataException($"Unexpected error while saving analysis report {report.Id.Id}", ex);
            }
        }

        public AnalysisReport? FindById(AnalysisReportId id)
        {
            using NpgsqlConnection connection = new(_connectionString);

            try
            {
                connection.Open();

                string sql = "SELECT * FROM analysis_reports WHERE id = @id";
                using NpgsqlCommand command = new(sql, connection);
                _ = command.Parameters.AddWithValue("id", id.Id);

                using NpgsqlDataReader reader = command.ExecuteReader();
                return reader.Read()
                    ? AnalysisReport.Restore(
                        new AnalysisReportId(reader.GetGuid("id")),
                        new FileId(reader.GetGuid("file_id")),
                        reader.GetBoolean("is_plagiarized"),
                        new SimilarityPercentage(reader.GetDouble("similarity_percentage")),
                        new AnalysisDate(reader.GetDateTime("analysis_date")),
                        reader.IsDBNull("report_file_path") ? null : reader.GetString("report_file_path")
                    )
                    : null;
            }
            catch (NpgsqlException ex)
            {
                throw new DataException($"Failed to find analysis report {id.Id}", ex);
            }
        }

        public AnalysisReport? FindByFileId(FileId fileId)
        {
            using NpgsqlConnection connection = new(_connectionString);

            try
            {
                connection.Open();

                string sql = "SELECT * FROM analysis_reports WHERE file_id = @fileId ORDER BY analysis_date DESC LIMIT 1";
                using NpgsqlCommand command = new(sql, connection);
                _ = command.Parameters.AddWithValue("fileId", fileId.Id);

                using NpgsqlDataReader reader = command.ExecuteReader();
                return reader.Read()
                    ? AnalysisReport.Restore(
                        new AnalysisReportId(reader.GetGuid("id")),
                        new FileId(reader.GetGuid("file_id")),
                        reader.GetBoolean("is_plagiarized"),
                        new SimilarityPercentage(reader.GetDouble("similarity_percentage")),
                        new AnalysisDate(reader.GetDateTime("analysis_date")),
                        reader.IsDBNull("report_file_path") ? null : reader.GetString("report_file_path")
                    )
                    : null;
            }
            catch (NpgsqlException ex)
            {
                throw new DataException($"Failed to find analysis report for file {fileId.Id}", ex);
            }
        }

        public List<AnalysisReport> FindByAssignmentId(Guid assignmentId)
        {
            using NpgsqlConnection connection = new(_connectionString);

            try
            {
                connection.Open();
                List<AnalysisReport> reports = [];

                string sql = @"
                    SELECT ar.* 
                    FROM analysis_reports ar
                    INNER JOIN file_uploads fu ON ar.file_id = fu.id
                    WHERE fu.assignment_id = @assignmentId
                    ORDER BY ar.analysis_date DESC
                ";

                using NpgsqlCommand command = new(sql, connection);
                _ = command.Parameters.AddWithValue("assignmentId", assignmentId);

                using NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    reports.Add(AnalysisReport.Restore(
                        new AnalysisReportId(reader.GetGuid("id")),
                        new FileId(reader.GetGuid("file_id")),
                        reader.GetBoolean("is_plagiarized"),
                        new SimilarityPercentage(reader.GetDouble("similarity_percentage")),
                        new AnalysisDate(reader.GetDateTime("analysis_date")),
                        reader.IsDBNull("report_file_path") ? null : reader.GetString("report_file_path")
                    ));
                }

                return reports;
            }
            catch (NpgsqlException ex)
            {
                throw new DataException($"Failed to find analysis reports for assignment {assignmentId}", ex);
            }
        }
    }
}

