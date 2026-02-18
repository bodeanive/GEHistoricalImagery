# GEHistoricalImagery Web Complete

This folder contains a full Web API wrapper over the original `GEHistoricalImagery` core.
The command-line parser layer has been removed from this project; runtime behavior is now handled by `Service + Controller + internal operation classes`.

## Run

```bash
dotnet run --project src/GEHistoricalImagery/GEHistoricalImagery.csproj -c Release
```

Default URL:

- `http://localhost:35001`

Swagger (development environment):

- `http://localhost:35001/swagger`

## Endpoints

- `GET /api/imagery/info`
- `POST /api/imagery/availability`
- `POST /api/imagery/download`
- `POST /api/imagery/dump`

Text output format for `info` and `availability` follows the original project console format (same labels, spacing, and date formatting).

## GitHub Actions

Workflows are included in `.github/workflows`:
- `validate.yml` (watch started trigger)
- `build.yml` (matrix build entry)
- `build-windows.yml`
- `build-linux.yml`

No release workflow is included.

## Build artifact layout

To keep artifact roots clean, CI publish output is placed under a `gdal/` subfolder and a launcher is created at the bundle root:

- Linux: `GEHistoricalImagery` (launcher script) + `gdal/`
- Windows: `GEHistoricalImagery.bat` (launcher script) + `gdal/`

This project includes GDAL runtime assets, so publish output contains many files by design. The files are grouped under `gdal/` to reduce top-level clutter.

For local publish with the same layout:

```bash
dotnet publish src/GEHistoricalImagery/GEHistoricalImagery.csproj -c Release -r linux-x64 -o src/bin/Publish/linux-x64/gdal
dotnet publish src/GEHistoricalImagery/GEHistoricalImagery.csproj -c Release -r win-x64 -o src/bin/Publish/win-x64/gdal
```

## Web API usage examples

Set base URL first (change port if needed):

```bash
BASE_URL="http://localhost:35001"
```

Run with custom port:

```bash
dotnet run --project src/GEHistoricalImagery/GEHistoricalImagery.csproj -c Release -- --Web:Url=http://0.0.0.0:35002
```

Run with proxy (example: local proxy `39080`):

```bash
export HTTP_PROXY=http://127.0.0.1:39080
export HTTPS_PROXY=http://127.0.0.1:39080
dotnet run --project src/GEHistoricalImagery/GEHistoricalImagery.csproj -c Release -- --Web:Url=http://0.0.0.0:35002
```

### 1) `GET /api/imagery/info`

Query parameters:

- `location` (required): `LAT,LONG`
- `zoom` (required): `1-23`
- `provider` (optional): `TM` or `Wayback`, default `TM`
- `noCache` (optional): `true/false`, default `false`

```bash
curl "$BASE_URL/api/imagery/info?location=39.63,-104.84&zoom=21&provider=TM&noCache=false"
```

Sample response:

```text
Dated Imagery at 39.630000°, -104.840000°
  Level = 21, Path = 0301232010121332002213
    date = 2017/05/14, version = 233
    date = 2019/10/03, version = 276
    date = 2020/10/03, version = 357
    date = 2023/10/20, version = 1019
```

### 2) `POST /api/imagery/availability`

Body parameters:

- `provider`: `TM` or `Wayback` (default `TM`)
- `noCache`: bool
- `zoom`: int (`1-23`)
- `parallel`: int (optional)
- `region`: polygon points (at least 3), optional if bbox is provided
- `lowerLeft` + `upperRight`: bbox corners, optional if `region` is provided
- `completeOnly`: bool
- `minDate`: `yyyy-MM-dd` (optional)
- `maxDate`: `yyyy-MM-dd` (optional)

Example (full parameter request):

```bash
curl -X POST "$BASE_URL/api/imagery/availability" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "TM",
    "zoom": 21,
    "noCache": false,
    "parallel": 2,
    "region": [
      { "latitude": 39.6299, "longitude": -104.8402 },
      { "latitude": 39.6301, "longitude": -104.8402 },
      { "latitude": 39.6301, "longitude": -104.8398 },
      { "latitude": 39.6299, "longitude": -104.8398 }
    ],
    "completeOnly": false,
    "minDate": "2019-01-01",
    "maxDate": "2024-12-31"
  }'
```

Alternative (bbox instead of polygon):

```bash
curl -X POST "$BASE_URL/api/imagery/availability" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "TM",
    "zoom": 20,
    "lowerLeft": { "latitude": 39.63, "longitude": -104.84 },
    "upperRight": { "latitude": 39.64, "longitude": -104.83 }
  }'
```

### 3) `POST /api/imagery/download` (returns `.tif`)

Body parameters:

- All AOI fields from `availability`: `provider`, `noCache`, `zoom`, `parallel`, `region` or `lowerLeft`+`upperRight`
- `dates` (required): array of `yyyy-MM-dd`
- `exactDate`: bool
- `layerDate`: bool
- `targetSpatialReference`: string (optional, e.g. `EPSG:3857`)
- `scaleFactor`: number
- `offsetX`: number
- `offsetY`: number
- `scaleFirst`: bool
- `fileName`: string (optional)

```bash
curl -X POST "$BASE_URL/api/imagery/download" \
  -H "Content-Type: application/json" \
  -o historical_imagery.tif \
  -d '{
    "provider": "TM",
    "zoom": 21,
    "noCache": false,
    "parallel": 2,
    "lowerLeft": { "latitude": 39.6299, "longitude": -104.8402 },
    "upperRight": { "latitude": 39.6301, "longitude": -104.8398 },
    "dates": ["2023-10-20"],
    "exactDate": false,
    "layerDate": false,
    "targetSpatialReference": "EPSG:3857",
    "scaleFactor": 1.0,
    "offsetX": 0,
    "offsetY": 0,
    "scaleFirst": false,
    "fileName": "historical_imagery.tif"
  }'
```

### 4) `POST /api/imagery/dump` (returns `.zip`)

Body parameters:

- All AOI fields from `availability`: `provider`, `noCache`, `zoom`, `parallel`, `region` or `lowerLeft`+`upperRight`
- `dates` (required): array of `yyyy-MM-dd`
- `exactDate`: bool
- `layerDate`: bool
- `targetSpatialReference`: string (optional)
- `formatter`: tile output file name template (optional)
- `writeWorldFile`: bool
- `archiveName`: string (optional)

```bash
curl -X POST "$BASE_URL/api/imagery/dump" \
  -H "Content-Type: application/json" \
  -o historical_tiles.zip \
  -d '{
    "provider": "TM",
    "zoom": 21,
    "noCache": false,
    "parallel": 2,
    "lowerLeft": { "latitude": 39.6299, "longitude": -104.8402 },
    "upperRight": { "latitude": 39.6301, "longitude": -104.8398 },
    "dates": ["2023-10-20"],
    "exactDate": false,
    "layerDate": false,
    "formatter": "z={Z}-Col={c}-Row={r}.jpg",
    "writeWorldFile": false,
    "archiveName": "tiles.zip"
  }'
```

### Common errors

`400 Bad Request`: invalid input (`location`, polygon points, date list, etc.).

`500 Internal Server Error`: upstream fetch/cache runtime error, response body includes error details.
