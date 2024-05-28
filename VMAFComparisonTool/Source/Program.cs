// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using VMAFComparisonTool;

ParserResult<Options>? result = Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
{
    Application application = new Application();
    application.Run(options);
});
