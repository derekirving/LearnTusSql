namespace Unify.Web.Ui.Component.Upload.Models;
public class TusFileInfo
{
    public required string FileId { get; init; }
    public string FileName { get; set; } = string.Empty;
    public long? UploadLength { get; init; }
    public long UploadOffset { get; init; } 
    public required string Metadata { get; init; }
    public DateTime CreatedAt { get; init; } 
    public DateTime? ExpiresAt { get; init; } 
    public required string SessionId { get; init; } 
    public required string AppId { get; init; } 
    public bool IsCommitted { get; init; }
}