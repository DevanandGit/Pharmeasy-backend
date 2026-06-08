using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PharmeasyAPI.Controllers;

/// <summary>Upload images and serve them as static files.</summary>
[ApiController]
[Route("images")]
[Produces("application/json")]
public class ImageUploadController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly IWebHostEnvironment _env;

    public ImageUploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>Upload an image file and receive its public URL path.</summary>
    /// <remarks>
    /// Send a **multipart/form-data** request with a single field named <c>image</c>.
    /// Allowed types: jpg, jpeg, png, gif, webp. Max size: 10 MB.
    ///
    /// The returned <c>imageUrl</c> (e.g. <c>/images/abc123.jpg</c>) can be stored
    /// directly in the <c>image</c> field when creating or updating a product or category.
    /// </remarks>
    /// <response code="200">Image uploaded successfully; returns the public URL path.</response>
    /// <response code="400">No file provided, unsupported file type, or file exceeds 10 MB.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImageUploadErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload([FromForm] ImageUploadRequest request)
    {
        var image = request.Image;

        if (image is null || image.Length == 0)
            return BadRequest(new ImageUploadErrorResponse { Message = "No image file provided." });

        if (image.Length > 10 * 1024 * 1024)
            return BadRequest(new ImageUploadErrorResponse { Message = "Image must be 10 MB or smaller." });

        var ext = Path.GetExtension(image.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new ImageUploadErrorResponse { Message = $"Unsupported file type '{ext}'. Allowed: jpg, jpeg, png, gif, webp." });

        var imagesDir = Path.Combine(_env.ContentRootPath, "images");
        Directory.CreateDirectory(imagesDir);

        var fileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var filePath = Path.Combine(imagesDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await image.CopyToAsync(stream);

        return Ok(new ImageUploadResponse { ImageUrl = $"/images/{fileName}" });
    }
}

public class ImageUploadRequest
{
    /// <summary>The image file to upload (jpg, jpeg, png, gif, webp — max 10 MB).</summary>
    public IFormFile? Image { get; set; }
}

public class ImageUploadResponse
{
    /// <summary>Public URL path to the uploaded image.</summary>
    /// <example>/images/3f2504e0-4f89-11d3-9a0c-0305e82c3301.jpg</example>
    public string ImageUrl { get; set; } = string.Empty;
}

public class ImageUploadErrorResponse
{
    /// <summary>Human-readable error description.</summary>
    /// <example>Unsupported file type '.pdf'. Allowed: jpg, jpeg, png, gif, webp.</example>
    public string Message { get; set; } = string.Empty;
}
