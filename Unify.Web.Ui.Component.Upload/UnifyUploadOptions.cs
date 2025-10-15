namespace Unify.Web.Ui.Component.Upload;

public sealed class UnifyUploadOptions
{
    public required string BaseUrl { get; set; } = string.Empty;
    public string EncryptedAppId { get; set; } = string.Empty;
}