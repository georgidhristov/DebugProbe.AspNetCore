using System.Collections.Concurrent;
using DebugProbe.AspNetCore.Models;

namespace DebugProbe.AspNetCore.Store;

public class RequestStore
{
    private readonly ConcurrentQueue<DebugEntry> _queue = new();
    private readonly int _limit = 20;

    public void Add(DebugEntry entry)
    {
        _queue.Enqueue(entry);
        while (_queue.Count > _limit)
            _queue.TryDequeue(out _);
    }

    public List<DebugEntry> GetAll() => _queue.ToList();
}