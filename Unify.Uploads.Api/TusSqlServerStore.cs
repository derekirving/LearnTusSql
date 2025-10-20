using System.Security.Cryptography;
using Dapper;
using Microsoft.Data.SqlClient;
using tusdotnet.Interfaces;
using tusdotnet.Models.Concatenation;
using Unify.Encryption;
using Unify.Web.Ui.Component.Upload;
using Unify.Web.Ui.Component.Upload.Models;

namespace Unify.Uploads.Api;

public class TusSqlServerStore : ITusStore,
    ITusCreationStore,
    ITusTerminationStore,
    ITusExpirationStore,
    ITusConcatenationStore,
    ITusReadableStore,
    ITusChecksumStore,
    ITusCreationDeferLengthStore
{
    private readonly IConfiguration _configuration;
    private readonly IUnifyEncryption _encryption;
    private readonly string _connectionString;
    private readonly string _uploadDirectory;
    
    public TusSqlServerStore(IConfiguration configuration, IUnifyEncryption encryption, string connectionString, string uploadDirectory)
    {
        _configuration = configuration;
        _encryption = encryption;
        _connectionString = connectionString;
        _uploadDirectory = uploadDirectory;

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }

        InitializeDatabase().GetAwaiter().GetResult();
    }

    private async Task InitializeDatabase()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
    
        const string sql = """

                                   IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TusFiles')
                                   BEGIN
                                       CREATE TABLE TusFiles (
                                           FileId NVARCHAR(50) PRIMARY KEY,
                                           FileName NVARCHAR(50) NOT NULL,
                                           UploadLength BIGINT NULL,
                                           UploadOffset BIGINT NOT NULL DEFAULT 0,
                                           Metadata NVARCHAR(MAX) NULL,
                                           CreatedAt DATETIME2 NOT NULL,
                                           ExpiresAt DATETIME2 NULL,
                                           UploadConcat NVARCHAR(20) NULL,
                                           PartialUploads NVARCHAR(MAX) NULL,
                                           UploadId NVARCHAR(50) NULL,
                                           ZoneId NVARCHAR(50) NULL,
                                           AppId NVARCHAR(50) NULL,
                                           IsCommitted BIT NOT NULL DEFAULT 0
                                       );

                                       CREATE INDEX IX_TusFiles_ExpiresAt ON TusFiles(ExpiresAt) WHERE ExpiresAt IS NOT NULL;
                                       CREATE INDEX IX_TusFiles_UploadId ON TusFiles(UploadId, IsCommitted) WHERE UploadId IS NOT NULL;
                                       CREATE INDEX IX_TusFiles_ZoneId ON TusFiles(ZoneId, IsCommitted) WHERE ZoneId IS NOT NULL;
                                       CREATE INDEX IX_TusFiles_AppId ON TusFiles(AppId) WHERE AppId IS NOT NULL;
                                       CREATE INDEX IX_TusFiles_Uncommitted ON TusFiles(CreatedAt) WHERE IsCommitted = 0;
                                   END
                                   ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TusFiles') AND name = 'AppId')
                                   BEGIN
                                       ALTER TABLE TusFiles ADD AppId NVARCHAR(50) NULL;
                                       CREATE INDEX IX_TusFiles_AppId ON TusFiles(AppId) WHERE AppId IS NOT NULL;
                                   END
                           """;
    
        await conn.ExecuteAsync(sql);
    }

    #region ITusCreationStore

    public async Task<string> CreateFileAsync(long uploadLength, string metadata, CancellationToken ct)
    {
        var secret = _configuration["Unify:Secret"];
        ArgumentException.ThrowIfNullOrEmpty(secret);
        
        var encryptedAppId = metadata.GetValue("appId");
        ArgumentException.ThrowIfNullOrEmpty(encryptedAppId);
        
        var appId = _encryption.Decrypt(encryptedAppId, secret);

        var zoneId = metadata.GetValue("zoneId");
        ArgumentException.ThrowIfNullOrEmpty(zoneId);
        
        var uploadId = metadata.GetValue("uploadId");
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        
        var fileName = metadata.GetValue("name");
        
        var fileId = Guid.NewGuid().ToString("N");
        var filePath = GetFilePath(fileId);
        // Create empty file (tus spec)
        await using (File.Create(filePath))
        {
            
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = @"
            INSERT INTO TusFiles (FileId, FileName, ZoneId, AppId, UploadId, UploadLength, UploadOffset, Metadata, CreatedAt)
            VALUES (@FileId, @FileName, @ZoneId, @AppId, @UploadId, @UploadLength, 0, @Metadata, @CreatedAt)";

        await conn.ExecuteAsync(sql, new
        {
            FileId = fileId,
            FileName = fileName,
            ZoneId = zoneId,
            AppId = appId,
            UploadId = uploadId,
            UploadLength = uploadLength,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        });

        return fileId;
    }

    public async Task<string> GetUploadMetadataAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = "SELECT Metadata FROM TusFiles WHERE FileId = @FileId";
        var result = await conn.QuerySingleOrDefaultAsync<string>(sql, new { FileId = fileId });

        return result ?? string.Empty;
    }

    #endregion

    #region ITusStore

    public async Task<bool> FileExistAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = "SELECT COUNT(*) FROM TusFiles WHERE FileId = @FileId";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { FileId = fileId });

        return count > 0 && File.Exists(GetFilePath(fileId));
    }

    public async Task<long?> GetUploadLengthAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = "SELECT UploadLength FROM TusFiles WHERE FileId = @FileId";
        return await conn.QuerySingleOrDefaultAsync<long?>(sql, new { FileId = fileId });
    }

    public async Task<long> GetUploadOffsetAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = "SELECT UploadOffset FROM TusFiles WHERE FileId = @FileId";
        var result = await conn.QuerySingleOrDefaultAsync<long?>(sql, new { FileId = fileId });

        return result ?? 0;
    }

    public async Task<long> AppendDataAsync(string fileId, Stream stream, CancellationToken ct)
    {
        var filePath = GetFilePath(fileId);
        var bytesWritten = 0L;

        await using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None))
        {
            var buffer = new byte[81920]; // 80 KB buffer
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                bytesWritten += bytesRead;
            }
        }

        // Update offset
        await using var conn = new SqlConnection(_connectionString);

        var sql = "UPDATE TusFiles SET UploadOffset = UploadOffset + @BytesWritten WHERE FileId = @FileId";
        await conn.ExecuteAsync(sql, new { BytesWritten = bytesWritten, FileId = fileId });

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

        await using var conn = new SqlConnection(_connectionString);

        var sql = "DELETE FROM TusFiles WHERE FileId = @FileId";
        await conn.ExecuteAsync(sql, new { FileId = fileId });
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
        await using var conn = new SqlConnection(_connectionString);

        var sql = "UPDATE TusFiles SET ExpiresAt = @ExpiresAt WHERE FileId = @FileId";
        await conn.ExecuteAsync(sql, new { ExpiresAt = expires.UtcDateTime, FileId = fileId });
    }

    public async Task<DateTimeOffset?> GetExpirationAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = "SELECT ExpiresAt FROM TusFiles WHERE FileId = @FileId";
        var result = await conn.QuerySingleOrDefaultAsync<DateTime?>(sql, new { FileId = fileId });

        return result.HasValue ? new DateTimeOffset(result.Value, TimeSpan.Zero) : null;
    }

    public async Task<IEnumerable<string>> GetExpiredFilesAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = @"
            SELECT FileId 
            FROM TusFiles 
            WHERE ExpiresAt IS NOT NULL AND ExpiresAt < @Now";

        return await conn.QueryAsync<string>(sql, new { Now = DateTime.UtcNow });
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
        await using var conn = new SqlConnection(_connectionString);

        var sql = "SELECT UploadConcat, PartialUploads FROM TusFiles WHERE FileId = @FileId";
        var result = await conn.QuerySingleOrDefaultAsync<(string UploadConcat, string PartialUploads)>(
            sql, new { FileId = fileId });

        if (result.UploadConcat == "partial")
            return new FileConcatPartial();

        if (!string.IsNullOrEmpty(result.PartialUploads))
        {
            var parts = result.PartialUploads.Split(',').ToArray();
            return new FileConcatFinal(parts);
        }

        return null;
    }

    public async Task<string> CreatePartialFileAsync(long uploadLength, string metadata, CancellationToken ct)
    {
        var fileId = await CreateFileAsync(uploadLength, metadata, ct);

        await using var conn = new SqlConnection(_connectionString);

        var sql = "UPDATE TusFiles SET UploadConcat = 'partial' WHERE FileId = @FileId";
        await conn.ExecuteAsync(sql, new { FileId = fileId });

        return fileId;
    }

    public async Task<string> CreateFinalFileAsync(string[] partialFiles, string metadata, CancellationToken ct)
    {
        var appId = metadata.GetValue("appId");
        ArgumentException.ThrowIfNullOrEmpty(appId);
        
        var zoneId = metadata.GetValue("zoneId");
        ArgumentException.ThrowIfNullOrEmpty(zoneId);
        
        var fileId = Guid.NewGuid().ToString("N");
        
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

        await using var conn = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO TusFiles (FileId, ZoneId, AppId, UploadLength, UploadOffset, Metadata, CreatedAt, PartialUploads)
            VALUES (@FileId, @ZoneId, @AppId, @UploadLength, @UploadOffset, @Metadata, @CreatedAt, @PartialUploads)";

        await conn.ExecuteAsync(sql, new
        {
            FileId = fileId,
            ZoneId = zoneId,
            AppId = appId,
            UploadLength = totalLength,
            UploadOffset = totalLength,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
            PartialUploads = string.Join(",", partialFiles)
        });

        return fileId;
    }

    #endregion

    #region ITusReadableStore

    public async Task<ITusFile?> GetFileAsync(string fileId, CancellationToken ct)
    {
        var exists = await FileExistAsync(fileId, ct);
        if (!exists)
            return null;

        return new TusSqlServerFile(this, fileId, _uploadDirectory);
    }

    #endregion

    #region ITusCreationDeferLengthStore

    public async Task SetUploadLengthAsync(string fileId, long uploadLength, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = "UPDATE TusFiles SET UploadLength = @UploadLength WHERE FileId = @FileId";
        await conn.ExecuteAsync(sql, new { UploadLength = uploadLength, FileId = fileId });
    }

    #endregion

    #region Session and Commit Management

    // public async Task AssociateFileWithSessionAsync(string fileId, string uploadId, CancellationToken ct)
    // {
    //     await using var conn = new SqlConnection(_connectionString);
    //
    //     var sql = "UPDATE TusFiles SET UploadId = @UploadId WHERE FileId = @FileId";
    //     await conn.ExecuteAsync(sql, new { UploadId = uploadId, FileId = fileId });
    // }
    //
    // public async Task SetAppIdAsync(string fileId, string appId, CancellationToken ct)
    // {
    //     await using var conn = new SqlConnection(_connectionString);
    //
    //     var sql = "UPDATE TusFiles SET AppId = @AppId WHERE FileId = @FileId";
    //     await conn.ExecuteAsync(sql, new { AppId = appId, FileId = fileId });
    // }

    public async Task CommitFileAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = "UPDATE TusFiles SET IsCommitted = 1 WHERE FileId = @FileId";
        await conn.ExecuteAsync(sql, new { FileId = fileId });
    }

    public async Task<List<UnifyUploadFile>> GetFilesBySessionAsync(string uploadId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);

        var sql = @"
        SELECT 
            FileId, 
            FileName, 
            ZoneId AS Zone, 
            CAST(UploadLength AS INT) AS Size
        FROM TusFiles 
        WHERE UploadId = @UploadId";

        var files = (await conn.QueryAsync(sql, new { UploadId = uploadId }))
            .Select(row => new UnifyUploadFile
            {
                FileId = row.FileId,
                FileName = row.FileName,
                Zone = row.Zone,
                Size = row.Size,
                Uri = new Uri($"https://domain.com/{row.FileId}")
            })
            .ToList();

        return files;
    }


    public async Task<int> CleanupUncommittedFilesAsync(TimeSpan olderThan, CancellationToken ct)
    {
        var cutoffDate = DateTime.UtcNow.Subtract(olderThan);

        await using var conn = new SqlConnection(_connectionString);

        var sql = @"
            SELECT FileId 
            FROM TusFiles 
            WHERE IsCommitted = 0 
            AND CreatedAt < @CutoffDate";

        var filesToDelete = await conn.QueryAsync<string>(sql, new { CutoffDate = cutoffDate });

        var count = 0;
        foreach (var fileId in filesToDelete)
        {
            await DeleteFileAsync(fileId, ct);
            count++;
        }

        return count;
    }
    
    public async Task<TusFileInfo?> GetFileInfoAsync(string fileId, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
    
        var sql = @"
        SELECT 
            FileId,
            UploadLength,
            UploadOffset,
            Metadata,
            CreatedAt,
            ExpiresAt,
            UploadId,
            AppId,
            IsCommitted
        FROM TusFiles 
        WHERE FileId = @FileId";
    
        var fi = await conn.QuerySingleOrDefaultAsync<TusFileInfo>(sql, new { FileId = fileId });
        if (fi == null) return null;
        
        fi.FileName = fi.Metadata.GetValue("name");
        
        return fi;
    }

    #endregion

    private string GetFilePath(string fileId)
    {
        return Path.Combine(_uploadDirectory, fileId);
    }

    // Internal file representation for ITusReadableStore
    private class TusSqlServerFile : ITusFile
    {
        private readonly TusSqlServerStore _store;
        private readonly string _fileId;
        private readonly string _uploadDirectory;

        public TusSqlServerFile(TusSqlServerStore store, string fileId, string uploadDirectory)
        {
            _store = store;
            _fileId = fileId;
            _uploadDirectory = uploadDirectory;
        }

        public string Id => _fileId;

        public Task<Stream> GetContentAsync(CancellationToken ct)
        {
            var filePath = Path.Combine(_uploadDirectory, _fileId);
            Stream stream = File.OpenRead(filePath);
            return Task.FromResult(stream);
        }

        public async Task<Dictionary<string, tusdotnet.Models.Metadata>> GetMetadataAsync(CancellationToken ct)
        {
            var metadataString = await _store.GetUploadMetadataAsync(_fileId, ct);

            if (string.IsNullOrWhiteSpace(metadataString))
                return new Dictionary<string, tusdotnet.Models.Metadata>();

            return tusdotnet.Models.Metadata.Parse(metadataString);
        }
    }
}