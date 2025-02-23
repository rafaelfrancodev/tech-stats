using StatRecovery.Core.Interfaces;

namespace StatRecovery.Core.Services
{
    public class CsvParserService : ICsvParserService
    {
        public Dictionary<string, string> ParseCsv(Stream csvStream)
        {
            var pdfToPoMapping = new Dictionary<string, string>();

            using var reader = new StreamReader(csvStream);
            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
                throw new InvalidDataException("The CSV file is empty or corrupt.");

            var headers = headerLine.Split('~');

            var attachmentListIndex = Array.FindIndex(headers, h => h.Trim().Equals("Attachment List", StringComparison.OrdinalIgnoreCase));
            var poNumberIndex = Array.FindIndex(headers, h => h.Trim().Equals("PO Number", StringComparison.OrdinalIgnoreCase));

            if (attachmentListIndex == -1 || poNumberIndex == -1)
                throw new InvalidDataException("The CSV file does not contain the required columns.");

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split('~');

                if (columns.Length <= Math.Max(attachmentListIndex, poNumberIndex))
                {
                    Console.WriteLine($"Invalid row (not enough columns): {line}");
                    continue;
                }

                var poNumber = columns[poNumberIndex].Trim();
                var attachmentList = columns[attachmentListIndex].Trim();

                if (string.IsNullOrEmpty(attachmentList)) continue;
                var filePaths = attachmentList.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var filePath in filePaths)
                {
                    var fileName = Path.GetFileName(filePath.Trim());
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        pdfToPoMapping[fileName] = poNumber;
                    }
                }
            }

            return pdfToPoMapping;
        }
    }
}
