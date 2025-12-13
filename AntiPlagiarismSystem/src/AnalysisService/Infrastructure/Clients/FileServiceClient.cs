using AnalysisService.Application.Ports;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnalysisService.Infrastructure.Clients
{
    public class FileServiceClient(string baseUrl) : IFileServiceClient
    {
        private readonly string _baseUrl = baseUrl.TrimEnd('/');
        private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(5) };
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public FileMetadata? GetFileMetadata(Guid fileId)
        {
            string url = $"{_baseUrl}/api/files/{fileId}/metadata";
            try
            {
                HttpRequestMessage request = new(HttpMethod.Get, url);

                HttpResponseMessage response = _httpClient.Send(request);

                if (!response.IsSuccessStatusCode)
                {
                    string error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return null;
                }

                string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                using Stream responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                GetFileMetadataResponseDto? responseDto = JsonSerializer.Deserialize<GetFileMetadataResponseDto>(responseStream, _jsonOptions);

                if (responseDto == null || !responseDto.FileExists || responseDto.Metadata == null)
                {
                    return null;
                }

                FileMetadataDto metadata = responseDto.Metadata;
                return new FileMetadata
                {
                    FileId = metadata.FileId,
                    FileName = metadata.FileName ?? string.Empty,
                    FileSize = metadata.FileSize,
                    UploadDate = metadata.UploadDate,
                    StudentName = metadata.StudentName ?? string.Empty,
                    StudentSurname = metadata.StudentSurname ?? string.Empty,
                    GroupNumber = metadata.GroupNumber,
                    AssignmentId = metadata.AssignmentId
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public byte[] GetFileData(Guid fileId)
        {
            try
            {
                string url = $"{_baseUrl}/api/files/{fileId}";
                HttpRequestMessage request = new(HttpMethod.Get, url);
                HttpResponseMessage response = _httpClient.Send(request);

                return !response.IsSuccessStatusCode
                    ? throw new HttpRequestException($"Failed to get file {fileId}: {response.StatusCode}")
                    : response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error getting file data for {fileId}", ex);
            }
        }

        public List<AssignmentFileInfo> GetAssignmentFiles(Guid assignmentId)
        {
            try
            {
                string url = $"{_baseUrl}/api/files/assignment/{assignmentId}";
                HttpRequestMessage request = new(HttpMethod.Get, url);
                HttpResponseMessage response = _httpClient.Send(request);

                if (!response.IsSuccessStatusCode)
                {
                    return [];
                }

                using Stream responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                List<AssignmentFileInfoDto>? files = JsonSerializer.Deserialize<List<AssignmentFileInfoDto>>(responseStream, _jsonOptions);

                return files == null
                    ? []
                    : [.. files.Select(f => new AssignmentFileInfo
                {
                    FileId = f.FileId,
                    FileName = f.FileName ?? string.Empty,
                    UploadDate = f.UploadDate,
                    StudentName = f.StudentName ?? string.Empty,
                    StudentSurname = f.StudentSurname ?? string.Empty,
                    GroupNumber = f.GroupNumber
                })];
            }
            catch
            {
                return [];
            }
        }

        private class GetFileMetadataResponseDto
        {
            [JsonPropertyName("metadata")]
            public FileMetadataDto? Metadata { get; set; }

            [JsonPropertyName("fileUrl")]
            public string? FileUrl { get; set; }

            [JsonPropertyName("fileExists")]
            public bool FileExists { get; set; }
        }

        private class FileMetadataDto
        {
            [JsonPropertyName("fileId")]
            public Guid FileId { get; set; }

            [JsonPropertyName("fileName")]
            public string? FileName { get; set; }

            [JsonPropertyName("fileSize")]
            public long FileSize { get; set; }

            [JsonPropertyName("uploadDate")]
            public DateTime UploadDate { get; set; }

            [JsonPropertyName("studentName")]
            public string? StudentName { get; set; }

            [JsonPropertyName("studentSurname")]
            public string? StudentSurname { get; set; }

            [JsonPropertyName("groupNumber")]
            public int GroupNumber { get; set; }

            [JsonPropertyName("assignmentId")]
            public Guid AssignmentId { get; set; }
        }

        private class AssignmentFileInfoDto
        {
            [JsonPropertyName("fileId")]
            public Guid FileId { get; set; }

            [JsonPropertyName("fileName")]
            public string? FileName { get; set; }

            [JsonPropertyName("uploadDate")]
            public DateTime UploadDate { get; set; }

            [JsonPropertyName("studentName")]
            public string? StudentName { get; set; }

            [JsonPropertyName("studentSurname")]
            public string? StudentSurname { get; set; }

            [JsonPropertyName("groupNumber")]
            public int GroupNumber { get; set; }
        }
    }
}

