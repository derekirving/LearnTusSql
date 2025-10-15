using Microsoft.Extensions.Configuration;

namespace Unify.Web.Ui.Component.Upload;

public sealed class UnifyUploadService(IConfiguration configuration)
{
    public int GetMinimumFiles(string zoneId)
    {
        return configuration.GetValue<int>($"Unify:Uploads:Zones:{zoneId}:MinFiles");
    }
}