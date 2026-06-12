using System.Net;
using System.Threading;

public class WorkerPool
{
    private readonly RequestQueue _queue;
    private readonly RequestHandler _handler;
    private readonly int _workerCount;

    public WorkerPool(RequestQueue queue, RequestHandler handler, int workerCount = 4)
    {
        _queue = queue;
        _handler = handler;
        _workerCount = workerCount;
    }

    public void Start()
    {
        for (int i = 0; i < _workerCount; i++)
        {
            int workerId = i + 1;
            Thread thread = new Thread(() => WorkerLoop(workerId));
            thread.IsBackground = true;
            thread.Name = $"Worker-{workerId}";
            thread.Start();
            Logger.LogInfo($"Worker-{workerId} pokrenut");
        }
    }

    private void WorkerLoop(int workerId)
    {
        while (true)
        {
            var context = _queue.Dequeue();
            Logger.LogInfo($"Worker-{workerId} preuzeo zahtev");
            _handler.Handle(context); // ← Handle, ne HandleAsync
        }
    }
}