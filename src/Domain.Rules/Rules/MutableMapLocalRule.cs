using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     A mutable Map Local rule containing zero or more (pattern, local response) entries.
///     Entries can be added, removed, or toggled at runtime. The rule evaluates entries in
///     registration order and short-circuits on the first enabled entry whose pattern matches,
///     returning a <see cref="RequestPipelineAction.ServeLocalResponse" />.
/// </summary>
public sealed class MutableMapLocalRule : IRequestPhaseRule
{
    /// <summary>
    ///     Raised whenever the rule's enabled state or entry collection changes.
    /// </summary>
    public event MutableMapLocalChanged? Changed;

    private readonly List<MapLocalEntry> _entries;
    private readonly Lock _mutationLock;
    private volatile CompiledEntry[] _compiled;
    private volatile bool _isEnabled;

    /// <summary>
    ///     Initializes a new empty <see cref="MutableMapLocalRule" />.
    /// </summary>
    /// <param name="priority">The rule's priority within request-phase rules.</param>
    /// <param name="isEnabled">Whether the rule is initially active.</param>
    public MutableMapLocalRule(int priority, bool isEnabled)
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

            var responseParameters = new HypertextTransferProtocolResponseDataParameters
            {
                Body = entry.Body,
                Headers = entry.Headers,
                ReasonPhrase = entry.ReasonPhrase,
                StatusCode = entry.StatusCode,
                Version = "HTTP/1.1",
            };
            var response = new HypertextTransferProtocolResponseData(responseParameters);
            return new RequestPipelineAction.ServeLocalResponse(response);
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
    public void AddEntry(MapLocalEntry entry)
    {
        if (entry.StatusCode is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(entry), entry.StatusCode, "Status code must be between 100 and 599.");
        }

        lock (_mutationLock)
        {
            var candidateEntries = new List<MapLocalEntry>(_entries.Count + 1);
            candidateEntries.AddRange(_entries);
            candidateEntries.Add(entry);
            var rebuilt = BuildCompiledEntries(candidateEntries);

            _entries.Add(entry);
            _compiled = rebuilt;
        }

        RaiseChanged();
    }

    /// <summary>
    ///     Returns a snapshot of the configured entries in registration order.
    /// </summary>
    /// <returns>An immutable snapshot of the configured entries.</returns>
    public IReadOnlyList<MapLocalEntry> GetEntries()
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
    public void RemoveEntry(MapLocalEntry entry)
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

    private CompiledEntry[] BuildCompiledEntries(List<MapLocalEntry> entries)
    {
        var rebuilt = new CompiledEntry[entries.Count];
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var headers = HeaderCollection.Empty;
            foreach (var header in entry.Headers)
            {
                headers = headers.Add(header.Key, header.Value);
            }

            var compiled = new CompiledEntry
            {
                Body = entry.Body,
                Headers = headers,
                IsEnabled = entry.IsEnabled,
                Matcher = entry.MatchingRule.Compile(),
                ReasonPhrase = entry.ReasonPhrase,
                StatusCode = entry.StatusCode,
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
        public required ReadOnlyMemory<byte> Body { get; init; }

        public required HeaderCollection Headers { get; init; }

        public required bool IsEnabled { get; init; }

        public required IUrlMatcher Matcher { get; init; }

        public required string ReasonPhrase { get; init; }

        public required int StatusCode { get; init; }
    }
}
