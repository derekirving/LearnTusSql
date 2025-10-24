#if RELEASE
using System.Net.Http.Json;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Models;

namespace Unify.Web.Ui.Component.Upload;

public class UnifyUploadsClient(HttpClient httpClient) : IUnifyUploadsClient
{
    public string Version => "PRODUCTION";

    // public async Task<bool> AssociateFileAsync(string fileId, string sessionId, string appId, CancellationToken ct = default)
    // {
    //     var response = await httpClient.PostAsJsonAsync(
    //         $"/api/files/{fileId}/associate",
    //         new { SessionId = sessionId, AppId = appId },
    //         ct);
    //
    //     return response.IsSuccessStatusCode;
    // }

    public async Task CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken ct = default)
    {
        var tasks = fileIds.Select(async item =>
        {
            var response = await httpClient.PostAsync(
                $"/api/files/{item.FileId}/commit",
                null,
                ct);

            return new CommitedUploadResult(item.FileId, response.IsSuccessStatusCode, response.ReasonPhrase);
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    
    public async Task<List<UnifyUploadFile>> GetFilesBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<UnifyUploadFile>>(
            $"/api/sessions/{sessionId}/files",
            ct);

        return response ?? [];
    }

    public async Task<TusFileInfo?> GetFileInfoAsync(string fileId, CancellationToken ct = default)
    {
        var dto = await httpClient.GetFromJsonAsync<TusFileInfo>(
            $"/api/files/{fileId}",
            ct);

        return dto;
    }

    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync(
            $"/api/files/{fileId}",
            ct);

        return response.IsSuccessStatusCode;
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadFileAsync(string fileId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/files/{fileId}/download", ct);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(ct);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileName ?? "download";
        // ContentDispositionHeaderValue may include surrounding double quotes, as per RFC standards
        fileName = fileName.Trim('"');
        return (stream, contentType, fileName);
    }
}
#endif