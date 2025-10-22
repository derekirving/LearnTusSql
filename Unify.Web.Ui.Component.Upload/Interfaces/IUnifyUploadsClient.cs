using Unify.Web.Ui.Component.Upload.Models;

namespace Unify.Web.Ui.Component.Upload.Interfaces;

public interface IUnifyUploadsClient
{
    string Version { get; }
    Task CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken ct = default);
    Task<List<UnifyUploadFile>> GetFilesBySessionAsync(string sessionId, CancellationToken ct = default);
    Task<FileInfoDto?> GetFileInfoAsync(string fileId, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(string fileId, CancellationToken ct = default);
    string GetDownloadUrl(string fileId);
}