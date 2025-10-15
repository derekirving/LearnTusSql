namespace Unify.Uploads.Api;

public static class EndPoints
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Minimal API endpoints

        endpoints.MapGet("/", async ctx => await ctx.Response.WriteAsync("Unify Uploads API at /api"));

// Get file info
        endpoints.MapGet("/api/files/{fileId}", async (
                string fileId,
                TusSqlServerStore store,
                CancellationToken ct) =>
            {
                var exists = await store.FileExistAsync(fileId, ct);
                if (!exists)
                    return Results.NotFound();

                var file = await store.GetFileInfoAsync(fileId, ct);
                return Results.Ok(file);
            })
            .WithName("GetFileInfo");

// Associate file with session
// endpoints.MapPost("/api/files/{fileId}/associate", async (
//         string fileId,
//         [FromBody] AssociateRequest request,
//         TusSqlServerStore store,
//         CancellationToken ct) =>
//     {
//         var exists = await store.FileExistAsync(fileId, ct);
//         if (!exists)
//             return Results.NotFound();
//
//         await store.AssociateFileWithSessionAsync(fileId, request.SessionId, ct);
//
//         if (!string.IsNullOrEmpty(request.AppId))
//         {
//             await store.SetAppIdAsync(fileId, request.AppId, ct);
//         }
//
//         return Results.Ok();
//     })
//     .WithName("AssociateFile");

// Commit file (mark as permanent)
        endpoints.MapPost("/api/files/{fileId}/commit", async (
                string fileId,
                TusSqlServerStore store,
                CancellationToken ct) =>
            {
                var exists = await store.FileExistAsync(fileId, ct);
                if (!exists)
                    return Results.NotFound();

                await store.CommitFileAsync(fileId, ct);
                return Results.Ok();
            })
            .WithName("CommitFile");

// Get files by session
        endpoints.MapGet("/api/sessions/{sessionId}/files", async (
                string sessionId,
                TusSqlServerStore store,
                CancellationToken ct) =>
            {
                var files = await store.GetFilesBySessionAsync(sessionId, ct);
                return Results.Ok(files);
            })
            .WithName("GetFilesBySession");

// Download file
        endpoints.MapGet("/api/files/{fileId}/download", async (
                string fileId,
                TusSqlServerStore store,
                CancellationToken ct) =>
            {
                var exists = await store.FileExistAsync(fileId, ct);
                if (!exists)
                    return Results.NotFound();

                var file = await store.GetFileAsync(fileId, ct);
                if(file == null)
                    return Results.NotFound();
                
                var stream = await file.GetContentAsync(ct);
                var metadata = await file.GetMetadataAsync(ct);

                var filename = "download";
                if (metadata.TryGetValue("filename", out var filenameMetadata))
                {
                    filename = filenameMetadata.GetString(System.Text.Encoding.UTF8);
                }

                var contentType = "application/octet-stream";
                if (metadata.TryGetValue("filetype", out var filetypeMetadata))
                {
                    contentType = filetypeMetadata.GetString(System.Text.Encoding.UTF8);
                }

                return Results.File(stream, contentType, filename);
            })
            .WithName("DownloadFile");

// Delete file
        endpoints.MapDelete("/api/files/{fileId}", async (
                string fileId,
                TusSqlServerStore store,
                CancellationToken ct) =>
            {
                var exists = await store.FileExistAsync(fileId, ct);
                if (!exists)
                    return Results.NotFound();

                await store.DeleteFileAsync(fileId, ct);
                return Results.NoContent();
            })
            .WithName("DeleteFile");

// Health check
        endpoints.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("HealthCheck");

        return endpoints;
    }
}