namespace VMAFComparisonTool;

/// <summary>
/// Represents the encoding process for a given preset and CRF value, as well as the VMAF computation process
/// that runs after the encoding finishes.
/// </summary>
public class EncodingPipeline
{
    public string Path { get; }
    public string Arguments { get; }
    public string Preset { get; }
    public int Crf { get; }

    private TaskCompletionSource TaskCompletionSource { get; } = new TaskCompletionSource();
    /// <summary>
    /// Returns a task that completes when the pipeline is successful.
    /// </summary>
    public Task Task => TaskCompletionSource.Task;

    /// <summary>
    /// Runs when the pipeline completes successfully. This is run immediately after the <see cref="Task"/> is marked complete.
    /// </summary>
    public event Action<EncodingPipeline>? PipelineCompleted;

    /// <summary>
    /// Do not pull this from external until the task completes.
    /// </summary>
    public EncodingResult Result { get; private set; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="path"></param>
    /// <param name="arguments">Don't include preset and CRF in the arguments list.</param>
    /// <param name="preset"></param>
    /// <param name="crf"></param>
    public EncodingPipeline(string path, string arguments, string preset, int crf)
    {
        Path = path;
        Arguments = arguments;
        Preset = preset;
        Crf = crf;
    }

    public async void Start()
    {
        while (!await Restart())
        {
            // If the encoding or VMAF extraction failed, try again.
        }

        TaskCompletionSource.SetResult();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>True if the encoding and VMAF extraction encoders are both successful and a VMAF score was obtained, false if something went wrong.</returns>
    private async Task<bool> Restart()
    {
        try
        {
            TaskCompletionSource<bool> s = new TaskCompletionSource<bool>();
            Result  = new EncodingResult {Preset = Preset, Crf = Crf};

            VideoEncoder encoder = new VideoEncoder(Path);
            encoder.ProcessExited += videoEncoder =>
            {
                Result.Size = new FileInfo(videoEncoder.OutputFilePath).Length;

                VideoEncoder vmafEncoder = new VideoEncoder(Path);
                vmafEncoder.ProcessExited += vmafVideoEncoder =>
                {
                    // Don't know why, but sometimes the VMAF score does not get logged when it's done.
                    // In that case, return false so that we can redo it.
                    if (vmafVideoEncoder.VMAFScore == null)
                    {
                        s.SetResult(false);
                        return;
                    }
                    Result.VMAFScore = vmafVideoEncoder.VMAFScore.Value;
                    Result.ProcessorTime = vmafVideoEncoder.ProcessorTime;
                    s.SetResult(true);
                };

                vmafEncoder.StartVMAF(videoEncoder.OutputFilePath);
            };

            string args = $"{Arguments} -preset {Preset} -crf {Crf}";
            encoder.StartEncoding(args, $"{Preset}_{Crf}.mkv");

            await s.Task;
            PipelineCompleted?.Invoke(this);
            return s.Task.Result;
        }
        catch (Exception e)
        {
            // If an error happened, return false so caller can try again if need be.
            // Don't know if this actually ever gets called, as errors in events are not caught here.
            return false;
        }
    }
}