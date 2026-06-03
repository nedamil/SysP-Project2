using System;
using System.Collections.Generic;
using System.Threading;

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

    public bool TryGet(string key, out int result)
    {
        object keyLock = GetKeyLock(key);

        lock (keyLock)
        {
            while (true)
            {
                if (_cache.TryGetValue(key, out CacheEntry? entry))
                {
                    if (!entry.IsReady)
                    {
                        Logger.LogInfo($"Nit ceka na rezultat za '{key}'");
                        Monitor.Wait(keyLock);
                        continue;
                    }

                    if (DateTime.Now - entry.CreatedAt > _ttl)
                    {
                        Logger.LogInfo($"Cache istekao za '{key}', brisemo");
                        _cache.Remove(key);
                        _cache[key] = new CacheEntry { IsReady = false };
                        result = 0;
                        return false;
                    }

                    Logger.LogInfo($"Cache hit za '{key}'");
                    result = entry.PalindromeCount;
                    return true;
                }
                else
                {
                    _cache[key] = new CacheEntry { IsReady = false };
                    result = 0;
                    return false;
                }
            }
        }
    }

    public void Set(string key, int result)
    {
        object keyLock = GetKeyLock(key);

        lock (keyLock)
        {
            _cache[key] = new CacheEntry
            {
                PalindromeCount = result,
                CreatedAt = DateTime.Now,
                IsReady = true
            };
            Monitor.PulseAll(keyLock);
            Logger.LogInfo($"Cache upisan za '{key}': {result} palindroma");
        }
    }

    public void SetError(string key)
    {
        object keyLock = GetKeyLock(key);

        lock (keyLock)
        {
            _cache.Remove(key);
            Monitor.PulseAll(keyLock);
            Logger.LogInfo($"Cache greska za '{key}', placeholder uklonjen");
        }
    }
}