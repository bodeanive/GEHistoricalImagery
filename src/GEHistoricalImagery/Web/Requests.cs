using GEHistoricalImagery.Services.Operations;

namespace GEHistoricalImagery.Web;

public sealed class GeoPointRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public abstract class AoiRequestBase
{
    public Provider Provider { get; set; } = Provider.TM;
    public bool NoCache { get; set; }
    public int Zoom { get; set; }
    public int? Parallel { get; set; }
    public List<GeoPointRequest>? Region { get; set; }
    public GeoPointRequest? LowerLeft { get; set; }
    public GeoPointRequest? UpperRight { get; set; }
}

public sealed class AvailabilityRequest : AoiRequestBase
{
    public bool CompleteOnly { get; set; }
    public DateOnly? MinDate { get; set; }
    public DateOnly? MaxDate { get; set; }
}

public sealed class DownloadRequest : AoiRequestBase
{
    public List<DateOnly> Dates { get; set; } = new();
    public bool ExactDate { get; set; }
    public bool LayerDate { get; set; }
    public string? TargetSpatialReference { get; set; }
    public double ScaleFactor { get; set; } = 1d;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public bool ScaleFirst { get; set; }
    public string? FileName { get; set; }
}

public sealed class DumpRequest : AoiRequestBase
{
    public List<DateOnly> Dates { get; set; } = new();
    public bool ExactDate { get; set; }
    public bool LayerDate { get; set; }
    public string? TargetSpatialReference { get; set; }
    public string? Formatter { get; set; }
    public bool WriteWorldFile { get; set; }
    public string? ArchiveName { get; set; }
}

