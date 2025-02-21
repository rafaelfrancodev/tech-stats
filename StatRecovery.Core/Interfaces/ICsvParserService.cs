namespace StatRecovery.Core.Interfaces
{
    public interface ICsvParserService
    {
        Dictionary<string, string> ParseCsv(Stream csvStream);
    }
}
