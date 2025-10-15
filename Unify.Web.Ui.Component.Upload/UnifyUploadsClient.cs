using System.Net.Http.Json;

namespace Unify.Web.Ui.Component.Upload;

public class UnifyUploadsClient(HttpClient httpClient)
{
    public string Version => "0.1.0";

    // public async Task<bool> AssociateFileAsync(string fileId, string sessionId, string appId, CancellationToken ct = default)
    // {
    //     var response = await httpClient.PostAsJsonAsync(
    //         $"/api/files/{fileId}/associate",
    //         new { SessionId = sessionId, AppId = appId },
    //         ct);
    //
    //     return response.IsSuccessStatusCode;
    // }

    public async Task<List<CommitedUploadResult>> CommitFilesAsync(List<string> fileIds, CancellationToken ct = default)
    {
        var list = new  List<CommitedUploadResult>();
        
        foreach (var fileId in fileIds)
        {
            var response = await httpClient.PostAsync(
                $"/api/files/{fileId}/commit",
                null,
                ct);
            
            list.Add(new CommitedUploadResult(fileId, response.IsSuccessStatusCode, response.ReasonPhrase));
            
        }

        return list;
    }

    public async Task<List<string>> GetFilesBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<string>>(
            $"/api/sessions/{sessionId}/files",
            ct);

        return response ?? [];
    }

    public async Task<FileInfoDto?> GetFileInfoAsync(string fileId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<FileInfoDto>(
            $"/api/files/{fileId}",
            ct);
    }

    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync(
            $"/api/files/{fileId}",
            ct);

        return response.IsSuccessStatusCode;
    }

    public string GetDownloadUrl(string fileId)
    {
        return $"{httpClient.BaseAddress}api/files/{fileId}/download";
    }
}