namespace Unify.Uploads.Api;

internal static partial class Log
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Debug,
        Message = "File {FileId} upload completed!")]
    internal static partial void FileUploadCompleted(ILogger logger, string fileId);
}