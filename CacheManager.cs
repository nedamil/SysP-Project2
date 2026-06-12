using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CacheManager
{
    private readonly Dictionary<string, CacheEntry> _cache
        = new Dictionary<string, CacheEntry>();
    private readonly Dictionary<string, object> _keyLocks
        = new Dictionary<string, object>();
    private readonly object _globalLock = new object();
    private readonly TimeSpan _ttl;

    public CacheManager(TimeSpan ttl)
    {
        _ttl = ttl;
    }

    private object GetKeyLock(string key)
    {
        lock (_globalLock)
        {
            if (!_keyLocks.TryGetValue(key, out object? keyLock))
            {
                keyLock = new object();
                _keyLocks[key] = keyLock;
            }
            return keyLock;
        }
    }

    public (bool shouldProcess, Task<int>? waitTask) TryGet(string key)
    {
        object keyLock = GetKeyLock(key);

        lock (keyLock)
        {
            if (_cache.TryGetValue(key, out CacheEntry? entry))
            {
                // Nije spreman - vrati Task za async cekanje
                // umesto Monitor.Wait
                if (!entry.IsReady)
                {
                    Logger.LogInfo($"Task ceka na rezultat za '{key}'");
                    return (false, entry.Tcs.Task);
                }

                // TTL istekao
                if (DateTime.Now - entry.CreatedAt > _ttl)
                {
                    Logger.LogInfo($"Cache istekao za '{key}', brisemo");
                    var newEntry = new CacheEntry { IsReady = false };
                    _cache[key] = newEntry;
                    return (true, null);
                }

                // Cache hit
                Logger.LogInfo($"Cache hit za '{key}'");
                return (false, Task.FromResult(entry.PalindromeCount));
            }
            else
            {
                // Cache miss - ova nit obradjuje
                var newEntry = new CacheEntry { IsReady = false };
                _cache[key] = newEntry;
                return (true, null);
            }
        }
    }

    public void Set(string key, int result)
    {
        object keyLock = GetKeyLock(key);

        lock (keyLock)
        {
            if (_cache.TryGetValue(key, out CacheEntry? entry))
            {
                entry.PalindromeCount = result;
                entry.CreatedAt = DateTime.Now;
                entry.IsReady = true;
                // Kompletira Task - probudi sve taskove koji cekaju
                entry.Tcs.TrySetResult(result);
            }
            Logger.LogInfo($"Cache upisan za '{key}': {result} palindroma");
        }
    }

    public void SetError(string key)
    {
        object keyLock = GetKeyLock(key);

        lock (keyLock)
        {
            if (_cache.TryGetValue(key, out CacheEntry? entry))
            {
                // Obavesti cekajuce taskove da je doslo do greske
                entry.Tcs.TrySetException(
                    new Exception($"Obrada nije uspela za '{key}'"));
                _cache.Remove(key);
                _keyLocks.Remove(key);
            }
            Logger.LogInfo($"Cache greska za '{key}', placeholder uklonjen");
        }
    }
}