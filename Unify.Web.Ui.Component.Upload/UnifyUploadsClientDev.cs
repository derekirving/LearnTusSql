#if DEBUG

using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Models;
using Unify.Web.Ui.Component.Upload.Stores;

namespace Unify.Web.Ui.Component.Upload;

public class UnifyUploadsClientDev(SharedServerStore store) : IUnifyUploadsClient
{
    public string Version => "DEVELOPMENT";
    public async Task CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken ct = default)
    {
        var tasks = fileIds.Select(async item =>
        {
            await store.CommitFileAsync(item.FileId, ct);
        });
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task<List<UnifyUploadFile>> GetFilesBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        return  await store.GetFilesBySessionAsync(sessionId, ct);
    }

    public async Task<TusFileInfo?> GetFileInfoAsync(string fileId, CancellationToken ct = default)
    {
        return await store.GetFileInfoAsync(fileId, ct);
    }

    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadFileAsync(string fileId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
#endif