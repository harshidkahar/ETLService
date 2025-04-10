
using ErrorOr;

namespace EtlService.Application.Common.Errors;

public static class ErrorCodes
{
    public static class Extract
    {
        public static Error AlphaVantageUnavailable => Error.Failure("Extract.AlphaVantageUnavailable", "Unable to fetch data from Alpha Vantage.");
        public static Error ParsingFailed => Error.Failure("Extract.ParsingFailed", "Failed to parse API response.");
        public static Error NoTimeSeries => Error.NotFound("Extract.NoTimeSeries", "Time series data not found in the API response.");
    }
}
