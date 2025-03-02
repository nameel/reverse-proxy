// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Yarp.ReverseProxy.Utilities;

namespace Yarp.ReverseProxy.ServiceFabric.Utilities;

public class Cache<T>
{
    private readonly TimeSpan _expirationTimeOffset;
    private readonly IClock _clock;
    private readonly Dictionary<string, Expirable> _cache = new Dictionary<string, Expirable>(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="Cache{T}"/> class.
    /// </summary>
    /// <param name="clock">A timer to use to track expirations.</param>
    /// <param name="expirationTimeOffset">The time it takes for cache values to expire.</param>
    public Cache(IClock clock, TimeSpan expirationTimeOffset)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _expirationTimeOffset = expirationTimeOffset;
    }

    /// <summary>
    /// TODO.
    /// </summary>
    public T Get(string key)
    {
        var present = TryGetValue(key, out var value);
        if (!present)
        {
            throw new KeyNotFoundException($"Key {key} is not present.");
        }
        return value;
    }

    /// <summary>
    /// TODO.
    /// </summary>
    public bool TryGetValue(string key, out T value)
    {
        var present = _cache.TryGetValue(key, out var expirable);
        if (!present || expirable.Expired(_clock))
        {
            value = default;
            if (present)
            {
                // Take the oportunity to update internal state
                _cache.Remove(key);
            }
            return false;
        }
        value = expirable.Value;
        return true;
    }

    /// <summary>
    /// TODO.
    /// </summary>
    public void Set(string key, T value)
    {
        _cache[key] = new Expirable(
            value: value,
            expirationTime: _clock.GetStopwatchTime().Add(_expirationTimeOffset));
    }

    /// <summary>
    /// Looks for expired values in the cache and deletes them.
    /// </summary>
    public void Cleanup()
    {
        var toRemove = _cache
            .Where(pair => pair.Value.Expired(_clock))
            .Select(pair => pair.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _cache.Remove(key);
        }
    }

    private struct Expirable
    {
        internal Expirable(T value, TimeSpan expirationTime)
        {
            Value = value;
            ExpirationTime = expirationTime;
        }
        internal T Value { get; }
        internal TimeSpan ExpirationTime { get; }
        internal bool Expired(IClock timer) => ExpirationTime < timer.GetStopwatchTime();
    }
}
