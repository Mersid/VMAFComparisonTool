using CommandLine;
using JetBrains.Annotations;

namespace VMAFComparisonTool;

[UsedImplicitly]
public class Options
{
    [Option('i', "input", Required = true, HelpText = "Path to the input video file.")]
    public string InputPath { get; set; } = string.Empty;

    [Option('o', "output", Required = true, HelpText = "Path to the results file, in CSV format.")]
    public string OutputPath { get; set; } = string.Empty;

    [Option('p', "parallel", Required = false, HelpText = "Number of parallel encodes to run. Default is 1.")]
    public int Parallel { get; set; } = 1;

    [Option('a', "arguments", Required = false,
        HelpText = "Additional arguments to pass to the encoder. By default, this is \"-c:v libx265 -c:a libopus\".\n" +
                   "NOTE: The entire argument string must be in quotes when passed in to the program!")]
    public string Arguments { get; set; } = "\"-c:v libx265 -c:a libopus\"";
}