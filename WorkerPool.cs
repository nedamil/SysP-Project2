using System.Net;
using System.Threading;
using System.Threading.Tasks;

// Klasicne niti za citanje iz queue-a - ima smisla jer je
// Dequeue blokirajuca operacija (Monitor.Wait)
// Taskovi se koriste za samu obradu zahteva
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
            // Klasicna nit za blokirajuce cekanje na queue
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
        
        try
        {
            _handler.Handle(context);
            Logger.LogInfo($"Worker-{workerId} završio obradu");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Worker-{workerId} greška: {ex.Message}");
        }
    }
}
}