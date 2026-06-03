using System;
using System.Threading.Tasks;

public class CacheEntry
{
    public int PalindromeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsReady { get; set; } = false;
    
    // TaskCompletionSource omogucava async cekanje
    // umesto Monitor.Wait, ostali taskovi await-uju ovo
    public TaskCompletionSource<int> Tcs { get; set; } 
        = new TaskCompletionSource<int>();
}