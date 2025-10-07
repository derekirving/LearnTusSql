using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using Unify.Validation.Binary;
using Unify.Web.Ui.Component.Upload.Stores;

namespace Unify.Web.Ui.Component.Upload;

public interface IUnifyUploads
{
    int GetMinimumFiles(string zoneId);
    int GetMaximumFiles(string zoneId);
    int GetMaximumFileSize(string zoneId);
    List<string> GetAcceptedFileTypes(string zoneId);
    Task<UnifyUploadFile?> GetFileInfo(string fileId, CancellationToken cancellationToken = default);
    void ValidateFileCount(string zoneId, int fileCount, ModelStateDictionary modelState);
    List<string> ValidateFileCount(string zoneId, int fileCount);
    Task DeleteUpload(string fileId, CancellationToken cancellationToken = default);
    Task MarkComplete(List<string> fileIds, CancellationToken cancellationToken = default);
    Task<List<string>> ConvertLegacyUpload(HttpRequest httpRequest, ModelStateDictionary modelState, string zone, List<string> existingFileIds);
}


public class UnifyUploads(
    ILogger<UnifyUploads> logger,
    IConfiguration configuration,
    DefaultTusConfiguration tus,
    TusDiskStorageOptionHelper tusDiskStorageOptionHelper) : IUnifyUploads
{
    private readonly ITusReadableStore _store = tus.Store as ITusReadableStore
                                                ?? throw new Exception("TusReadableStore is not TusStore");

    public int GetMinimumFiles(string zoneId)
    {
        return configuration.GetValue<int>($"Unify:Uploads:Zones:{zoneId}:MinFiles");
    }

    public int GetMaximumFiles(string zoneId)
    {
        return configuration.GetValue<int>($"Unify:Uploads:Zones:{zoneId}:MaxFiles");
    }

    public int GetMaximumFileSize(string zoneId)
    {
        return configuration.GetValue<int>($"Unify:Uploads:Zones:{zoneId}:MaxSize");
    }

    public List<string> GetAcceptedFileTypes(string zoneId)
    {
        var accepted = configuration.GetValue<string>($"Unify:Uploads:Zones:{zoneId}:Accepted");
        return accepted?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList() ?? [];
    }

    public void ValidateFileCount(string zoneId, int fileCount, ModelStateDictionary modelState)
    {
        var maximumUploads = GetMaximumFiles(zoneId);
        var minimumUploads = GetMinimumFiles(zoneId);

        if (fileCount + 1 > maximumUploads)
        {
            modelState.AddModelError(string.Empty, $"Maximum uploads is {maximumUploads}");
        }

        if (fileCount - 1 < minimumUploads)
        {
            modelState.AddModelError(string.Empty, $"Minimum uploads is {minimumUploads}");
        }
    }

    public List<string> ValidateFileCount(string zoneId, int fileCount)
    {
        var errors = new List<string>();
        var maximumUploads = GetMaximumFiles(zoneId);
        var minimumUploads = GetMinimumFiles(zoneId);

        if (fileCount + 1 > maximumUploads)
            errors.Add($"Maximum uploads is {maximumUploads}");

        if (fileCount - 1 < minimumUploads)
            errors.Add($"Minimum uploads is {minimumUploads}");

        return errors;
    }

    public async Task<UnifyUploadFile?> GetFileInfo(string fileId, CancellationToken cancellationToken = default)
    {
        var file = await _store.GetFileAsync(fileId, cancellationToken);
        if (file is null) return null;

        var metadata = await file.GetMetadataAsync(cancellationToken);
        metadata.TryGetValue("size", out var size);
        metadata.TryGetValue("name", out var name);

        return new UnifyUploadFile
        {
            FileId = fileId,
            FileName = name?.GetString(Encoding.UTF8),
            Size = Convert.ToInt32(size?.GetString(Encoding.UTF8))
        };
    }

    public async Task DeleteUpload(string fileId, CancellationToken cancellationToken)
    {
        try
        {
            var terminationStore = (ITusTerminationStore)_store;
            await terminationStore.DeleteFileAsync(fileId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting file");
        }
    }

    public async Task MarkComplete(List<string> fileIds, CancellationToken cancellationToken = default)
    {
        foreach (var item in fileIds)
        {
            var path = Path.Combine(tusDiskStorageOptionHelper.StorageDiskPath, item + ".metadata");

            if (!File.Exists(path))
            {
                throw new UploadException($"File id {item} not found");
            }

            var content = await File.ReadAllTextAsync(path, cancellationToken);
            var updatedContent = content.Replace(Constants.InComplete, Constants.Complete);
            await File.WriteAllTextAsync(path, updatedContent, cancellationToken);
        }
    }

    public async Task<List<string>> ConvertLegacyUpload(HttpRequest httpRequest, ModelStateDictionary modelState,
        string zone,
        List<string> existingFileIds)
    {
        var files = httpRequest.Form.Files;
        var cancellationToken = httpRequest.HttpContext.RequestAborted;

        if (existingFileIds.Count > 0 && files.Count > 0)
        {
            throw new UploadException("Both tus and regular file uploads are present");
        }

        foreach (var file in files)
        {
            var originalFileName = file.FileName;
            var fileType = file.ContentType;
            var fileSizeInBytes = file.Length;

            var maxSize = GetMaximumFileSize(zone);
            var ext = Path.GetExtension(file.FileName);
            var binaryValidator = httpRequest.HttpContext.RequestServices.GetRequiredService<IUnifyBinaryValidator>();

            var (valid, reason) = binaryValidator.Validate(file.OpenReadStream(), maxSize, ext);

            if (!valid)
            {
                modelState.AddModelError("", reason);
            }

            var tusFileName = Guid.NewGuid().ToString("N");
            var filePath = Path.Combine(tusDiskStorageOptionHelper.StorageDiskPath, tusFileName);

            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream, cancellationToken);

            var chunkStartPath = Path.Combine(tusDiskStorageOptionHelper.StorageDiskPath, tusFileName + ".chunkstart");
            var chunkCompletePath =
                Path.Combine(tusDiskStorageOptionHelper.StorageDiskPath, tusFileName + ".chunkcomplete");
            var uploadLengthPath =
                Path.Combine(tusDiskStorageOptionHelper.StorageDiskPath, tusFileName + ".uploadlength");

            await File.WriteAllTextAsync(chunkStartPath, "0", cancellationToken);
            await File.WriteAllTextAsync(chunkCompletePath, "1", cancellationToken);
            await File.WriteAllTextAsync(uploadLengthPath, fileSizeInBytes.ToString(), cancellationToken);

            var metaDataPath = Path.Combine(tusDiskStorageOptionHelper.StorageDiskPath, tusFileName + ".metadata");

            var stringBuilder = new StringBuilder()
                .Append($"name {Convert.ToBase64String(Encoding.UTF8.GetBytes(originalFileName))},")
                .Append($"contentType {Convert.ToBase64String(Encoding.UTF8.GetBytes(fileType))},")
                .Append($"size {Convert.ToBase64String(Encoding.UTF8.GetBytes(fileSizeInBytes.ToString()))},")
                .Append($"zone {Convert.ToBase64String(Encoding.UTF8.GetBytes(zone))},")
                .Append("completed ZmFsc2U="); // false

            await File.WriteAllTextAsync(metaDataPath, stringBuilder.ToString(), cancellationToken);

            existingFileIds.Add(tusFileName);
        }

        return existingFileIds;
    }
}