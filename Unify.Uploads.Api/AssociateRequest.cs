namespace Unify.Uploads.Api;

public record AssociateRequest(string SessionId, string? AppId = null);