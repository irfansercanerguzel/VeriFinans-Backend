namespace VeriFinans.DTOs
{
    public class AiAnalyzeRequestDto
    {
        public string ActionType { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string DataJson { get; set; } = string.Empty;
    }
}