using Microsoft.AspNetCore.Authorization;
using Unify.Web.Ui.Component.Upload.Constants;

namespace Unify.Uploads.Api.Authentication;

public class ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the endpoint allows anonymous access
        var endpoint = context.GetEndpoint();
        var path = context.Request.Path.Value ?? string.Empty;
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null ||
            !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            // Skip authentication for endpoints with AllowAnonymous or non-/api paths
            await next(context);
            return;
        }

        var apiKey = configuration.GetValue<string>(UploadConstants.ApiKeySectionName) ?? "";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("API Key not configured.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(UploadConstants.UnifyAppIdHeaderName, out _))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unify App Id missing.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(UploadConstants.ApiKeyHeaderName, out var extractedKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key missing.");
            return;
        }

        if (apiKey != extractedKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key invalid.");
            return;
        }

        await next(context);
    }
}