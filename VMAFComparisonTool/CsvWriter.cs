using System.Text;

namespace VMAFComparisonTool;

public class CsvWriter
{
    /// <summary>
    /// Write the encoding results to a CSV file.
    /// </summary>
    /// <param name="path">The path to write to. Subdirectories in the path must exist.</param>
    /// <param name="data">The list of results to write to the file in CSV format.</param>
    public static void Write(string path, List<EncodingResult> data)
    {
        StringBuilder sb = new StringBuilder();
        // Write header
        sb.AppendLine("Order,Preset,CRF,VMAF,Size,Processor Time");

        for (int i = 0; i < data.Count; i++)
        {
            EncodingResult result = data[i];
            sb.AppendLine($"{i},{result.Preset},{result.Crf},{result.VMAFScore},{result.Size},{result.ProcessorTime}");
        }

        File.WriteAllText(path, sb.ToString());
    }
}