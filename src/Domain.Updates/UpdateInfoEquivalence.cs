using System;

namespace Proxyfan.Domain.Updates;

/// <summary>
///     Helpers for comparing <see cref="UpdateInfo" /> instances.
/// </summary>
public static class UpdateInfoEquivalence
{
    /// <summary>
    ///     Determines whether two <see cref="UpdateInfo" /> instances represent the same
    ///     available update. Two instances are equivalent when both are <see langword="null" />,
    ///     when both refer to the same object, or when their <see cref="UpdateInfo.Version" />
    ///     strings are ordinally equal.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true" /> when the two instances are equivalent.</returns>
    public static bool HasSameAvailableUpdate(UpdateInfo? left, UpdateInfo? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return string.Equals(left.Version, right.Version, StringComparison.Ordinal);
    }
}
