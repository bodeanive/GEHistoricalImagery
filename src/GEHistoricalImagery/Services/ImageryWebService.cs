using GEHistoricalImagery.Services.Operations;
using GEHistoricalImagery.Web;
using LibMapCommon;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace GEHistoricalImagery.Services;

public sealed class ImageryWebService
{
    private readonly SemaphoreSlim _consoleLock = new(1, 1);

    public async Task<CommandRunResult> RunInfoAsync(Provider provider, bool noCache, Wgs1984 coordinate, int zoom, CancellationToken cancellationToken)
    {
        var verb = new Info
        {
            Provider = provider,
            DisableCache = noCache,
            Coordinate = coordinate,
            ZoomLevel = zoom
        };

        return await ExecuteOperationAsync(verb, captureConsole: true, cancellationToken);
    }

    public async Task<CommandRunResult> RunAvailabilityAsync(AvailabilityRequest request, CancellationToken cancellationToken)
    {
        var verb = new Availability
        {
            CompleteOnly = request.CompleteOnly,
            MinDate = request.MinDate ?? default,
            MaxDate = request.MaxDate ?? default
        };

        ApplyAoi(verb, request);
        return await ExecuteOperationAsync(verb, captureConsole: true, cancellationToken);
    }

    public async Task<FileRunResult> RunDownloadAsync(DownloadRequest request, CancellationToken cancellationToken)
    {
        if (request.Dates is null || request.Dates.Count == 0)
            throw new ArgumentException("At least one date is required.", nameof(request.Dates));

        var outputPath = Path.Combine(Path.GetTempPath(), $"gehi_download_{Guid.NewGuid():N}.tif");

        var verb = new Download
        {
            SavePath = outputPath,
            Dates = request.Dates,
            ExactMatch = request.ExactDate,
            LayerDate = request.LayerDate,
            TargetSpatialReference = request.TargetSpatialReference,
            ScaleFactor = request.ScaleFactor,
            OffsetX = request.OffsetX,
            OffsetY = request.OffsetY,
            ScaleFirst = request.ScaleFirst
        };

        ApplyAoi(verb, request);
        var run = await ExecuteOperationAsync(verb, captureConsole: false, cancellationToken);

        return new FileRunResult
        {
            StdOut = run.StdOut,
            StdErr = run.StdErr,
            Exception = run.Exception,
            OutputPath = outputPath
        };
    }

    public async Task<FileRunResult> RunDumpAsync(DumpRequest request, CancellationToken cancellationToken)
    {
        if (request.Dates is null || request.Dates.Count == 0)
            throw new ArgumentException("At least one date is required.", nameof(request.Dates));

        var dumpDirectory = Path.Combine(Path.GetTempPath(), $"gehi_dump_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dumpDirectory);

        var verb = new Dump
        {
            SavePath = dumpDirectory,
            Dates = request.Dates,
            ExactMatch = request.ExactDate,
            LayerDate = request.LayerDate,
            TargetSpatialReference = request.TargetSpatialReference,
            Formatter = request.Formatter,
            WriteWorldFile = request.WriteWorldFile
        };

        ApplyAoi(verb, request);

        var run = await ExecuteOperationAsync(verb, captureConsole: false, cancellationToken);
        var archivePath = Path.Combine(Path.GetTempPath(), $"gehi_dump_{Guid.NewGuid():N}.zip");

        try
        {
            if (run.Exception is null)
            {
                var hasFiles = Directory.EnumerateFiles(dumpDirectory, "*", SearchOption.AllDirectories).Any();
                if (hasFiles)
                {
                    ZipFile.CreateFromDirectory(dumpDirectory, archivePath, CompressionLevel.Fastest, includeBaseDirectory: false);
                    return new FileRunResult
                    {
                        StdOut = run.StdOut,
                        StdErr = run.StdErr,
                        Exception = null,
                        OutputPath = archivePath
                    };
                }

                return new FileRunResult
                {
                    StdOut = run.StdOut,
                    StdErr = AppendError(run.StdErr, "No tiles were generated for this request."),
                    Exception = null,
                    OutputPath = string.Empty
                };
            }

            return new FileRunResult
            {
                StdOut = run.StdOut,
                StdErr = run.StdErr,
                Exception = run.Exception,
                OutputPath = string.Empty
            };
        }
        finally
        {
            TryDeleteDirectory(dumpDirectory);
        }
    }

    private static void ApplyAoi(AoiOperation verb, AoiRequestBase request)
    {
        verb.Provider = request.Provider;
        verb.DisableCache = request.NoCache;
        verb.ZoomLevel = request.Zoom;

        if (request.Parallel.HasValue)
            verb.ConcurrentDownload = request.Parallel.Value;

        if (request.Region is { Count: >= 3 })
        {
            verb.RegionCoordinates = request.Region
                .Select(ToCoordinateString)
                .ToList();
            return;
        }

        if (request.LowerLeft is not null && request.UpperRight is not null)
        {
            verb.LowerLeft = ToWgs1984(request.LowerLeft);
            verb.UpperRight = ToWgs1984(request.UpperRight);
            return;
        }

        throw new ArgumentException("You must provide either a polygon region (>= 3 points) or lower-left and upper-right corners.");
    }

    private static Wgs1984 ToWgs1984(GeoPointRequest point)
        => new(point.Latitude, point.Longitude);

    private static string ToCoordinateString(GeoPointRequest point)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{point.Latitude:F8},{point.Longitude:F8}");

    private async Task<CommandRunResult> ExecuteOperationAsync(OptionsBase operation, bool captureConsole, CancellationToken cancellationToken)
    {
        if (!captureConsole)
        {
            try
            {
                await operation.RunAsync();
                return new CommandRunResult();
            }
            catch (Exception ex)
            {
                return new CommandRunResult { Exception = ex };
            }
        }

        await _consoleLock.WaitAsync(cancellationToken);

        var stdOutWriter = new StringWriter(CultureInfo.InvariantCulture);
        var stdErrWriter = new StringWriter(CultureInfo.InvariantCulture);
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        Exception? exception = null;

        try
        {
            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);
            await operation.RunAsync();
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            _consoleLock.Release();
        }

        return new CommandRunResult
        {
            StdOut = NormalizeConsoleOutput(stdOutWriter.ToString()),
            StdErr = NormalizeConsoleOutput(stdErrWriter.ToString()),
            Exception = exception
        };
    }

    private static string NormalizeConsoleOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);

        foreach (var ch in text)
        {
            if (ch == '\b')
            {
                if (builder.Length > 0)
                    builder.Length--;
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    private static string AppendError(string current, string message)
    {
        if (string.IsNullOrWhiteSpace(current))
            return message;

        return current + Environment.NewLine + message;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}

public class CommandRunResult
{
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
}

public sealed class FileRunResult : CommandRunResult
{
    public string OutputPath { get; init; } = string.Empty;
}



