using EtlService.Application.Interfaces;
using EtlService.Application.Services;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EtlService.Domain.Entities;

namespace EtlService.Tests;

public class BackfillServiceTests
{
    [Fact]
    public async Task BackfillLast30DaysAsync_SkipsDates_AlreadyDownloaded()
    {
        // Arrange
        var mockExtractService = new Mock<IExtractService>();
        var mockTracker = new Mock<IDownloadTracker>();
        var mockExporter = new Mock<ICsvExporter>();
        var mockLogger = new Mock<ILogger<BackfillService>>();

        var fakeData = Enumerable.Range(1, 3).Select(i => new StockRecord
        {
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Open = "100",
            High = "110",
            Low = "95",
            Close = "105",
            Volume = "10000"
        }).ToList();

        var dataDict = new Dictionary<string, StockRecord>
        {
            { DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), fakeData.First() }
        };

        mockExtractService.Setup(x => x.ExtractDailyStockData(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(ErrorOrFactory.From(dataDict));

        mockTracker.Setup(x => x.HasAlreadyDownloaded(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .Returns(true); // Simulate all dates already downloaded

        var service = new BackfillService(
            mockExtractService.Object,
            mockTracker.Object,
            mockExporter.Object,
            mockLogger.Object);

        // Act
        await service.BackfillLast30DaysAsync("AAPL", "15min");

        // Assert
        mockExporter.Verify(x => x.SaveToCsv(It.IsAny<IEnumerable<StockRecord>>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        mockTracker.Verify(x => x.MarkAsDownloaded(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BackfillLast30DaysAsync_SavesAndTracksData_WhenNotAlreadyDownloaded()
    {
        // Arrange
        var mockExtractService = new Mock<IExtractService>();
        var mockTracker = new Mock<IDownloadTracker>();
        var mockExporter = new Mock<ICsvExporter>();
        var mockLogger = new Mock<ILogger<BackfillService>>();

        var today = DateTime.UtcNow.Date.AddDays(-1);

        var fakeRecord = new StockRecord
        {
            Date = today.ToString("yyyy-MM-dd") + " 15:00:00",
            Open = "100",
            High = "110",
            Low = "95",
            Close = "105",
            Volume = "10000"
        };

        var dataDict = new Dictionary<string, StockRecord>
        {
            { fakeRecord.Date, fakeRecord }
        };

        mockExtractService.Setup(x => x.ExtractDailyStockData(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(ErrorOrFactory.From(dataDict));

        mockTracker.Setup(x => x.HasAlreadyDownloaded(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .Returns(false);

        var service = new BackfillService(
            mockExtractService.Object,
            mockTracker.Object,
            mockExporter.Object,
            mockLogger.Object);

        // Act
        await service.BackfillLast30DaysAsync("AAPL", "15min");

        // Assert
        mockExporter.Verify(x => x.SaveToCsv(It.IsAny<IEnumerable<StockRecord>>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.AtLeastOnce);
        mockTracker.Verify(x => x.MarkAsDownloaded(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.AtLeastOnce);
    }
}
