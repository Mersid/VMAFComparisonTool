using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FFBatchConverter;

public static class Helpers
{
	public static string? GetFFmpegPath()
	{
		return FindCommand("ffmpeg");
	}

	public static string? GetFFprobePath()
	{
		return FindCommand("ffprobe");
	}

	/// <summary>
	/// Finds the path of the executable for the command.
	/// </summary>
	/// <returns>The first match for the command specified, or null if it could not be found.</returns>
	private static string? FindCommand(string command)
	{
		// Call the "where" or "which" command to find the path of the ffmpeg executable
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = OperatingSystem.IsWindows() ? "where" : "which",
			Arguments = command,
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		Process process = new Process
		{
			StartInfo = startInfo
		};

		process.Start();

		while (!process.StandardOutput.EndOfStream)
		{
			string? info = process.StandardOutput.ReadLine();
			if (info != null)
				return info;
		}

		return null;
	}
}