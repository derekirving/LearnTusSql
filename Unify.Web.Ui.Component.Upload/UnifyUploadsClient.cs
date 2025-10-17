using System.Net.Http.Json;

namespace Unify.Web.Ui.Component.Upload;

public class UnifyUploadsClient(HttpClient httpClient)
{
    internal string Version => "0.1.0";

    // public async Task<bool> AssociateFileAsync(string fileId, string sessionId, string appId, CancellationToken ct = default)
    // {
    //     var response = await httpClient.PostAsJsonAsync(
    //         $"/api/files/{fileId}/associate",
    //         new { SessionId = sessionId, AppId = appId },
    //         ct);
    //
    //     return response.IsSuccessStatusCode;
    // }

    internal async Task<List<CommitedUploadResult>> CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken ct = default)
    {
        var tasks = fileIds.Select(async item =>
        {
            var response = await httpClient.PostAsync(
                $"/api/files/{item.FileId}/commit",
                null,
                ct);

            return new CommitedUploadResult(item.FileId, response.IsSuccessStatusCode, response.ReasonPhrase);
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.ToList();
    }



    internal async Task<List<UnifyUploadFile>> GetFilesBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<UnifyUploadFile>>(
            $"/api/sessions/{sessionId}/files",
            ct);

        return response ?? [];
    }

    internal async Task<FileInfoDto?> GetFileInfoAsync(string fileId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<FileInfoDto>(
            $"/api/files/{fileId}",
            ct);
    }

    internal async Task<bool> DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync(
            $"/api/files/{fileId}",
            ct);

        return response.IsSuccessStatusCode;
    }

    internal string GetDownloadUrl(string fileId)
    {
        return $"{httpClient.BaseAddress}api/files/{fileId}/download";
    }
}