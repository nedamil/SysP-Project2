using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string rootFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "files");
        Directory.CreateDirectory(rootFolder);

        Logger.LogInfo("=== Palindrome Word Server v2 (Tasks) ===");
        Logger.LogInfo($"Root folder: {rootFolder}");

        var cache = new CacheManager(TimeSpan.FromMinutes(5));
        var queue = new RequestQueue(maxSize: 100);
        var handler = new RequestHandler(cache, rootFolder);
        var workerPool = new WorkerPool(queue, handler, workerCount: 4);
        var server = new HttpServer("http://localhost:5050/", queue);

        workerPool.Start();
        server.Start();

        Logger.LogInfo("Server radi. Pritisni Enter za zaustavljanje...");
        Console.ReadLine();

        server.Stop();
    }
}