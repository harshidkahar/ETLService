namespace EtlService.Application.Interfaces;

public interface IDownloadTracker
{
    bool HasAlreadyDownloaded(string symbol, DateTime date, string interval);
    void MarkAsDownloaded(string symbol, DateTime date, string interval);
}
