# VMAFComparisonTool
VMAFComparisonTool is a cross-platform command-line application that leverages the powerful FFmpeg suite and Netflix's VMAF tool to calculate the perceptual quality between an original video and one encoded with various presets and CRF values.

# Compiling
This program can be compiled using Microsoft Visual Studio or Jetbrains Rider. You will need the .NET SDK and runtime to be installed.

1. Restore the project using `dotnet restore`.
2. Build with `dotnet build` to create a debug build, or `dotnet build -c Release` for a release build. Builds created with this method will require .NET to be installed on the computer to run.

## Publish
Publishing allows for builds that work on systems without .NET installed. All publish builds are release builds by default.

1. Restore the project using `dotnet restore`.
2. Publish with `dotnet publish -r <runtime id> --self-contained <true|false> -p:PublishSingleFile=<true|false> -p:PublishTrimmed=<true|false>`. For example,

```
dotnet publish -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```
