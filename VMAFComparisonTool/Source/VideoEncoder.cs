using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace VMAFComparisonTool;

/// <summary>
/// Represents the encoder for a single video.
/// </summary>
public class VideoEncoder
{
    public string InputFilePath { get; }

    /// <summary>
    /// Null if the encoding process has not yet started or if the processing method
    /// does not generate an output. This is an absolute file path.
    /// </summary>
    public string? OutputFilePath { get; private set; }

    /// <summary>
    /// The VMAF score of the video. Null if the encoding process has not yet started
    /// or if the processing method does not generate a VMAF score.
    /// </summary>
    public double? VMAFScore { get; private set; }

    private bool IsVMAF { get; set; }
    public StringBuilder Log { get; } = new StringBuilder();

    /// <summary>
    /// A task that represents the encoding process.
    /// This task completes when the encoding process exits.
    /// </summary>
    private Task Task { get; init; }

    /// <summary>
    /// Null when Start() has not yet been called.
    /// </summary>
    private Process? Process { get; set; }

    /// <summary>
    /// Duration of the video in seconds.
    /// </summary>
    public double Duration { get; }
    public double CurrentDuration { get; private set; }
    public EncodingState State { get; private set; } = EncodingState.Pending;

    /// <summary>
    /// The time the processor spent encoding the video.
    /// Might be a bit more useful since we're parallel processing, but I dunno...
    /// </summary>
    public TimeSpan ProcessorTime { get; private set; }

    public event Action<VideoEncoder, DataReceivedEventArgs?>? InfoUpdate;
    public event Action<VideoEncoder>? ProcessExited;

    public VideoEncoder(string inputFilePath)
    {
        InputFilePath = inputFilePath;
        Task = new Task(() => { });

        Process probe = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Helpers.GetFFprobePath(),
                Arguments = $"-v quiet -print_format json -show_format \"{inputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        probe.Start();

        // Run ffprobe to get duration data
        StringBuilder probeOutput = new StringBuilder();
        while (!probe.StandardOutput.EndOfStream)
        {
            string? info = probe.StandardOutput.ReadLine();
            if (info != null)
                probeOutput.AppendLine(info);
        }

        // Json output is in probeOutput
        JsonDocument json = JsonDocument.Parse(probeOutput.ToString());

        // Copy probeOutput to Log for debugging, if we need it.
        Log.AppendLine(probeOutput.ToString());

        try
        {
            Duration = double.Parse(json.RootElement.GetProperty("format").GetProperty("duration").GetString() ?? throw new InvalidOperationException());
        }
        catch (Exception e)
        {
            Log.AppendLine(e.Message);
            State = EncodingState.Error;
        }
    }

    public Task StartEncoding(string ffmpegArguments, string outputFilePath)
    {
        string outputFilePathAbsolute = Path.Combine(Environment.CurrentDirectory, outputFilePath);
        string outputFileDirectory = Path.GetDirectoryName(outputFilePathAbsolute) ?? throw new InvalidOperationException();

        OutputFilePath = outputFilePathAbsolute;

        // Create the output directory if it doesn't exist
        if (!Directory.Exists(outputFileDirectory))
            Directory.CreateDirectory(outputFileDirectory);

        string arguments = $"-i \"{InputFilePath}\" -y {ffmpegArguments} \"{outputFilePathAbsolute}\"";
        return StartProcess(arguments);
    }

    public Task StartVMAF(string distortedPath)
    {
        IsVMAF = true;

        // Note that the filter reverses the order of the inputs. The filter expects the distorted video first, then the reference video.
        // By swapping them, we can put the reference video first, then the distorted video.
        string arguments = $"-i \"{InputFilePath}\" -i \"{distortedPath}\" " +
                           $"-filter_complex \"[0:v]setpts=PTS-STARTPTS[reference]; [1:v]setpts=PTS-STARTPTS[distorted]; [distorted][reference]libvmaf=model=version=vmaf_v0.6.1:n_threads={Environment.ProcessorCount}\" " +
                           $"-f null -";
        return StartProcess(arguments);
    }

    private Task StartProcess(string arguments)
    {
        if (State != EncodingState.Pending)
        {
            Log.AppendLine($"Cannot start encoding, state is not pending. Current state: {State}");

            // Return a completed task to prevent the caller from awaiting on a task that will never complete.
            Task.Start();
            return Task.CompletedTask;
        }

        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Helpers.GetFFmpegPath(),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true
        };

        State = EncodingState.Encoding;

        // Attach event handlers for the process
        Process.OutputDataReceived += OnStreamDataReceivedEvent;
        Process.ErrorDataReceived += OnStreamDataReceivedEvent;

        Process.Start();

        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();

        Process.Exited += OnProcessExited;

        return Task;
    }

    /// <summary>
    /// Raised when the underlying ffmpeg process exits.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnProcessExited(object? sender, EventArgs args)
    {
        Debug.Assert(Process != null, nameof(Process) + " != null");
        Process.WaitForExit(); // TODO: THIS!!!

        State = Process.ExitCode == 0 ? EncodingState.Success : EncodingState.Error;
        ProcessorTime = Process.TotalProcessorTime;

        Log.AppendLine($"Process exited with code {Process.ExitCode}");

        InfoUpdate?.Invoke(this, null);
        ProcessExited?.Invoke(this);

        Process.OutputDataReceived -= OnStreamDataReceivedEvent;
        Process.ErrorDataReceived -= OnStreamDataReceivedEvent;
        Process.Exited -= OnProcessExited;

        // Mark the task as completed.
        Task.Start();
        Task.Wait();
    }

    /// <summary>
    /// Fired when data is written by the underlying ffmpeg process.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnStreamDataReceivedEvent(object sender, DataReceivedEventArgs args)
    {
        if (State != EncodingState.Encoding)
            return;

        // Extract timestamp
        if (args.Data != null && args.Data.Contains("time="))
        {
            string time = args.Data.Split("time=")[1].Split(" ")[0];

            try
            {
                // Sometimes time is N/A. We don't need to worry, since it's likely that the video is done.
                CurrentDuration = TimeSpan.Parse(time).TotalSeconds;
            }
            catch (Exception e)
            {
                Log.AppendLine(e.Message);
            }
        }

        if (IsVMAF)
        {
            int t = 8;
        }

        // Try extract VMAF score.
        if (IsVMAF && args.Data != null && args.Data.Contains(" VMAF score: "))
        {
            // The string actually terminates with something like "e=N/A".
            string vmafScore = args.Data.Split("VMAF score:")[1].Split("e=")[0];

            VMAFScore = double.Parse(vmafScore);
        }

        Log.AppendLine(args.Data);
        InfoUpdate?.Invoke(this, args);
    }
}