using EtlService.Application.Configuration;
using EtlService.Application.Interfaces;
using EtlService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace EtlService.Tests;

public class JsonDownloadTrackerTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly JsonDownloadTracker _tracker;

    public JsonDownloadTrackerTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"DownloadLog_{Guid.NewGuid()}.json");
        var optionsMock = new Mock<IOptions<DownloadTrackerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new DownloadTrackerOptions
        {
            LogFilePath = _testFilePath
        });

        _tracker = new JsonDownloadTracker(optionsMock.Object);

    }

    [Fact]
    public void HasAlreadyDownloaded_ReturnsFalse_WhenNotMarked()
    {
        // Act
        bool result = _tracker.HasAlreadyDownloaded("AAPL", DateTime.UtcNow.Date, "15min");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MarkAsDownloaded_ThenHasAlreadyDownloaded_ReturnsTrue()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;

        // Act
        _tracker.MarkAsDownloaded("AAPL", date, "15min");
        bool result = _tracker.HasAlreadyDownloaded("AAPL", date, "15min");

        // Assert
        result.Should().BeTrue();
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
