# GEHistoricalImagery
GEHistoricalImagery is a utility for downloading historical aerial imagery from Google Earth...
**and now also from Esri's World Atlas Wayback**

**Features**
- Find historical imagery availability at any location and zoom level
- Always uses the most recent provider data
- Automatically substitutes unavailable tiles with temporally closest available tile
- Outputs a georeferenced GeoTiff or dumps image tiles to a folder
- Supports warping to new coordinate systems
- Fast! Parallel downloading and local caching

**Commands**
|Command|Description|
|-|-|
|[info](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/info.md)|Get imagery info at a specified location.|
|[availability](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/availability.md)|Get imagery date availability in a specified region.|
|[download](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/download.md)|Download historical imagery.|
|[dump](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/dump.md)|Dump historical image tiles into a folder.|


**Web API**
`http://localhost:35001`
``` bash
curl -X GET "http://localhost:35001/api/imagery/info?location=39.63,-104.84&zoom=21&provider=TM"
Dated Imagery at 39.630000°, -104.840000°
  Level = 21, Path = 0301232010121332002213
    date = 2017/05/14, version = 233
    date = 2019/10/03, version = 276
    date = 2020/10/03, version = 357
    date = 2023/10/20, version = 1019
```
=======
To learn about defining regions of interest for these commands, please refer to the [Regions of Interest article](https://github.com/Mbucari/GEHistoricalImagery/blob/master/docs/regions.md).

************************
## Build 
download or git clone 
```console
dotnet publish -c Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release --runtime win-arm64 --self-contained true -p:PublishSingleFile=true

dotnet publish -c Release --runtime linux-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release --runtime linux-arm64 --self-contained true -p:PublishSingleFile=true
```

************************
<p align="center"><i>Updated 2025/06/20</i></p>

## Deploy to EdgeOne
[![使用 EdgeOne Pages 部署](https://cdnstatic.tencentcs.com/edgeone/pages/deploy.svg)](https://edgeone.ai/pages/new?repository-url=https://github.com/bodeanive/GEHistoricalImagery)
