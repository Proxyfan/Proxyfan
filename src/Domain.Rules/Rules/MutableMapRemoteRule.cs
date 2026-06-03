using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     A mutable Map Remote rule containing zero or more (pattern, destination) entries.
///     Entries can be added, removed, or toggled at runtime. The rule evaluates entries in
///     registration order and short-circuits on the first enabled entry whose pattern matches.
/// </summary>
public sealed class MutableMapRemoteRule : IRequestPhaseRule
{
    /// <summary>
    ///     Raised whenever the rule's enabled state or entry collection changes.
    /// </summary>
    public event MutableMapRemoteChanged? Changed;

    private readonly List<MapRemoteEntry> _entries;
    private readonly Lock _mutationLock;
    private volatile CompiledEntry[] _compiled;
    private volatile bool _isEnabled;

    /// <summary>
    ///     Initializes a new empty <see cref="MutableMapRemoteRule" />.
    /// </summary>
    /// <param name="priority">The rule's priority within request-phase rules.</param>
    /// <param name="isEnabled">Whether the rule is initially active.</param>
    public MutableMapRemoteRule(int priority, bool isEnabled)
    {
        Priority = priority;
        _isEnabled = isEnabled;
        _entries = [];
        _compiled = [];
        var mutationLock = new Lock();
        _mutationLock = mutationLock;
    }

    /// <inheritdoc />
    public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var snapshot = _compiled;
        if (snapshot.Length == 0)
        {
            return null;
        }

        var url = request.RequestUri.ToString();
        for (var index = 0; index < snapshot.Length; index++)
        {
            var entry = snapshot[index];
            if (!entry.IsEnabled)
            {
                continue;
            }

            if (!entry.Matcher.HasMatch(url))
            {
                continue;
            }

            var rewrittenUri = MapRemoteUriRewriter.Rewrite(request.RequestUri, entry.Destination);
            var rewrittenHeaders = entry.Destination.IsPreservingHostHeader
                ? request.Headers
                : MapRemoteHeaderRewriter.ReplaceHostHeader(request.Headers, rewrittenUri);
            var parameters = new HypertextTransferProtocolRequestDataParameters
            {
                Body = request.Body,
                Headers = rewrittenHeaders,
                Method = request.Method,
                RequestUri = rewrittenUri,
                Version = request.Version,
            };
            var rewrittenRequest = new HypertextTransferProtocolRequestData(parameters);
            return new RequestPipelineAction.Redirect(rewrittenRequest);
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc />
    public int Priority { get; }

    /// <summary>
    ///     Adds a new entry to the rule.
    /// </summary>
    /// <param name="entry">The mapping entry to add.</param>
    public void AddEntry(MapRemoteEntry entry)
    {
        lock (_mutationLock)
        {
            var updatedEntries = new List<MapRemoteEntry>(_entries.Count + 1);
            updatedEntries.AddRange(_entries);
            updatedEntries.Add(entry);

            var rebuilt = BuildCompiledEntries(updatedEntries);
            _entries.Add(entry);
            _compiled = rebuilt;
        }

        RaiseChanged();
    }

    /// <summary>
    ///     Returns a snapshot of the configured entries in registration order.
    /// </summary>
    /// <returns>An immutable snapshot of the configured entries.</returns>
    public IReadOnlyList<MapRemoteEntry> GetEntries()
    {
        lock (_mutationLock)
        {
            return [.. _entries];
        }
    }

    /// <summary>
    ///     Removes the supplied entry from the rule. Reference equality is used.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    public void RemoveEntry(MapRemoteEntry entry)
    {
        bool removed;
        lock (_mutationLock)
        {
            removed = _entries.Remove(entry);
            if (removed)
            {
                RebuildUnderLock();
            }
        }

        if (removed)
        {
            RaiseChanged();
        }
    }

    /// <summary>
    ///     Enables or disables the rule as a whole.
    /// </summary>
    /// <param name="isEnabled">Whether the rule should be active.</param>
    public void SetEnabled(bool isEnabled)
    {
        if (_isEnabled == isEnabled)
        {
            return;
        }

        _isEnabled = isEnabled;
        RaiseChanged();
    }

    private CompiledEntry[] BuildCompiledEntries(List<MapRemoteEntry> entries)
    {
        var rebuilt = new CompiledEntry[entries.Count];
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var compiled = new CompiledEntry
            {
                Destination = entry.Destination,
                IsEnabled = entry.IsEnabled,
                Matcher = entry.MatchingRule.Compile(),
            };
            rebuilt[index] = compiled;
        }

        return rebuilt;
    }

    private void RaiseChanged()
    {
        Changed?.Invoke();
    }

    private void RebuildUnderLock()
    {
        _compiled = BuildCompiledEntries(_entries);
    }

    private sealed class CompiledEntry
    {
        public required MapRemoteDestination Destination { get; init; }

        public required bool IsEnabled { get; init; }

        public required IUrlMatcher Matcher { get; init; }
    }
}
