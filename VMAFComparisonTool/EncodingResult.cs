namespace VMAFComparisonTool;

public class EncodingResult
{
    public string Preset { get; set; }
    public int Crf { get; set; }
    public double VMAFScore { get; set; }
    /// <summary>
    /// Output file size in bytes.
    /// </summary>
    public long Size { get; set; }
    public TimeSpan ProcessorTime { get; set; }
}