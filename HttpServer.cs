using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly RequestQueue _queue;

    public HttpServer(string prefix, RequestQueue queue)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _queue = queue;
    }

    public void Start()
    {
        _listener.Start();
        Logger.LogInfo("HTTP Server pokrenut na http://localhost:5050/");

        // Klasicna nit za listener - GetContext() je blokirajuce
        Thread listenerThread = new Thread(ListenLoop);
        listenerThread.IsBackground = true;
        listenerThread.Name = "ListenerThread";
        listenerThread.Start();
    }

    private void ListenLoop()
    {
        while (_listener.IsListening)
        {
            try
            {
                // GetContext blokira - klasicna nit je ispravna
                HttpListenerContext context = _listener.GetContext();
                Logger.LogInfo($"Primljen zahtev: {context.Request.Url}");
                _queue.Enqueue(context);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Greška pri prijemu: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _listener.Stop();
        Logger.LogInfo("Server zaustavljen.");
    }
}