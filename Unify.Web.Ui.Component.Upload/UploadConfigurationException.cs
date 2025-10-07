using tusdotnet.Models;

namespace Unify.Web.Ui.Component.Upload;

public class UploadException(string message) : TusConfigurationException(message);