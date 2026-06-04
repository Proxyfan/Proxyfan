namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents the shell flavor used to quote a generated cURL command.
/// </summary>
public enum CurlCommandShellFlavor
{
    /// <summary>
    ///     Quotes arguments for Bash-compatible shells.
    /// </summary>
    Bash = 0,

    /// <summary>
    ///     Quotes arguments for PowerShell.
    /// </summary>
    PowerShell = 1,
}
