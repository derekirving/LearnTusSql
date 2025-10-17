namespace Unify.Web.Ui.Component.Upload;

public interface IUnifyUploadSession
{
    string UnifyUploadId { get; set; }
    List<UnifyUploadFile> UnifyUploads { get; set; }
}