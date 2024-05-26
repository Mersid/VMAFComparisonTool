namespace VMAFComparisonTool;

public class EncodingResult
{
    public EncodingSettings Settings { get; set; }

    public string Preset => Settings.Preset;
    public int Crf => Settings.Crf;
    public double VMAFScore { get; set; }
    /// <summary>
    /// Output file size in bytes.
    /// </summary>
    public long Size { get; set; }
    public TimeSpan ProcessorTime { get; set; }
}