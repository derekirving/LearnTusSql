using Unify.Web.Ui.Component.Upload.Models;

namespace Unify.Web.Ui.Component.Upload.Interfaces;

public interface IUnifyUploads
{
    string ClientVersion();
    string GenerateUploadId();
    int GetMinimumFiles(string zoneId);
    int GetMaximumFiles(string zoneId);
    int GetMaximumFileSize(string zoneId);
    List<string> GetAcceptedFileTypes(string zoneId);
    Task<UnifyUploadSession> GetSessionAsync(string uploadId, CancellationToken cancellationToken = default);
    Task CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken cancellationToken = default);
    Task<List<UnifyUploadFile>> GetFilesBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadFileAsync(string fileId, CancellationToken ct = default);
    Task<bool> DeleteUpload(string fileId, CancellationToken cancellationToken = default);
}