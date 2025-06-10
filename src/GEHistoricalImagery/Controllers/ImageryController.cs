using Microsoft.AspNetCore.Mvc;
using GEHistoricalImagery.Services;
using LibMapCommon;
using LibMapCommon.Geometry;

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

        // GET /api/imagery/info?lat=39.63&lon=-104.84&zoom=21&provider=TM
        [HttpGet("info")]
        public async Task<IActionResult> GetInfo(double lat, double lon, int zoom, Provider provider = Provider.TM)
        {
            try
            {
                var coordinate = new Wgs1984(lat, lon);
                var info = await _imageryService.GetImageryInfoAsync(provider, coordinate, zoom);
                return Ok(info); // 返回 JSON 格式的结果和 HTTP 200 OK
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
        public List<Point> Region { get; set; }
        public int Zoom { get; set; }
        public DateOnly Date { get; set; }
    }

    public class Point
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
