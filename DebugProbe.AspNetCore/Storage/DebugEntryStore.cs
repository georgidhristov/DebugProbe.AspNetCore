using System.Collections.Concurrent;
using DebugProbe.AspNetCore.Models;
using DebugProbe.AspNetCore.Options;

namespace DebugProbe.AspNetCore.Storage;

/// <summary>
/// In-memory store for DebugEntry instances with a configurable size limit.
/// </summary>
public class DebugEntryStore
{
    private readonly ConcurrentQueue<DebugEntry> _queue = new();
    private readonly int _limit;

    public DebugEntryStore(DebugProbeOptions options)
    {
        _limit = options.MaxEntries;
    }

    public void Add(DebugEntry entry)
    {
        _queue.Enqueue(entry);
        while (_queue.Count > _limit)
            _queue.TryDequeue(out _);
    }

    public List<DebugEntry> GetAll()
    {
        return _queue.ToList();
    }

    public DebugEntry? Get(string id)
    {
        return _queue.FirstOrDefault(x => x.Id == id);
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}