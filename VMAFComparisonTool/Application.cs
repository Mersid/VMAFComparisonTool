using System.Diagnostics;

namespace VMAFComparisonTool;

public class Application
{
    public void Run(Options options)
    {
        Task.Run(async () => await RunAsync(options)).Wait();
    }

    private async Task RunAsync(Options options)
    {
        SemaphoreSlim semaphore = new SemaphoreSlim(options.Parallel);
        Queue<EncodingSettings> encodingSettingsQueue = new Queue<EncodingSettings>(GetEncodingSettings());
        Stopwatch sw = new Stopwatch();
        sw.Start();

        // TEST
        encodingSettingsQueue.Clear();
        encodingSettingsQueue.Enqueue(new EncodingSettings {Preset = "veryfast", Crf = 23});

        while (encodingSettingsQueue.Count > 0)
        {
            await semaphore.WaitAsync();
            EncodingSettings encodingSettings = encodingSettingsQueue.Dequeue();
            VideoEncoder videoEncoder = new VideoEncoder(options.InputPath);
            videoEncoder.ProcessExited += (sender) =>
            {
                // TODO: What if exit with fail code?
                semaphore.Release();

                VideoEncoder encoder2 = new VideoEncoder(sender.OutputFilePath);
            };

            // TODO: Improve this. Bit of a hack job...
            string args = $"{options.Arguments.Substring(1, options.Arguments.Length - 2)} -preset {encodingSettings.Preset} -crf {encodingSettings.Crf}";
            videoEncoder.Start(args, $"_{Path.GetFileNameWithoutExtension(options.OutputPath)}_{encodingSettings.Preset}_{encodingSettings.Crf}.mkv");

            Console.WriteLine($"Encoding {encodingSettings.Preset} {encodingSettings.Crf}");
        }

        // TODO: Wait for all encodes to finish
        sw.Stop();
        Console.WriteLine($"Encoding took {sw.Elapsed}");
    }

    private IEnumerable<EncodingSettings> GetEncodingSettings()
    {
        string[] presets = ["ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "placebo"];
        int maxCrf = 51;

        foreach (string preset in presets)
        {
            for (int crf = 0; crf <= maxCrf; crf++)
            {
                yield return new EncodingSettings { Preset = preset, Crf = crf };
            }
        }
    }

}