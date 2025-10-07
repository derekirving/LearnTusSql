namespace Unify.Web.Ui.Component.Upload.Stores;

public class TusDiskStorageOptionHelper
{
    public string StorageDiskPath { get; }

    public TusDiskStorageOptionHelper()
    {
        var path = Path.Combine(Environment.CurrentDirectory, "App_Data", "tusfiles-sqlite");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        StorageDiskPath = path;
    }
}