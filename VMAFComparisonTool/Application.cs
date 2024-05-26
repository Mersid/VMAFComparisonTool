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
        List<EncodingResult> results = [];

        // List of tasks to track when the program can exit.
        List<Task> tasks = [];
        Stopwatch sw = new Stopwatch();
        sw.Start();

        // TEST
        // encodingSettingsQueue.Clear();
        // encodingSettingsQueue.Enqueue(new EncodingSettings {Preset = "veryfast", Crf = 23});
        // VideoEncoder test;

        while (encodingSettingsQueue.Count > 0)
        {
            await semaphore.WaitAsync();
            EncodingSettings encodingSettings = encodingSettingsQueue.Dequeue();
            EncodingResult result = new EncodingResult { Settings = encodingSettings };
            results.Add(result);
            VideoEncoder videoEncoder = new VideoEncoder(options.InputPath);
            videoEncoder.ProcessExited += (sender) =>
            {
                // TODO: What if exit with fail code?
                semaphore.Release();

                result.Size = new FileInfo(sender.OutputFilePath).Length;

                VideoEncoder encoder2 = new VideoEncoder(options.InputPath);
                encoder2.ProcessExited += (sender) =>
                {
                    // encodingSettings obtained by closure.
                    result.VMAFScore = sender.VMAFScore.Value;
                    result.ProcessorTime = sender.ProcessorTime;
                };
                Task t2 = encoder2.StartVMAF(sender.OutputFilePath);
                tasks.Add(t2);
                // test = encoder2;
            };

            // TODO: Improve this. Bit of a hack job...
            string args = $"{options.Arguments.Substring(1, options.Arguments.Length - 2)} -preset {encodingSettings.Preset} -crf {encodingSettings.Crf}";
            Task t = videoEncoder.StartEncoding(args, $"_{Path.GetFileNameWithoutExtension(options.OutputPath)}_{encodingSettings.Preset}_{encodingSettings.Crf}.mkv");
            tasks.Add(t);

            Console.WriteLine($"Encoding {encodingSettings.Preset} {encodingSettings.Crf}");
        }

        Task.WaitAll(tasks.ToArray());
        // When the number of videos to encode is less than the number of parallel encodes, the above wait
        // may only wait for all the videos to finish encoding, not the VMAF calculation. This is because
        // that line of code is hit before all the VMAF calculation task is added to the list of tasks. Tasks
        // added after the wait is called will not be counted. This is why we need to wait again here.
        // Since the VMAF task is generated before the encoding task is complete, calling the wait again
        // will ensure that any VMAF tasks are also awaited.
        Task.WaitAll(tasks.ToArray());

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