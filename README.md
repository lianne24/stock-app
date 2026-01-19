# Stock Market Data Visualization Platform

This project is a full-stack stock market analysis platform that ingests real-world financial data, stores it in MySQL, exposes it through an ASP.NET REST API, and visualizes it in an interactive React candlestick chart.

The system includes an automated daily data pipeline, public HTTPS deployment on Azure using Docker and nginx, and an extensible architecture designed to support advanced wave analysis, Fibonacci-based confirmations, and predictive “beauty” scoring as defined in the project specifications.

# Live Demo

Frontend:
https://myappssite.com/

API Example:
https://myappssite.com/api/stocks/symbols

# Architecture Overview

                Internet
                    |
             Cloudflare DNS
                    |
            Azure VM (Ubuntu)
                    |
              Nginx Reverse Proxy
            ┌────────────┬────────────┐
            │            │            │
        React Frontend   ASP.NET API   Certbot
            │            │
            └─────── MySQL (Docker) ───────┘
                    |
              Daily Updater (cron)

Components
- Frontend: React + Vite, served as static site via nginx
- Backend API: ASP.NET Minimal API (C#)
- Database: MySQL 8 (Docker volume)
- Updater: C# console app that fetches stock data daily
- Reverse Proxy: nginx
- HTTPS: Let’s Encrypt (certbot in Docker)
- Hosting: Azure Ubuntu VM

# Features
- Daily ingestion of OHLCV stock data from Alpha Vantage
- Incremental updates (no duplicate rows)
- REST API:
  - List symbols
  - Query price history by symbol, timeframe, date range
  - Date range metadata for UI
- Interactive candlestick chart (React + lightweight-charts)
- Peaks & valleys detection (N=3 smoothing)
- Public HTTPS deployment
- Automated daily updater (cron)

# Local Development Setup
1. Backend API (local)
~cd backend/StockApi
~dotnet run

Runs on: http://localhost:8080

2. Frontend (local)
~cd frontend
~npm install
~npm run dev

Runs on: http://localhost:5173

Set in frontend/.env.local:
VITE_API_BASE=http://localhost:8080/api

# Production Deployment (Azure VM)
Services (Docker Compose):
- mysql
- stockapi
- frontend
- reverse-proxy (nginx)
- updater (manual / scheduled profile)

Start production stack:
~cd docker
~docker compose -f docker-compose.prod.yml up -d --build

HTTPS (Encryption)
Certificates are issued using certbot in Docker and mounted into nginx.

Certificate paths:
/etc/letsencrypt/live/myappssite.com/fullchain.pem
/etc/letsencrypt/live/myappssite.com/privkey.pem

nginx redirects all HTTP → HTTPS.

Auto-renew cron:
30 3 * * * docker run --rm \
  -v /home/azureuser/stock-app/docker/certbot/www:/var/www/certbot \
  -v /home/azureuser/stock-app/docker/certbot/conf:/etc/letsencrypt \
  certbot/certbot:v2.10.0 renew --webroot -w /var/www/certbot && \
  docker exec stock_proxy nginx -s reload >> /home/azureuser/certbot-renew.log 2>&1

# Daily Stock Updater (cron)
Runs every day at 11:00 PM EST.

Cron entry:
0 23 * * * flock -n /tmp/stock-updater.lock \
cd /home/azureuser/stock-app/docker && \
docker compose -f docker-compose.prod.yml --profile manual run --rm updater >> \
/home/azureuser/stock-updater.log 2>&1

Logs:
/home/azureuser/stock-updater.log

# API Endpoints
List symbols
~GET /api/stocks/symbols

Get available date range
~GET /api/stocks/range?symbol=AAPL&timeframe=D

Get price history
~GET /api/stocks/prices?symbol=AAPL&timeframe=D&from=2025-01-01&to=2025-12-31&limit=1500

Timeframes:
D = Daily
W = Weekly (server-side aggregation)
M = Monthly (server-side aggregation)

Chart Features
- Candlestick OHLC rendering
- Automatic Y-axis normalization (5% padding above/below)
- Crosshair + tooltip
- Peak (P) and Valley (V) markers with toggle
- Server-side weekly/monthly aggregation

# Environment Variables
Production secrets stored in docker/.env (not committed):

MYSQL_ROOT_PASSWORD=...
MYSQL_DATABASE=stockdb
MYSQL_USER=stockuser
MYSQL_PASSWORD=...

MYSQL_CONNECTION_STRING=Server=mysql;Port=3306;Database=stockdb;User=stockuser;Password=...

ALPHAVANTAGE_API_KEY=...

VITE_API_BASE=/api

# Future Work (Part 2 of Project)
The next phase of this project will extend the platform into a full wave-analysis and prediction engine. Users will be able to interactively select valid price waves on the chart, compute Fibonacci levels and confirmation patterns, and evaluate a custom “Beauty(price)” function to study how well high-beauty zones predict future highs and lows. These phases will enable quantitative evaluation of predictive accuracy.

# Author
Lianne Lamorena
B.S. Computer Science Engineering – University of South Florida
IT Specialist | Systems Analyst @ Ntiva
Interests: Full-stack systems, cloud, data pipelines, financial analytics
