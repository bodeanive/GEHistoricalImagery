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
- `validate.yml` (push/PR compile checks)
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

## Request examples

### info

```bash
curl "http://localhost:35001/api/imagery/info?location=39.63,-104.84&zoom=21&provider=TM"
```

### availability

```json
{
  "provider": "TM",
  "zoom": 20,
  "region": [
    { "latitude": 39.63, "longitude": -104.84 },
    { "latitude": 39.64, "longitude": -104.84 },
    { "latitude": 39.64, "longitude": -104.83 }
  ],
  "completeOnly": false
}
```

### download

```json
{
  "provider": "TM",
  "zoom": 20,
  "region": [
    { "latitude": 39.63, "longitude": -104.84 },
    { "latitude": 39.64, "longitude": -104.84 },
    { "latitude": 39.64, "longitude": -104.83 }
  ],
  "dates": ["2023-10-20"],
  "exactDate": false,
  "layerDate": false,
  "fileName": "historical_imagery.tif"
}
```

### dump

```json
{
  "provider": "TM",
  "zoom": 20,
  "region": [
    { "latitude": 39.63, "longitude": -104.84 },
    { "latitude": 39.64, "longitude": -104.84 },
    { "latitude": 39.64, "longitude": -104.83 }
  ],
  "dates": ["2023-10-20"],
  "formatter": "z={Z}-Col={c}-Row={r}.jpg",
  "writeWorldFile": false,
  "archiveName": "tiles.zip"
}
```
