using LibGoogleEarth;
using LibEsri;
using LibMapCommon;
using LibMapCommon.Geometry;
using GEHistoricalImagery.Cli;

namespace GEHistoricalImagery.Services
{
    public class ImageryService
    {
        private readonly string? _cacheDir;

        public ImageryService(bool disableCache)
        {
            _cacheDir = disableCache ? null 
                : Environment.GetEnvironmentVariable("GEHistoricalImagery_Cache") ?? "./cache";
        }

        // 从 Info.cs 重构的逻辑
        public async Task<List<DatedImageInfo>> GetImageryInfoAsync(Provider provider, Wgs1984 coordinate, int zoomLevel)
        {
            var results = new List<DatedImageInfo>();
            if (provider == Provider.TM)
            {
                var root = await DbRoot.CreateAsync(Database.TimeMachine, _cacheDir);
                var tile = KeyholeTile.GetTile(coordinate, zoomLevel);
                var node = await root.GetNodeAsync(tile);
                if (node != null)
                {
                    foreach (var dated in node.GetAllDatedTiles())
                    {
                        if (dated.Date.Year != 1)
                            results.Add(new DatedImageInfo { Date = dated.Date, Version = dated.Epoch });
                    }
                }
            }
            else // Wayback
            {
                var wayBack = await WayBack.CreateAsync(_cacheDir);
                var tile = EsriTile.GetTile(coordinate.ToWebMercator(), zoomLevel);
                await foreach (var dated in wayBack.GetDatesAsync(tile))
                {
                     results.Add(new DatedImageInfo { Date = dated.CaptureDate, LayerDate = dated.LayerDate });
                }
            }
            return results;
        }

        // 从 Download.cs 重构的逻辑
        public async Task<byte[]> DownloadImageAsync(Provider provider, GeoPolygon<Wgs1984> region, int zoom, DateOnly date)
        {
            // ... 这里是 Download.cs 中的大部分下载和图像拼接逻辑 ...
            // 这个方法最终应该返回一个图像文件的字节数组 (byte[])。
            // 为了简化，我们只返回一个占位符
            await Task.CompletedTask;
            return System.Text.Encoding.UTF8.GetBytes("This is a placeholder for the generated GeoTiff image.");
        }
    }

    // 一个简单的数据传输对象 (DTO)
    public class DatedImageInfo
    {
        public DateOnly Date { get; set; }
        public DateOnly? LayerDate {get; set; }
        public int? Version { get; set; }
    }
}
