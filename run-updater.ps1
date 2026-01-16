Set-Location "D:\VSCODE_PROJECTS\stock-app\docker"

# Run updater container
docker compose run --rm updater `
  | Tee-Object -FilePath "..\logs\updater.log" -Append
