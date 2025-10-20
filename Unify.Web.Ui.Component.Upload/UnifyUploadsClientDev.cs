using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Models;

#if DEBUG
namespace Unify.Web.Ui.Component.Upload;

public class UnifyUploadsClientDev(HttpClient httpClient) : IUnifyUploadsClient
{
    public string Version { get; }
    public async Task<List<CommitedUploadResult>> CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<List<UnifyUploadFile>> GetFilesBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<FileInfoDto?> GetFileInfoAsync(string fileId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public string GetDownloadUrl(string fileId)
    {
        throw new NotImplementedException();
    }
}
#endif