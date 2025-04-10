using EtlService.Application.Configuration;
using EtlService.Application.Interfaces;
using EtlService.Domain.Entities;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EtlService.Infrastructure.Services;

public class JsonDownloadTracker : IDownloadTracker
{
    private readonly string _logFilePath;
    private List<DownloadLogEntry> _entries;

    public JsonDownloadTracker(IOptions<DownloadTrackerOptions> options)
    {
        _logFilePath = options.Value.LogFilePath;

        _entries = File.Exists(_logFilePath)
            ? JsonSerializer.Deserialize<List<DownloadLogEntry>>(File.ReadAllText(_logFilePath))
            : new List<DownloadLogEntry>();
    }

    public bool HasAlreadyDownloaded(string symbol, DateTime date, string interval)
    {
        return _entries.Any(e => e.Symbol == symbol && e.Date.Date == date.Date && e.Interval == interval);
    }

    public void MarkAsDownloaded(string symbol, DateTime date, string interval)
    {
        _entries.Add(new DownloadLogEntry { Symbol = symbol, Date = date.Date, Interval = interval });
        File.WriteAllText(_logFilePath, JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
    }
}

