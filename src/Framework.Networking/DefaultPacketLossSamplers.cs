using System;
using System.Threading;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Factory and default implementations of <see cref="PacketLossSampler" />. Tests use a
///     deterministic sampler that returns a controlled value; production uses the shared
///     <see cref="DefaultPacketLossSamplers.Shared" /> sampler that draws from a thread-safe
///     <see cref="Random" /> instance.
/// </summary>
public static class DefaultPacketLossSamplers
{
    private static readonly ThreadLocal<Random> RandomPerThread;

    /// <summary>
    ///     Gets the default packet-loss sampler used by the proxy handler when no explicit
    ///     sampler is supplied. Returns a uniform random value in the inclusive-exclusive
    ///     range [0, 1).
    /// </summary>
    public static PacketLossSampler Shared { get; }

    static DefaultPacketLossSamplers()
    {
        var perThread = new ThreadLocal<Random>(CreateRandom);
        RandomPerThread = perThread;
        Shared = Sample;
    }

    private static Random CreateRandom()
    {
        var generator = new Random();
        return generator;
    }

    private static double Sample()
    {
        var generator = RandomPerThread.Value!;
        return generator.NextDouble();
    }
}
