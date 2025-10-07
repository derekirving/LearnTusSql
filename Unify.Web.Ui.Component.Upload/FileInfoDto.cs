namespace Unify.Web.Ui.Component.Upload;

public record FileInfoDto(
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