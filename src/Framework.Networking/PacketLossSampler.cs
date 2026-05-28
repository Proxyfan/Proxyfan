namespace Proxyfan.Framework.Networking;

/// <summary>
///     Returns a uniform random sample in <c>[0, 1)</c>. Injected into packet-loss decisions
///     so callers can supply a deterministic source in tests.
/// </summary>
/// <returns>The sampled value.</returns>
public delegate double PacketLossSampler();
