using Microsoft.Extensions.Logging;

namespace Unify.Web.Ui.Component.Upload;

internal static partial class Log
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Debug,
        Message = "File {FileId} upload completed!")]
    internal static partial void FileUploadCompleted(ILogger logger, string fileId);
}