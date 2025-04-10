using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using EtlService.Application.Interfaces;
using EtlService.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using EtlService.Infrastructure.Services;
using EtlService.Application.Configuration;
using Microsoft.Extensions.Options;

namespace EtlService.Tests;

public class ExtractServiceTests
{
    [Fact]
    public async Task ExtractDailyStockData_ReturnsStockRecords_WhenApiRespondsSuccessfully()
    {
        // Arrange
        var json = @"{
            ""Time Series (15min)"": {
                ""2025-04-09 20:00:00"": {
                    ""1. open"": ""140.0"",
                    ""2. high"": ""145.0"",
                    ""3. low"": ""139.0"",
                    ""4. close"": ""144.0"",
                    ""5. volume"": ""10000""
                }
            }
        }";

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        var optionsMock = new Mock<IOptions<AlphaVantageOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new AlphaVantageOptions
        {
            ApiKey = "demo"
        });

        var service = new ExtractService(httpClient, optionsMock.Object);

        // Act
        var result = await service.ExtractDailyStockData("AAPL", "15min");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().ContainKey("2025-04-09 20:00:00");
        result.Value["2025-04-09 20:00:00"].Open.Should().Be("140.0");
    }
}
