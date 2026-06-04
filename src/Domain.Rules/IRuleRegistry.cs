using System.Collections.Generic;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Mutable registry of request- and response-phase rules consumed by
///     <see cref="RuleEngine" /> on each evaluation. Allows the user
///     interface to add, remove and reorder rules at runtime.
/// </summary>
public interface IRuleRegistry
{
    /// <summary>
    ///     Raised whenever the registry contents change, signalling that
    ///     any cached snapshots should be discarded.
    /// </summary>
    event RuleRegistryChanged? Changed;

    /// <summary>
    ///     Gets a snapshot of the current async request-phase rules, sorted by
    ///     ascending <see cref="IAsyncRequestPhaseRule.Priority" />.
    /// </summary>
    /// <returns>The ordered async request-phase rule list.</returns>
    IReadOnlyList<IAsyncRequestPhaseRule> GetAsyncRequestPhaseRules();

    /// <summary>
    ///     Gets a snapshot of the current async response-phase rules, sorted by
    ///     ascending <see cref="IAsyncResponsePhaseRule.Priority" />.
    /// </summary>
    /// <returns>The ordered async response-phase rule list.</returns>
    IReadOnlyList<IAsyncResponsePhaseRule> GetAsyncResponsePhaseRules();

    /// <summary>
    ///     Gets a snapshot of the current request-phase rules, sorted by
    ///     ascending <see cref="IRequestPhaseRule.Priority" />.
    /// </summary>
    /// <returns>The ordered request-phase rule list.</returns>
    IReadOnlyList<IRequestPhaseRule> GetRequestPhaseRules();

    /// <summary>
    ///     Gets a snapshot of the current response-phase rules, sorted by
    ///     ascending <see cref="IResponsePhaseRule.Priority" />.
    /// </summary>
    /// <returns>The ordered response-phase rule list.</returns>
    IReadOnlyList<IResponsePhaseRule> GetResponsePhaseRules();

    /// <summary>
    ///     Registers an additional async request-phase rule with the registry.
    /// </summary>
    /// <param name="rule">The rule to register.</param>
    void RegisterAsyncRequestPhaseRule(IAsyncRequestPhaseRule rule);

    /// <summary>
    ///     Registers an additional async response-phase rule with the registry.
    /// </summary>
    /// <param name="rule">The rule to register.</param>
    void RegisterAsyncResponsePhaseRule(IAsyncResponsePhaseRule rule);

    /// <summary>
    ///     Registers an additional request-phase rule with the registry.
    /// </summary>
    /// <param name="rule">The rule to register.</param>
    void RegisterRequestPhaseRule(IRequestPhaseRule rule);

    /// <summary>
    ///     Registers an additional response-phase rule with the registry.
    /// </summary>
    /// <param name="rule">The rule to register.</param>
    void RegisterResponsePhaseRule(IResponsePhaseRule rule);

    /// <summary>
    ///     Removes a previously registered async request-phase rule from the registry.
    ///     No-op when the rule was not present.
    /// </summary>
    /// <param name="rule">The rule to remove.</param>
    void UnregisterAsyncRequestPhaseRule(IAsyncRequestPhaseRule rule);

    /// <summary>
    ///     Removes a previously registered async response-phase rule from the registry.
    ///     No-op when the rule was not present.
    /// </summary>
    /// <param name="rule">The rule to remove.</param>
    void UnregisterAsyncResponsePhaseRule(IAsyncResponsePhaseRule rule);

    /// <summary>
    ///     Removes a previously registered request-phase rule from the registry.
    ///     No-op when the rule was not present.
    /// </summary>
    /// <param name="rule">The rule to remove.</param>
    void UnregisterRequestPhaseRule(IRequestPhaseRule rule);

    /// <summary>
    ///     Removes a previously registered response-phase rule from the registry.
    ///     No-op when the rule was not present.
    /// </summary>
    /// <param name="rule">The rule to remove.</param>
    void UnregisterResponsePhaseRule(IResponsePhaseRule rule);
}
