using GEHistoricalImagery.Services.Operations;
using GEHistoricalImagery.Services;
using GEHistoricalImagery.Web;
using LibMapCommon;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace GEHistoricalImagery.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ImageryController : ControllerBase
{
    private readonly ImageryWebService _imageryService;

    public ImageryController(ImageryWebService imageryService)
    {
        _imageryService = imageryService;
    }

    [HttpGet("info")]
    [Produces("text/plain")]
    public async Task<IActionResult> GetInfo(
        [FromQuery] string location,
        [FromQuery] int zoom,
        [FromQuery] Provider provider = Provider.TM,
        [FromQuery] bool noCache = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseLocation(location, out var coordinate, out var errorMessage))
            return BadRequest(errorMessage);

        var result = await _imageryService.RunInfoAsync(provider, noCache, coordinate, zoom, cancellationToken);
        return ToTextResult(result);
    }

    [HttpPost("availability")]
    [Produces("text/plain")]
    public async Task<IActionResult> GetAvailability([FromBody] AvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        try
        {
            var result = await _imageryService.RunAvailabilityAsync(request, cancellationToken);
            return ToTextResult(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("download")]
    public async Task<IActionResult> Download([FromBody] DownloadRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        FileRunResult result;
        try
        {
            result = await _imageryService.RunDownloadAsync(request, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        if (result.Exception is not null)
        {
            TryDeleteFile(result.OutputPath);
            return StatusCode(500, BuildErrorOutput(result));
        }

        if (!IsOutputFileReady(result.OutputPath))
        {
            TryDeleteFile(result.OutputPath);
            return BadRequest(BuildErrorOutput(result));
        }

        var fileName = NormalizeFileName(request.FileName, "historical_imagery.tif", ".tif");
        var stream = new FileStream(result.OutputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        HttpContext.Response.OnCompleted(() => CleanupFileAsync(stream, result.OutputPath));

        return File(stream, "image/tiff", fileName);
    }

    [HttpPost("dump")]
    public async Task<IActionResult> Dump([FromBody] DumpRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        FileRunResult result;
        try
        {
            result = await _imageryService.RunDumpAsync(request, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        if (result.Exception is not null)
        {
            TryDeleteFile(result.OutputPath);
            return StatusCode(500, BuildErrorOutput(result));
        }

        if (!IsOutputFileReady(result.OutputPath))
            return BadRequest(BuildErrorOutput(result));

        var fileName = NormalizeFileName(request.ArchiveName, "historical_tiles.zip", ".zip");
        var stream = new FileStream(result.OutputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        HttpContext.Response.OnCompleted(() => CleanupFileAsync(stream, result.OutputPath));

        return File(stream, "application/zip", fileName);
    }

    private IActionResult ToTextResult(CommandRunResult result)
    {
        if (result.Exception is not null)
            return StatusCode(500, BuildErrorOutput(result));

        if (!string.IsNullOrEmpty(result.StdOut))
            return Content(result.StdOut, "text/plain", Encoding.UTF8);

        if (!string.IsNullOrEmpty(result.StdErr))
            return BadRequest(result.StdErr);

        return BadRequest("Command did not return any output.");
    }

    private static string BuildErrorOutput(CommandRunResult result)
    {
        var stdout = result.StdOut ?? string.Empty;
        var stderr = result.StdErr ?? string.Empty;
        var exceptionText = result.Exception?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(stdout) && string.IsNullOrEmpty(stderr) && string.IsNullOrEmpty(exceptionText))
            return "Operation failed.";

        var parts = new List<string>(3);
        if (!string.IsNullOrEmpty(stdout))
            parts.Add(stdout);
        if (!string.IsNullOrEmpty(stderr))
            parts.Add(stderr);
        if (!string.IsNullOrEmpty(exceptionText))
            parts.Add(exceptionText);

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private static bool TryParseLocation(string location, out Wgs1984 coordinate, out string errorMessage)
    {
        coordinate = default;
        errorMessage = "Invalid location format. Use location=LAT,LONG";

        if (string.IsNullOrWhiteSpace(location))
            return false;

        var parts = location.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            return false;
        }

        try
        {
            coordinate = new Wgs1984(latitude, longitude);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static string NormalizeFileName(string? requestedName, string fallbackName, string requiredExtension)
    {
        var name = string.IsNullOrWhiteSpace(requestedName) ? fallbackName : requestedName.Trim();

        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        if (!name.EndsWith(requiredExtension, StringComparison.OrdinalIgnoreCase))
            name += requiredExtension;

        return name;
    }

    private static bool IsOutputFileReady(string path)
        => !string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path) && new FileInfo(path).Length > 0;

    private static Task CleanupFileAsync(FileStream stream, string filePath)
    {
        stream.Dispose();
        TryDeleteFile(filePath);
        return Task.CompletedTask;
    }

    private static void TryDeleteFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}

