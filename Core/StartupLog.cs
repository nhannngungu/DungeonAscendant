using System;
using System.IO;
using DungeonAscendant.Persistence;

namespace DungeonAscendant.Core;

public static class StartupLog
{
    private static readonly object Sync = new();
    private static string _path;

    public static string Path => _path ?? ApplicationDataPaths.StartupLogFile;

    public static void Initialize()
    {
        _path = ApplicationDataPaths.StartupLogFile;
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            Write("[PROCESS EXIT] CLR process-exit event reached.");
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            Write($"[UNHANDLED] terminating={eventArgs.IsTerminating}; object={eventArgs.ExceptionObject}");
        Write("------------------------------------------------------------");
        Write($"Program.Main entered; base={AppContext.BaseDirectory}");
        Write($"OS={Environment.OSVersion}; 64-bit={Environment.Is64BitProcess}");
    }

    public static void Write(string message)
    {
        try
        {
            lock (Sync)
            {
                string path = Path;
                string directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.AppendAllText(
                    path,
                    $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
            NotSupportedException)
        {
        }
    }

    public static void Exception(string context, Exception exception) =>
        Write($"{context}: {exception}");
}
