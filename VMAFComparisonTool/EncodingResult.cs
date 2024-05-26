namespace VMAFComparisonTool;

public class EncodingResult
{
    public EncodingSettings Settings { get; set; }
    public double VMAFScore { get; set; }
    /// <summary>
    /// Output file size in bytes.
    /// </summary>
    public long Size { get; set; }
}