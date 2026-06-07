# MarketRadar

MarketRadar is a .NET 8 console app that builds a daily market risk report from macro, equity, currency, volatility, crypto, and cross-asset signals. It can print the report locally and send it to a Discord channel through a webhook.

MarketRadar 是一個 .NET 8 Console 專案，用來產生每日市場風險雷達報告。它會整合股市、美元、殖利率曲線、波動率、半導體、黃金、BTC 與匯率訊號，並可透過 Discord Webhook 自動發送報告。

## Features / 功能

- Nasdaq short-term trend: 1D, 3D, 5D, momentum
- DXY trend and risk-off confirmation
- FRED 10Y-2Y yield spread
- VIX volatility / fear signal
- SOX and TSM ADR for semiconductor and Taiwan market context
- Gold and BTC cross-asset sentiment checks
- USD/TWD currency pressure check
- Regime, risk, stress, score, previous score, and score change
- Chinese market interpretation and analysis
- Discord webhook delivery
- GitHub Actions scheduled execution

## Data Sources / 資料來源

- Yahoo Finance chart API
  - `^IXIC` Nasdaq Composite
  - `DX-Y.NYB` DXY
  - `^VIX` VIX
  - `^SOX` Philadelphia Semiconductor Index
  - `TSM` TSMC ADR
  - `GC=F` Gold futures
  - `BTC-USD` Bitcoin
  - `TWD=X` USD/TWD
- FRED API
  - `DGS10`
  - `DGS2`

## Configuration / 設定

`appsettings.json` is safe to commit and should only contain empty sensitive values:

`appsettings.json` 可以 commit，但敏感值必須保持空白：

```json
{
  "FredApiKey": "",
  "DiscordSetting": {
    "WebhookUrl": ""
  }
}
```

For local development, create `appsettings.Development.json` in the project root:

本機測試請在專案根目錄建立 `appsettings.Development.json`：

```json
{
  "FredApiKey": "your-fred-api-key",
  "DiscordSetting": {
    "WebhookUrl": "your-discord-webhook-url"
  }
}
```

`appsettings.Development.json` is ignored by Git and must not be committed.

`appsettings.Development.json` 已被 `.gitignore` 忽略，請不要 commit。

## Run Locally / 本機執行

Using the launch profile:

使用 launch profile：

```powershell
dotnet run --launch-profile MarketRadar
```

Or set the environment manually:

或手動設定環境：

```powershell
$env:DOTNET_ENVIRONMENT="Development"
dotnet run
```

## GitHub Actions / GitHub 自動排程

The workflow is defined in:

排程檔案位於：

```text
.github/workflows/market-radar.yml
```

Current schedule:

目前排程：

```yaml
cron: "0 0 * * 1-5"
```

This runs Monday to Friday at 00:00 UTC, which is 08:00 Taiwan time.

這代表週一到週五 UTC 00:00 執行，也就是台灣時間 08:00。

Add these GitHub Actions secrets:

請在 GitHub Actions Secrets 加入：

```text
FRED_API_KEY
DISCORD_WEBHOOK_URL
```

Path:

設定路徑：

```text
GitHub Repository > Settings > Secrets and variables > Actions
```

## Discord Webhook / Discord 發送

Create a webhook in the Discord channel where you want to receive reports:

在要接收報告的 Discord 頻道建立 Webhook：

```text
Channel Settings > Integrations > Webhooks > New Webhook
```

Copy the webhook URL into:

將 Webhook URL 放到：

- Local: `appsettings.Development.json`
- GitHub Actions: `DISCORD_WEBHOOK_URL` secret

## Security Notes / 安全注意事項

- Do not commit real API keys.
- Do not commit real Discord webhook URLs.
- If a key or webhook was ever committed or shared, rotate it.
- Keep `appsettings.json` as a safe template only.
- Use `appsettings.Development.json` locally and GitHub Secrets in Actions.

請務必注意：

- 不要 commit 真實 API key。
- 不要 commit 真實 Discord webhook。
- 如果 key 或 webhook 曾經被推上 GitHub 或貼出來，請重新產生。
- `appsettings.json` 只保留安全範本。
- 本機用 `appsettings.Development.json`，GitHub Actions 用 Secrets。

## Disclaimer / 免責聲明

MarketRadar is a risk-monitoring and decision-support tool. It does not predict prices and should not be treated as financial advice.

MarketRadar 是市場風險監控與決策輔助工具，不是價格預測模型，也不構成投資建議。
