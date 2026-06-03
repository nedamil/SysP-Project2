using System;
using System.IO;

public class Logger
{
    private static readonly object _lock = new object();
    private static readonly string _logFile = "server.log";

    public static void Log(string message)
    {
        string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        lock (_lock)
        {
            Console.WriteLine(entry);
            File.AppendAllText(_logFile, entry + Environment.NewLine);
        }
    }

    public static void LogError(string message) => Log($"[ERROR] {message}");
    public static void LogInfo(string message) => Log($"[INFO] {message}");
}