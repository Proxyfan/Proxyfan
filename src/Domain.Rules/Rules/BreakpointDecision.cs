using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Defines the action a user takes to resolve a breakpoint pause.
/// </summary>
public sealed class BreakpointDecision
{
    /// <summary>
    ///     Gets a value indicating whether the proxy should abort the request and close the connection.
    /// </summary>
    public bool IsAborting { get; }

    /// <summary>
    ///     Gets the (possibly modified) request data to forward. <see langword="null" /> when
    ///     this decision is for the response phase.
    /// </summary>
    public HypertextTransferProtocolRequestData? ModifiedRequest { get; }

    /// <summary>
    ///     Gets the (possibly modified) response data to deliver. <see langword="null" /> when
    ///     this decision is for the request phase.
    /// </summary>
    public HypertextTransferProtocolResponseData? ModifiedResponse { get; }

    /// <summary>
    ///     Initializes a new <see cref="BreakpointDecision" />. Prefer the factory helpers on
    ///     <see cref="BreakpointDecisions" /> to express the intended decision shape clearly.
    ///     Construction is validated so every instance represents exactly one of the supported
    ///     decision shapes: abort, resume-request, or resume-response.
    /// </summary>
    /// <param name="isAborting">Whether the request should be aborted.</param>
    /// <param name="modifiedRequest">The (possibly modified) request to forward.</param>
    /// <param name="modifiedResponse">The (possibly modified) response to deliver.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when the supplied combination does not match a valid decision shape — i.e. an
    ///     aborting decision carrying a payload, or a non-aborting decision that carries neither
    ///     or both payloads.
    /// </exception>
    public BreakpointDecision(
        bool isAborting,
        HypertextTransferProtocolRequestData? modifiedRequest,
        HypertextTransferProtocolResponseData? modifiedResponse)
    {
        if (isAborting)
        {
            if (modifiedRequest is not null || modifiedResponse is not null)
            {
                throw new ArgumentException(
                    "An aborting breakpoint decision must not carry a modified request or response.",
                    nameof(isAborting));
            }
        }
        else
        {
            if (modifiedRequest is null && modifiedResponse is null)
            {
                throw new ArgumentException(
                    "A non-aborting breakpoint decision must carry either a modified request or a modified response.",
                    nameof(modifiedRequest));
            }

            if (modifiedRequest is not null && modifiedResponse is not null)
            {
                throw new ArgumentException(
                    "A breakpoint decision cannot carry both a modified request and a modified response.",
                    nameof(modifiedResponse));
            }
        }

        IsAborting = isAborting;
        ModifiedRequest = modifiedRequest;
        ModifiedResponse = modifiedResponse;
    }
}
