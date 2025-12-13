using FileService.Application.Ports;
using FileService.Domain.Entities;
using FileService.Domain.ValueObjects;
using Npgsql;
using System.Data;

namespace FileService.Infrastructure.Persistence
{
    public class PostgreSqlFileMetadataRepository(string connectionString) : IFileMetadataRepository
    {
        private readonly string _connectionString = connectionString;

        public void Save(FileUpload fileUpload)
        {
            // Используем using для автоматического закрытия соединения
            using NpgsqlConnection connection = new(_connectionString);

            try
            {
                connection.Open();

                // Используем транзакцию для обеспечения целостности данных
                using NpgsqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    string sql = @"
                    INSERT INTO file_uploads 
                    (id, file_name, file_size, upload_date, student_name, student_surname, group_number, assignment_id)
                    VALUES 
                    (@id, @fileName, @fileSize, @uploadDate, @studentName, @studentSurname, @groupNumber, @assignmentId)
                    ON CONFLICT (id) DO UPDATE SET
                        file_name = @fileName,
                        file_size = @fileSize,
                        upload_date = @uploadDate;
                ";
                    using NpgsqlCommand command = new(sql, connection, transaction);

                    _ = command.Parameters.AddWithValue("id", fileUpload.Id.Id);
                    _ = command.Parameters.AddWithValue("fileName", fileUpload.Name.Value);
                    _ = command.Parameters.AddWithValue("fileSize", fileUpload.Size.Bytes);
                    _ = command.Parameters.AddWithValue("uploadDate", fileUpload.Date.Date);
                    _ = command.Parameters.AddWithValue("studentName", fileUpload.Uploader.Name);
                    _ = command.Parameters.AddWithValue("studentSurname", fileUpload.Uploader.Surname);
                    _ = command.Parameters.AddWithValue("groupNumber", fileUpload.Uploader.GroupNumber);
                    _ = command.Parameters.AddWithValue("assignmentId", fileUpload.AssignmentId.Id);

                    int rowsAffected = command.ExecuteNonQuery();
                    
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        throw new DataException($"Failed to save file {fileUpload.Id.Id}: No rows affected");
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
                throw new DataException($"Database error while saving file {fileUpload.Id.Id}", ex);
            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DataException($"Unexpected error while saving file {fileUpload.Id.Id}", ex);
            }
        }

        public FileUpload? FindById(FileUploadId id)
        {
            using NpgsqlConnection connection = new(_connectionString);

            try
            {
                connection.Open();

                string sql = "SELECT * FROM file_uploads WHERE id = @id";
                using NpgsqlCommand command = new(sql, connection);
                _ = command.Parameters.AddWithValue("id", id.Id);

                using NpgsqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    // Преобразуем данные из БД в доменный объект
                    return FileUpload.Restore(
                        new FileUploadId(reader.GetGuid("id")),
                        new FileName(reader.GetString("file_name")),
                        new FileSize(reader.GetInt64("file_size")),
                        new UploadDate(reader.GetDateTime("upload_date")),
                        new Uploader(
                            reader.GetString("student_name"),
                            reader.GetString("student_surname"),
                            reader.GetInt32("group_number")
                        ),
                        new AssignmentId(reader.GetGuid("assignment_id"))
                    );
                }

                return null;
            }
            catch (NpgsqlException ex)
            {
                throw new DataException($"Failed to find file {id.Id}", ex);
            }
        }

        public List<FileUpload> FindByAssignmentId(AssignmentId assignmentId)
        {
            using NpgsqlConnection connection = new(_connectionString);
            try
            {
                connection.Open();
                List<FileUpload> files = [];


                string sql = "SELECT * FROM file_uploads WHERE assignment_id = @assignmentId ORDER BY upload_date DESC";
                using NpgsqlCommand command = new(sql, connection);
                _ = command.Parameters.AddWithValue("assignmentId", assignmentId.Id);

                using NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    files.Add(FileUpload.Restore(
                        new FileUploadId(reader.GetGuid("id")),
                        new FileName(reader.GetString("file_name")),
                        new FileSize(reader.GetInt64("file_size")),
                        new UploadDate(reader.GetDateTime("upload_date")),
                        new Uploader(
                            reader.GetString("student_name"),
                            reader.GetString("student_surname"),
                            reader.GetInt32("group_number")
                        ),
                        new AssignmentId(reader.GetGuid("assignment_id"))
                    ));
                }

                return files;
            }
            catch (NpgsqlException ex)
            {
                throw new DataException($"Failed to find files for assignment {assignmentId}", ex);
            }
        }

        public bool Exists(FileUploadId id)
        {
            using NpgsqlConnection connection = new(_connectionString);
            try
            {
                connection.Open();

                const string sql = "SELECT COUNT(*) FROM file_uploads WHERE id = @id";
                using NpgsqlCommand command = new(sql, connection);
                _ = command.Parameters.AddWithValue("id", id.Id);

                long count = (long)(command.ExecuteScalar() ?? 0);
                return count > 0;
            }
            catch (NpgsqlException ex)
            {
                throw new DataException($"Failed to check file existence {id.Id}", ex);
            }
        }
    }
}
