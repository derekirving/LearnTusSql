using Microsoft.Extensions.Configuration;

namespace Unify.Web.Ui.Component.Upload;

public interface IUnifyUploads
{
    string ClientVersion();
    string GenerateFormSessionId();
    int GetMinimumFiles(string zoneId);
    int GetMaximumFiles(string zoneId);
    int GetMaximumFileSize(string zoneId);
    List<string> GetAcceptedFileTypes(string zoneId);
    Task<List<CommitedUploadResult>> CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken ct = default);
}

public sealed class UnifyUploads(IConfiguration configuration, UnifyUploadsClient client) : IUnifyUploads
{
    private const string SecName = "Unify:Uploads:Zones:";

    public string ClientVersion()
    {
        return client.Version;
    }

    public string GenerateFormSessionId()
    {
        return Guid.NewGuid().ToString("n");
    }
    
    public int GetMinimumFiles(string zoneId)
    {
        var v = client.Version;
        return configuration.GetValue<int>($"{SecName}{zoneId}:MinFiles");
    }
    
    public int GetMaximumFiles(string zoneId)
    {
        return configuration.GetValue<int>($"{SecName}{zoneId}:MaxFiles");
    }
    
    public int GetMaximumFileSize(string zoneId)
    {
        return configuration.GetValue<int>($"{SecName}{zoneId}:MaxSize");
    }
    
    public List<string> GetAcceptedFileTypes(string zoneId)
    {
        var accepted = configuration.GetValue<string>($"{SecName}{zoneId}:Accepted");
        return accepted?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList() ?? [];
    }

    public async Task<List<CommitedUploadResult>> CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken ct = default)
    {
        return await client.CommitFilesAsync(fileIds, ct);
    }
}