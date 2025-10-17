namespace Unify.Web.Ui.Component.Upload;

public class UnifyUploadSession
{
    public UnifyUploadSession() { } // Needed for model binding

    public UnifyUploadSession(IUnifyUploads unifyUploads)
    {
        _unifyUploads = unifyUploads;
    }

    private readonly IUnifyUploads? _unifyUploads;
    private string? _id;

    public string Id
    {
        get => _id ??= _unifyUploads?.GenerateUploadId() ?? string.Empty;
        set => _id = value;
    }

    public List<UnifyUploadFile> Files { get; set; } = [];
}
