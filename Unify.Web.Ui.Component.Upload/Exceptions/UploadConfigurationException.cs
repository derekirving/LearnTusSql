using tusdotnet.Models;

namespace Unify.Web.Ui.Component.Upload.Exceptions;

public class UploadException(string message) : TusConfigurationException(message);