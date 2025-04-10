# 🧪 C# ETL Service for Stock Market Data

This project implements a Clean Architecture-based ETL pipeline in C# that extracts intraday stock market data from the Alpha Vantage API, transforms and saves it as CSV files, and optionally handles backfilling and queuing via Azure Service Bus.

---

## 🚀 Features

- ✅ Extract 15-minute interval stock data using Alpha Vantage API
- ✅ Clean Architecture (Domain, Application, Infrastructure, API)
- ✅ Save transformed data to CSV (organized by symbol and date)
- ✅ Prevent duplicate downloads using a JSON-based tracker
- ✅ Backfill data for the last 30 days
- ✅ Queue-based ETL processing using Azure Service Bus
- ✅ Logging with Serilog
- ✅ Error handling with ErrorOr
- ✅ Unit tests using xUnit, Moq, FluentAssertions

---

## 📁 Project Structure

```
EtlService.sln
│
├── EtlService.API              # API project (entry point)
├── EtlService.Application      # Interfaces, models, use cases
├── EtlService.Infrastructure   # Services, external systems (CSV, Service Bus)
├── EtlService.Domain           # Entities
├── EtlService.Tests            # Unit tests
```

---

## ⚙️ Configuration

Update the `appsettings.json` in the `API` project:

```json
"AlphaVantage": {
  "ApiKey": "your-alpha-vantage-api-key",
  "Interval": "15min"
},
"CsvExportPath": "C:\\ETLShared\\StockData",
"AzureServiceBus": {
  "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxx",
  "QueueName": "etlqueue"
}
```

---

## 📦 NuGet Packages Used

- `Azure.Messaging.ServiceBus`
- `Serilog.AspNetCore`
- `ErrorOr`
- `xUnit`, `FluentAssertions`, `Moq`

---

## 🧪 Running the Project

### 1. Run API
```bash
dotnet run --project EtlService.API
```

### 2. Trigger ETL from API

- **Immediate execution**:  
  `POST /api/etl/{symbol}`

- **Queued via Azure Service Bus**:  
  `POST /api/etl/queue/{symbol}`

### 3. Process Queued Messages

The `EtlQueueConsumer` runs as a background service and processes queued messages.

### 4. Run Unit Tests
```bash
dotnet test EtlService.Tests
```

---

## 🧠 How It Works

1. API receives ETL request
2. If queued, it publishes the request to Azure Service Bus
3. Background worker consumes the message
4. Extracts, transforms, and saves the data as CSV
5. Tracks completed jobs to avoid duplication

---

## 📋 Backfill Strategy

- Automatically processes past 30 days
- Skips already processed days
- Honors Alpha Vantage rate limit with `Task.Delay(15000)`

---

## 📈 Next Steps (optional)

- [ ] Add retry queue for failed jobs
- [ ] Add dashboard to view ETL status
- [ ] Add SQLite or Azure Table for persistent tracking
- [ ] Add Hangfire or Quartz for scheduled ETL

---

## 🙋‍♂️ About Me

- 🔗 [LinkedIn](https://www.linkedin.com/in/harshidkahar/)
- 🌐 [Website](https://harshidkahar.com/)

## 👨‍💻 Contributing

Feel free to fork and contribute! Open issues and PRs are welcome.

---

## 📄 License

MIT © Harshid Kahar
