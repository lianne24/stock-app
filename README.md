# stock-app

A full-stack stock analysis app with a data pipeline and web UI.

## Status (what’s done so far)

### ✅ Data pipeline (working)
The project currently has a working ingestion pipeline that:
- Runs **MySQL in Docker**
- Creates the schema automatically on first boot (`docker/init/01_schema.sql`)
- Runs a **C# .NET StockUpdater** that:
  - Calls Alpha Vantage time-series endpoints (Daily currently)
  - Parses JSON to OHLCV rows
  - Upserts data into MySQL using a unique key (idempotent)
  - Supports incremental updates by inserting only dates newer than what’s already stored

### ✅ Local “daily scheduler” (Windows) - will be implemented in a VM in the future
The updater is scheduled to run daily on a Windows machine using **Windows Task Scheduler**, executing:
- `docker compose run --rm updater`
and appending logs to `logs/updater.log`.

## Repository structure (current)
- `docker/`  
  Docker Compose services + MySQL init scripts
- `backend/StockUpdater/`  
  C# updater batch job (fetch → parse → upsert)

## Quick start (data pipeline)
1) Start MySQL:
```bash
cd docker
docker compose up -d mysql
