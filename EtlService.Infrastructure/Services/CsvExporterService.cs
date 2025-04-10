using CsvHelper.Configuration;
using CsvHelper;
using EtlService.Application.Interfaces;
using EtlService.Domain.Entities;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using EtlService.Application.Configuration;
using Microsoft.Extensions.Options;

namespace EtlService.Infrastructure.Services
{
    public class CsvExporterService : ICsvExporter
    {
        private readonly string _baseDirectory;

        public CsvExporterService(IOptions<CsvExportOptions> options)
        {
            _baseDirectory = options.Value.ExportPath;
        }
        public string SaveToCsv(IEnumerable<StockRecord> data, string symbol, DateTime date)
        {
            if (data == null || !data.Any())
                return "NoData";

            string folderPath = Path.Combine(_baseDirectory, symbol.ToUpper(), date.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(folderPath);

            // Create the file name with the symbol and date
            string fileName = $"{symbol.ToUpper()}_{date:yyyy-MM-dd}.csv";
            string fullPath = Path.Combine(folderPath, fileName);

            using var writer = new StreamWriter(fullPath);
            writer.WriteLine("Timestamp,Open,High,Low,Close,Volume");

            foreach (var entry in data)
            {
                writer.WriteLine($"{entry.Date:yyyy-MM-dd HH:mm:ss},{entry.Open},{entry.High},{entry.Low},{entry.Close},{entry.Volume}");
            }
            return fullPath;
        }
    }
}
