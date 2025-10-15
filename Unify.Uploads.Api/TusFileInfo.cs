namespace Unify.Uploads.Api;

public record TusFileInfo(
    string FileId,
    string FileName,
    long? UploadLength,
    long UploadOffset,
    string Metadata,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string SessionId,
    string AppId,
    bool IsCommitted
);