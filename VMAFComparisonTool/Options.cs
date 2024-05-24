using CommandLine;

namespace VMAFComparisonTool;

public class Options
{
    [Option('v', "verbose", Required = false, HelpText = "Prints all messages to standard output.")]
    public bool Verbose { get; set; }
}