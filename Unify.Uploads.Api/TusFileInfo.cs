namespace Unify.Uploads.Api;

public record TusFileInfo(
    string FileId,
    long? UploadLength,
    long UploadOffset,
    string Metadata,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string SessionId,
    string AppId,
    bool IsCommitted
);