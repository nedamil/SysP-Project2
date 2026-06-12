using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class RequestHandler
{
    private readonly CacheManager _cache;
    private readonly string _rootFolder;
    private readonly SemaphoreSlim _semaphore;

    public RequestHandler(CacheManager cache, string rootFolder, int maxParallel = 4)
    {
        _cache = cache;
        _rootFolder = rootFolder;
        _semaphore = new SemaphoreSlim(maxParallel);
    }

    public Task HandleAsync(HttpListenerContext context)
    {
        return _semaphore.WaitAsync()
            .ContinueWith(async _ =>
            {
                try
                {
                    await ProcessRequestAsync(context);
                }
                finally
                {
                    _semaphore.Release();
                    Logger.LogInfo("Semaphore oslobodjen");
                }
            }).Unwrap();
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        string? fileName = context.Request.Url?.AbsolutePath.TrimStart('/');

        if (string.IsNullOrWhiteSpace(fileName))
        {
            SendResponse(context, 400,
                "Naziv fajla nije naveden. Primer: http://localhost:5050/fajl.txt");
            return;
        }

        Logger.LogInfo($"Obrada zahteva za fajl: '{fileName}'");

        try
        {
            var (shouldProcess, waitTask) = _cache.TryGet(fileName);

            if (!shouldProcess)
            {
                if (waitTask == null)
                {
                    SendResponse(context, 500, "Greška u kešu.");
                    return;
                }

                // await umesto Monitor.Wait - nit se oslobadja dok ceka
                int cachedResult = await waitTask;
                string cachedMsg = cachedResult == 0
                    ? "Nema palindroma u fajlu."
                    : $"Broj palindroma (iz cache-a): {cachedResult}";
                SendResponse(context, 200, cachedMsg);
                return;
            }

            // Async pretraga fajlova sa ContinueWith
            string[] files = await Task.Run(() =>
                Directory.GetFiles(_rootFolder, fileName,
                    SearchOption.AllDirectories))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Logger.LogError($"Greska pri pretrazivanju: {t.Exception?.Message}");
                        return Array.Empty<string>();
                    }
                    Logger.LogInfo($"Pretraga zavrsena, pronadjeno {t.Result.Length} fajlova");
                    return t.Result;
                });

            if (files.Length == 0)
            {
                _cache.SetError(fileName);
                SendResponse(context, 404, $"Fajl '{fileName}' nije pronađen.");
                return;
            }

            // Async brojanje palindroma sa ContinueWith
            int count = await CountPalindromesAsync(files[0])
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Logger.LogError($"Greska pri brojanju: {t.Exception?.Message}");
                        return 0;
                    }
                    Logger.LogInfo($"Brojanje zavrseno: {t.Result} palindroma");
                    return t.Result;
                });

            _cache.Set(fileName, count);

            string message = count == 0
                ? "Nema palindroma u fajlu."
                : $"Broj palindroma: {count}";

            SendResponse(context, 200, message);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Greška pri obradi '{fileName}': {ex.Message}");
            _cache.SetError(fileName);
            SendResponse(context, 500, "Interna greška servera.");
        }
    }

    private async Task<int> CountPalindromesAsync(string filePath)
    {
        // I/O operacija - async, ne blokira nit
        string text = await File.ReadAllTextAsync(filePath);

        // CPU operacija - Task.Run da ne blokira
        return await Task.Run(() =>
        {
            int count = 0;
            string[] words = text.Split(
                new char[] { ' ', '\n', '\r', '\t', ',', '.', '!', '?', ';', ':' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                string cleaned = word.ToLower();
                if (cleaned.Length > 1 && IsPalindrome(cleaned))
                {
                    count++;
                    Logger.LogInfo($"Palindrom pronađen: '{cleaned}'");
                }
            }
            return count;
        });
    }

    private bool IsPalindrome(string word)
    {
        int left = 0, right = word.Length - 1;
        while (left < right)
        {
            if (word[left] != word[right]) return false;
            left++;
            right--;
        }
        return true;
    }

    private void SendResponse(HttpListenerContext context, int statusCode, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
        Logger.LogInfo($"Odgovor poslan: {statusCode} - {message}");
    }
}