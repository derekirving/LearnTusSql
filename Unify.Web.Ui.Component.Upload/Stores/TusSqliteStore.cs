using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using tusdotnet.Interfaces;
using tusdotnet.Models.Concatenation;

namespace Unify.Web.Ui.Component.Upload.Stores;

public class TusSqliteStore : 
    ITusStore, 
    ITusCreationStore, 
    ITusReadableStore,
    ITusTerminationStore,
    ITusChecksumStore,
    ITusConcatenationStore,
    ITusExpirationStore,
    ITusCreationDeferLengthStore
{
    private readonly string _connectionString;
    private readonly string _uploadDirectory;

    public TusSqliteStore(string databasePath, string uploadDirectory)
    {
        _connectionString = $"Data Source={databasePath}";
        _uploadDirectory = uploadDirectory;

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS TusFiles (
                FileId TEXT PRIMARY KEY,
                UploadLength INTEGER,
                UploadOffset INTEGER,
                Metadata TEXT,
                CreatedAt TEXT,
                ExpiresAt TEXT,
                UploadConcat TEXT,
                PartialUploads TEXT
            )";
        cmd.ExecuteNonQuery();
    }

    #region ITusCreationStore

    public async Task<string> CreateFileAsync(long uploadLength, string metadata, CancellationToken ct)
    {
        var fileId = Guid.NewGuid().ToString("n");
        var filePath = GetFilePath(fileId);

        // Create empty file
        await using (File.Create(filePath))
        {
        }

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO TusFiles (FileId, UploadLength, UploadOffset, Metadata, CreatedAt)
            VALUES (@FileId, @UploadLength, 0, @Metadata, @CreatedAt)";
        cmd.Parameters.AddWithValue("@FileId", fileId);
        cmd.Parameters.AddWithValue("@UploadLength", uploadLength);
        cmd.Parameters.AddWithValue("@Metadata", metadata ?? string.Empty);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);

        return fileId;
    }

    public async Task<string> GetUploadMetadataAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Metadata FROM TusFiles WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@FileId", fileId);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString() ?? string.Empty;
    }

    #endregion

    #region ITusStore

    public async Task<bool> FileExistAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM TusFiles WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@FileId", fileId);

        var result = await cmd.ExecuteScalarAsync(ct);
        var count = result != null ? (long)result : 0L;
        return count > 0 && File.Exists(GetFilePath(fileId));
    }

    public async Task<long?> GetUploadLengthAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT UploadLength FROM TusFiles WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@FileId", fileId);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result == DBNull.Value ? null : (long?)result;
    }

    public async Task<long> GetUploadOffsetAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT UploadOffset FROM TusFiles WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@FileId", fileId);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result == null ? 0 : (long)result;
    }

    public async Task<long> AppendDataAsync(string fileId, Stream stream, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId);
        var bytesWritten = 0L;

        await using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None))
        {
            var buffer = new byte[81920]; // 80 KB buffer
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                bytesWritten += bytesRead;
            }
        }

        // Update offset
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE TusFiles SET UploadOffset = UploadOffset + @BytesWritten WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@BytesWritten", bytesWritten);
        cmd.Parameters.AddWithValue("@FileId", fileId);

        await cmd.ExecuteNonQueryAsync(ct);

        return bytesWritten;
    }

    #endregion

    #region ITusTerminationStore

    public async Task DeleteFileAsync(string fileId, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM TusFiles WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@FileId", fileId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    #endregion

    #region ITusChecksumStore

    public async Task<IEnumerable<string>> GetSupportedAlgorithmsAsync(CancellationToken ct)
    {
        return await Task.FromResult(new[] { "sha1", "sha256", "md5" });
    }

    public async Task<bool> VerifyChecksumAsync(string fileId, string algorithm, byte[] checksum, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId);

        if (!File.Exists(filePath))
            return false;

        await using var fileStream = File.OpenRead(filePath);

        byte[] fileHash;

        switch (algorithm.ToLowerInvariant())
        {
            case "sha1":
                using (var sha1 = SHA1.Create())
                {
                    fileHash = await Task.Run(() => sha1.ComputeHash(fileStream), ct);
                }

                break;

            case "sha256":
                using (var sha256 = SHA256.Create())
                {
                    fileHash = await Task.Run(() => sha256.ComputeHash(fileStream), ct);
                }

                break;

            case "md5":
                using (var md5 = MD5.Create())
                {
                    fileHash = await Task.Run(() => md5.ComputeHash(fileStream), ct);
                }

                break;

            default:
                return false;
        }

        return checksum.SequenceEqual(fileHash);
    }

    #endregion

    #region ITusExpirationStore

    public async Task SetExpirationAsync(string fileId, DateTimeOffset expires, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE TusFiles SET ExpiresAt = @ExpiresAt WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@ExpiresAt", expires.UtcDateTime.ToString("o"));
        cmd.Parameters.AddWithValue("@FileId", fileId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<DateTimeOffset?> GetExpirationAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ExpiresAt FROM TusFiles WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@FileId", fileId);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result == null || result == DBNull.Value)
            return null;

        var dateString = result.ToString();
        if (string.IsNullOrEmpty(dateString))
            return null;

        return DateTimeOffset.Parse(dateString);
    }

    public async Task<IEnumerable<string>> GetExpiredFilesAsync(CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT FileId FROM TusFiles WHERE ExpiresAt IS NOT NULL AND ExpiresAt < @Now";
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("o"));

        var files = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            files.Add(reader.GetString(0));
        }

        return files;
    }

    public async Task<int> RemoveExpiredFilesAsync(CancellationToken ct)
    {
        var expiredFiles = await GetExpiredFilesAsync(ct);
        var count = 0;

        foreach (var fileId in expiredFiles)
        {
            await DeleteFileAsync(fileId, ct);
            count++;
        }

        return count;
    }

    #endregion

    #region ITusConcatenationStore

    public async Task<FileConcat?> GetUploadConcatAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT UploadConcat, PartialUploads FROM TusFiles WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@FileId", fileId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var uploadConcat = reader.IsDBNull(0) ? null : reader.GetString(0);
            var partialUploads = reader.IsDBNull(1) ? null : reader.GetString(1);

            if (uploadConcat == "partial")
                return new FileConcatPartial();

            if (partialUploads != null)
            {
                var parts = partialUploads.Split(',').ToArray();
                return new FileConcatFinal(parts);
            }
        }

        return null;
    }

    public async Task<string> CreatePartialFileAsync(long uploadLength, string metadata, CancellationToken ct)
    {
        var fileId = await CreateFileAsync(uploadLength, metadata, ct);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE TusFiles SET UploadConcat = 'partial' WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@FileId", fileId);

        await cmd.ExecuteNonQueryAsync(ct);

        return fileId;
    }

    public async Task<string> CreateFinalFileAsync(string[] partialFiles, string metadata, CancellationToken ct)
    {
        var fileId = Guid.NewGuid().ToString("n");
        var filePath = GetFilePath(fileId);

        // Concatenate partial files
        await using (var finalStream = File.Create(filePath))
        {
            foreach (var partialFileId in partialFiles)
            {
                var partialPath = GetFilePath(partialFileId);
                if (File.Exists(partialPath))
                {
                    await using var partialStream = File.OpenRead(partialPath);
                    await partialStream.CopyToAsync(finalStream, ct);
                }
            }
        }

        var totalLength = new FileInfo(filePath).Length;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO TusFiles (FileId, UploadLength, UploadOffset, Metadata, CreatedAt, PartialUploads)
            VALUES (@FileId, @UploadLength, @UploadOffset, @Metadata, @CreatedAt, @PartialUploads)";
        cmd.Parameters.AddWithValue("@FileId", fileId);
        cmd.Parameters.AddWithValue("@UploadLength", totalLength);
        cmd.Parameters.AddWithValue("@UploadOffset", totalLength);
        cmd.Parameters.AddWithValue("@Metadata", metadata ?? string.Empty);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@PartialUploads", string.Join(",", partialFiles));

        await cmd.ExecuteNonQueryAsync(ct);

        return fileId;
    }

    #endregion

    #region ITusReadableStore

    public async Task<ITusFile?> GetFileAsync(string fileId, CancellationToken ct)
    {
        var exists = await FileExistAsync(fileId, ct);
        return !exists ? null : new TusSqliteFile(this, fileId, _uploadDirectory);
    }

    #endregion

    #region ITusCreationDeferLengthStore

    public async Task SetUploadLengthAsync(string fileId, long uploadLength, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE TusFiles SET UploadLength = @UploadLength WHERE FileId = @FileId";
        cmd.Parameters.AddWithValue("@UploadLength", uploadLength);
        cmd.Parameters.AddWithValue("@FileId", fileId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    #endregion

    private string GetFilePath(string fileId)
    {
        return Path.Combine(_uploadDirectory, fileId);
    }

    // Internal file representation for ITusReadableStore
    private class TusSqliteFile(TusSqliteStore store, string fileId, string uploadDirectory)
        : ITusFile
    {
        public string Id => fileId;

        public Task<Stream> GetContentAsync(CancellationToken ct)
        {
            var filePath = Path.Combine(uploadDirectory, fileId);
            Stream stream = File.OpenRead(filePath);
            return Task.FromResult(stream);
        }

        public async Task<Dictionary<string, tusdotnet.Models.Metadata>> GetMetadataAsync(CancellationToken ct)
        {
            var metadataString = await store.GetUploadMetadataAsync(fileId, ct);

            if (string.IsNullOrWhiteSpace(metadataString))
                return new Dictionary<string, tusdotnet.Models.Metadata>();

            // Use the built-in MetadataParser from tusdotnet
            return tusdotnet.Models.Metadata.Parse(metadataString);
        }
    }
}