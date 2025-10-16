namespace Unify.Web.Ui.Component.Upload;

public class UnifyUploadFile
{
    public required string FileId { get; set; }
    public required string FileName { get; set; }
    public required string Zone { get; set; }
    public int Size { get; set; }
    public required Uri Uri { get; set; }
}