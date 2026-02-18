
namespace GEHistoricalImagery.Services.Operations;

internal abstract class FileDownloadOperation : AoiOperation
{
	public IEnumerable<DateOnly>? Dates { get; set; }

	public bool ExactMatch { get; set; }

	public bool LayerDate { get; set; }

	public string? TargetSpatialReference { get; set; }

	public abstract string? SavePath { get; set; }

	protected bool AnyFileDownloadErrors()
	{
		var errors = GetFileDownloadErrors().ToList();
		errors.ForEach(Console.Error.WriteLine);
		return errors.Count > 0;
	}

	private IEnumerable<string> GetFileDownloadErrors()
	{
		foreach (var errorMessage in GetAoiErrors())
			yield return errorMessage;

		if (Dates?.Any() is not true)
		{
			yield return "At least one date must be specified.";
		}

		if (string.IsNullOrWhiteSpace(SavePath))
		{
			yield return "Invalid output file path";
		}
	}
}


