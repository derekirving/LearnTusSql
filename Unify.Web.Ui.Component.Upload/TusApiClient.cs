using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Unify.Web.Ui.Component.Upload;

public class TusApiClient(HttpClient httpClient, IConfiguration configuration)
{
    private readonly string _appId = configuration["TusApi:AppId"] ?? "DefaultApp";

    public static string Version => "0.1.0";

    public async Task<bool> AssociateFileAsync(string fileId, string sessionId, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/files/{fileId}/associate",
            new { SessionId = sessionId, AppId = _appId },
            ct);

        return response.IsSuccessStatusCode;
    }

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

        return response ?? new List<string>();
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