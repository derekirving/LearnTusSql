#if DEBUG
using Microsoft.Extensions.Configuration;
using Unify.Web.Ui.Component.Upload.Interfaces;
using Unify.Web.Ui.Component.Upload.Models;

namespace Unify.Web.Ui.Component.Upload;

public class UnifyUploadsDev(IConfiguration configuration) : IUnifyUploads
{
    private const string SecName = "Unify:Uploads:Zones:";
    
    public string ClientVersion()
    {
        return "DEV";
    }

    public string GenerateUploadId()
    {
        return Guid.NewGuid().ToString("n");
    }

    public int GetMinimumFiles(string zoneId)
    {
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

    public async Task<UnifyUploadSession> GetSessionAsync(string uploadId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<List<CommitedUploadResult>> CommitFilesAsync(List<UnifyUploadFile> fileIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<List<UnifyUploadFile>> GetFilesBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteUpload(string fileId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
#endif