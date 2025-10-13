using Microsoft.AspNetCore.Mvc;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using Unify.Uploads.Api;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("TusSettings:AllowedOrigins").Get<string[]>();
if (allowedOrigins == null)
{
    throw new Exception("No allowed origins are specified");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClientApps", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Upload-Offset", "Upload-Length", "Tus-Resumable", "Location");
    });
});

builder.Services.AddSingleton<TusSqlServerStore>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var connectionString = configuration.GetConnectionString("TusDatabase");
    ArgumentException.ThrowIfNullOrEmpty(connectionString);

    var uploadsDirectory = configuration["TusSettings:UploadDirectory"];
    ArgumentException.ThrowIfNullOrEmpty(uploadsDirectory);

    return new TusSqlServerStore(connectionString, uploadsDirectory);
});

//builder.Services.AddHostedService<TusCleanupService>();

var app = builder.Build();

app.UseCors("AllowClientApps");

app.UseTus(httpContext => new DefaultTusConfiguration
{
    Store = httpContext.RequestServices.GetRequiredService<TusSqlServerStore>(),
    UrlPath = "/files",
    Events = new Events
    {
        OnFileCompleteAsync = async eventContext =>
        {
            var file = await eventContext.GetFileAsync();
            var metadata = await file.GetMetadataAsync(eventContext.CancellationToken);

            // Extract session ID and app ID from metadata
            if (metadata.TryGetValue("sessionId", out var sessionIdMetadata))
            {
                var sessionId = sessionIdMetadata.GetString(System.Text.Encoding.UTF8);
                var store = eventContext.Store as TusSqlServerStore;
                await store.AssociateFileWithSessionAsync(file.Id, sessionId, eventContext.CancellationToken);
            }

            if (metadata.TryGetValue("appId", out var appIdMetadata))
            {
                var appId = appIdMetadata.GetString(System.Text.Encoding.UTF8);
                var store = eventContext.Store as TusSqlServerStore;
                await store.SetAppIdAsync(file.Id, appId, eventContext.CancellationToken);
            }

            var logger = eventContext.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"File {file.Id} upload completed!");
        },
        OnBeforeCreateAsync = async eventContext =>
        {
            // Validate file size
            if (eventContext.UploadLength > 5_000_000_000) // 5GB
            {
                eventContext.FailRequest("File size exceeds maximum allowed size of 5GB");
            }

            // You could add API key validation here
            // if (!eventContext.HttpContext.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
            // {
            //     eventContext.FailRequest("Missing API key");
            // }
        }
    }
});

// Minimal API endpoints

// Get file info
app.MapGet("/api/files/{fileId}", async (
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
app.MapPost("/api/files/{fileId}/associate", async (
        string fileId,
        [FromBody] AssociateRequest request,
        TusSqlServerStore store,
        CancellationToken ct) =>
    {
        var exists = await store.FileExistAsync(fileId, ct);
        if (!exists)
            return Results.NotFound();

        await store.AssociateFileWithSessionAsync(fileId, request.SessionId, ct);

        if (!string.IsNullOrEmpty(request.AppId))
        {
            await store.SetAppIdAsync(fileId, request.AppId, ct);
        }

        return Results.Ok();
    })
    .WithName("AssociateFile");

// Commit file (mark as permanent)
app.MapPost("/api/files/{fileId}/commit", async (
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
app.MapGet("/api/sessions/{sessionId}/files", async (
        string sessionId,
        TusSqlServerStore store,
        CancellationToken ct) =>
    {
        var files = await store.GetFilesBySessionAsync(sessionId, ct);
        return Results.Ok(files);
    })
    .WithName("GetFilesBySession");

// Download file
app.MapGet("/api/files/{fileId}/download", async (
        string fileId,
        TusSqlServerStore store,
        CancellationToken ct) =>
    {
        var exists = await store.FileExistAsync(fileId, ct);
        if (!exists)
            return Results.NotFound();

        var file = await store.GetFileAsync(fileId, ct);
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
app.MapDelete("/api/files/{fileId}", async (
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

app.Run();