using System.Collections.Generic;
using System.Net;
using System.Threading;

// Zadrzavamo klasicne niti i Monitor.Wait/Pulse
// Queue je deljeni resurs izmedju listener niti i taskova
// Blokirajuca sinhronizacija ovde ima smisla
public class RequestQueue
{
    private readonly Queue<HttpListenerContext> _queue 
        = new Queue<HttpListenerContext>();
    private readonly object _lock = new object();
    private readonly int _maxSize;

    public RequestQueue(int maxSize = 100)
    {
        _maxSize = maxSize;
    }

    public void Enqueue(HttpListenerContext context)
    {
        lock (_lock)
        {
            while (_queue.Count >= _maxSize)
            {
                Logger.LogInfo("Queue je pun, cekamo...");
                Monitor.Wait(_lock);
            }
            _queue.Enqueue(context);
            Logger.LogInfo($"Zahtev dodat u queue. Trenutno: {_queue.Count}");
            Monitor.Pulse(_lock);
        }
    }

    public HttpListenerContext Dequeue()
    {
        lock (_lock)
        {
            while (_queue.Count == 0)
            {
                Monitor.Wait(_lock);
            }
            var context = _queue.Dequeue();
            Monitor.Pulse(_lock);
            return context;
        }
    }
}