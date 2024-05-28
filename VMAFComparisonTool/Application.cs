﻿using System.Diagnostics;

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

        // Parser requires the entire set of arguments to be quoted. We need to remove it before passing it
        // on to the pipeline.
        string unquotedArguments = options.Arguments.Substring(1, options.Arguments.Length - 2);

        // List of pipelines we'll need for this job.
        List<EncodingPipeline> pipelines =
        [
            ..GetEncodingSettings().Select(settings =>
                new EncodingPipeline(options.InputPath, unquotedArguments, settings.Preset, settings.Crf))
        ];

        // Queue of pipelines that have yet to run. Will always be a subset of pipelines.
        Queue<EncodingPipeline> pipelineQueue = new Queue<EncodingPipeline>(pipelines);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        // TEST
        // encodingSettingsQueue.Clear();
        // encodingSettingsQueue.Enqueue(new EncodingSettings {Preset = "veryfast", Crf = 23});
        // VideoEncoder test;

        while (pipelineQueue.Count > 0)
        {
            await semaphore.WaitAsync();

            EncodingPipeline pipeline = pipelineQueue.Dequeue();
            pipeline.PipelineCompleted += _ =>
            {
                semaphore.Release();
            };
            pipeline.Start();

            Console.WriteLine($"Encoding {pipeline.Preset} {pipeline.Crf}");
        }

        Task[] tasks = pipelines.Select(pipeline => pipeline.Task).ToArray();
        Task.WaitAll(tasks);

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