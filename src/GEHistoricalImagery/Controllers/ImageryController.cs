using Microsoft.AspNetCore.Mvc;
using GEHistoricalImagery.Services;
using LibMapCommon;
using LibMapCommon.Geometry;
using GEHistoricalImagery.Cli;
using System.Text;
using System.Globalization;
namespace GEHistoricalImagery.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageryController : ControllerBase
    {
        private readonly ImageryService _imageryService;

        // 使用依赖注入来获取服务实例
        public ImageryController(ImageryService imageryService)
        {
            _imageryService = imageryService;
        }

        // using System.Globalization; // 确保文件顶部有这个 using 指令

        // GET /api/imagery/info?location=39.63,-104.84&zoom=21
        [HttpGet("info")]
        public async Task<IActionResult> GetInfo([FromQuery] string location, [FromQuery] int zoom, [FromQuery] Provider provider = Provider.TM)
        {
            // 1. 手动解析 location 字符串
            if (string.IsNullOrWhiteSpace(location))
            {
                return BadRequest("Location parameter is required. Use format: location=LAT,LONG");
            }

            var parts = location.Split(',');
            if (parts.Length != 2 ||
                !double.TryParse(parts[0], CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(parts[1], CultureInfo.InvariantCulture, out var lon))
            {
                return BadRequest("Invalid location format. Use format: location=LAT,LONG");
            }

            var wgsLocation = new Wgs1984(lat, lon);

            // 2. 后续逻辑保持不变
            try
            {
                var imageryInfo = await _imageryService.GetImageryInfoAsync(provider, wgsLocation, zoom);

                var sb = new StringBuilder();

                if (provider == Provider.TM)
                {
                    var tile = LibGoogleEarth.KeyholeTile.GetTile(wgsLocation, zoom);
                    sb.AppendLine($"Dated Imagery at {wgsLocation.Latitude:F6}°, {wgsLocation.Longitude:F6}°");
                    sb.AppendLine($"  Level = {zoom}, Path = {tile.Path}");

                    if (imageryInfo.Count == 0)
                    {
                        sb.AppendLine("  No dated imagery found at this location.");
                    }
                    else
                    {
                        foreach (var info in imageryInfo)
                        {
                            sb.AppendLine($"    date = {info.Date:yyyy/MM/dd}, version = {info.Version}");
                        }
                    }
                }
                else // Wayback
                {
                    sb.AppendLine($"Dated Imagery at {wgsLocation.Latitude:F6}°, {wgsLocation.Longitude:F6}° (Wayback)");
                    if (imageryInfo.Count == 0)
                    {
                        sb.AppendLine("  No dated imagery found at this location.");
                    }
                    else
                    {
                        foreach (var info in imageryInfo)
                        {
                            sb.AppendLine($"    Capture Date = {info.Date:yyyy/MM/dd}, Layer Date = {info.LayerDate:yyyy/MM/dd}");
                        }
                    }
                }

                return Content(sb.ToString(), "text/plain", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        // POST /api/imagery/download
        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        {
            if (request?.Region == null || request.Region.Count < 3)
            {
                return BadRequest("A valid region with at least 3 points is required.");
            }

            try
            {
                var regionPolygon = new GeoPolygon<Wgs1984>(request.Region.Select(p => new Wgs1984(p.Latitude, p.Longitude)).ToArray());

                // 注意：实际的 DownloadImageAsync 实现会很复杂
                byte[] imageBytes = await _imageryService.DownloadImageAsync(request.Provider, regionPolygon, request.Zoom, request.Date);

                // 返回文件流
                return File(imageBytes, "image/tiff", $"historical_imagery_{request.Date:yyyy-MM-dd}.tif");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }

    // 用于接收下载请求 body 的模型
    public class DownloadRequest
    {
        public Provider Provider { get; set; }
        public List<Point> Region { get; set; } = new List<Point>(); // <--- 修改这一行
        public int Zoom { get; set; }
        public DateOnly Date { get; set; }
    }

    public class Point
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
