using LibMapCommon;
using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Services.Operations;

internal abstract class AoiOperation : OptionsBase
{
	public IList<string>? RegionCoordinates { get; set; }

	public Wgs1984? LowerLeft { get; set; }

	public Wgs1984? UpperRight { get; set; }

	public int ZoomLevel { get; set; }

	public int ConcurrentDownload { get; set; }

	protected GeoRegion<Wgs1984> Region { get; set; } = null!;

	protected bool AnyAoiErrors()
	{
		var errors = GetAoiErrors().ToList();
		errors.ForEach(Console.Error.WriteLine);
		return errors.Count > 0;
	}

	protected IEnumerable<string> GetAoiErrors()
	{
		if (ConcurrentDownload <= 0)
			ConcurrentDownload = Environment.ProcessorCount;
		ConcurrentDownload = Math.Min(ConcurrentDownload, Environment.ProcessorCount);

		if (ZoomLevel > 23)
			yield return $"Zoom level: {ZoomLevel} is too large. Max zoom is 23";
		else if (ZoomLevel < 1)
			yield return $"Zoom level: {ZoomLevel} is too small. Min zoom is 1";

		if (RegionCoordinates?.Count >= 3)
		{
			var converter = new Wgs1984TypeConverter();
			var coords = new Wgs1984[RegionCoordinates.Count];
			for (int i = 0; i < RegionCoordinates.Count; i++)
			{
				if (converter.ConvertFrom(RegionCoordinates[i]) is not Wgs1984 coord)
				{
					yield return $"Invalid coordinate '{RegionCoordinates[i]}'";
					yield break;
				}
				coords[i] = coord;
			}

			Region = GeoRegion<Wgs1984>.Create(coords);
		}
		else if (LowerLeft is null && UpperRight is null)
			yield return "An area of interest must be specified either with the 'region' option or the 'lower-left' and 'upper-right' options";
		else if (LowerLeft is null)
			yield return $"Invalid lower-left coordinate.{Environment.NewLine} Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
		else if (UpperRight is null)
			yield return $"Invalid upper-right coordinate.{Environment.NewLine} Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
		else
		{
			string? errorMessage = null;
			try
			{
				var llX = LowerLeft.Value.Longitude;
				var llY = LowerLeft.Value.Latitude;
				var urX = UpperRight.Value.Longitude;
				var urY = UpperRight.Value.Latitude;
				if (urX < llX)
					urX += 360;

				Region = GeoRegion<Wgs1984>.Create(
					new Wgs1984(llY, llX),
					new Wgs1984(urY, llX),
					new Wgs1984(urY, urX),
					new Wgs1984(llY, urX));
			}
			catch (Exception e)
			{
				errorMessage = $"Invalid rectangle.{Environment.NewLine} {e.Message}";
			}
			if (errorMessage != null)
				yield return errorMessage;
		}
	}
}


