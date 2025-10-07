# Unify.Validation

This can be used in conjunction with the NPM package [sbs-aspnet-client-validation](../../NpmLibraries/sbs-aspnet-client-validation)

## Binary Validation

Unify.Validation uses [Mime-Detective](https://github.com/MediatedCommunications/Mime-Detective)
to accurately identify over 14,000 different file variants by analyzing a raw stream or array of bytes

1. Add `AddUnifyBinaryValidation` to the service collection passing in all the file extensions you expect your application to have to validate.
2. Use the `IUnifyBinaryValidator` interface to perform the validation. This method will validate both the [media/mime-type](https://en.wikipedia.org/wiki/Media_type) and file size.

```c#
using Unify.Validation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddUnifyBinaryValidation(new[] { "png", "jpg", "docx" });

var app = builder.Build();

app.MapPost("/upload", async Task<IResult>(HttpRequest request, IUnifyBinaryValidator validator, IWebHostEnvironment environment) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest();

     var token = request.HttpContext.RequestAborted;

    var form = await request.ReadFormAsync(token);
    var formFile = form.Files["file"];

    if (formFile is null || formFile.Length == 0)
        return Results.BadRequest();

    await using var stream = formFile.OpenReadStream();

    var (success, failureReason) = validator.Validate(stream, maxLength:6291456, Path.GetExtension(formFile.FileName));

    if(success)
    {
        var name = Guid.NewGuid().ToString()[..8] + Path.GetExtension(formFile.FileName);
        var mediaFolder = Path.Combine(environment.WebRootPath, "media");
        var filePath = Path.Combine(mediaFolder, name);
        stream.Position = 0;
        await using var file = File.Create(filePath);
        await stream.CopyToAsync(file, token);
    }

    return !success ? Results.BadRequest(failureReason) : Results.Ok();
});

// .NET7.0 supports the IFormFile interface directly
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-7.0#file-uploads-using-iformfile-and-iformfilecollection
app.MapPost("/upload-net7", (IFormFile file, IUnifyBinaryValidator validator) =>
{
    var (success, reason) = validator.Validate(file, 9000);
    return !success ? Results.BadRequest(reason) : Results.Ok();
});```

## Extended Attributes

There is nothing to set up or configure, just use the validators on your models, for example:

```c#
using System;
using System.ComponentModel.DataAnnotations;
using Unify.Validation;

public class ViewModel
{
    [MustBeChecked(ErrorMessage = "You must confirm you have read and understood the privacy policy")]
    public bool Terms { get; set; }
}
```

And then add the elements to your View/Razor page:

```html
<div class="form-group form-check">
    <input asp-for="ViewModel.Terms" class="form-check-input">
    <label asp-for="ViewModel.Terms" class="form-check-label">I have understood the <a asp-page="Privacy">privacy policy</a></label>
    <span asp-validation-for="ViewModel.Terms"></span>
</div>
```