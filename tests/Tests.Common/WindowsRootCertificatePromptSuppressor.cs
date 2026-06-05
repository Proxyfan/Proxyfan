using Microsoft.Win32;
using System;
using System.Runtime.Versioning;

namespace Proxyfan.Tests.Common;

/// <summary>
///     Best-effort helper that disables the Windows security-warning dialog raised when a
///     root certificate is installed into the CurrentUser\Root store. The dialog blocks
///     unattended test execution; suppressing it lets test infrastructure install the
///     persistent test CA without user interaction.
/// </summary>
/// <remarks>
///     This writes <c>HKCU\Software\Policies\Microsoft\SystemCertificates\Root\ProtectedRoots\Flags</c>
///     to <c>0x1</c>. On machines where an enterprise Group Policy locks the
///     <c>HKCU\Software\Policies</c> hive, the write is rejected with an
///     <see cref="UnauthorizedAccessException" /> and this helper silently no-ops — the
///     persistent test CA will still install successfully but the user will be prompted once
///     for the new thumbprint. Subsequent runs reuse the persisted thumbprint, so the prompt
///     never appears again.
///
///     There is no fallback to a non-policy registry path because, on managed machines, the
///     enterprise GPO that locks the policy hive almost always also overrides any user-level
///     setting under <c>HKCU\Software\Microsoft\SystemCertificates</c>. Pretending to honour
///     the suppression in that case would create a false sense of unattended-ness.
/// </remarks>
[SupportedOSPlatform("windows")]
public static class WindowsRootCertificatePromptSuppressor
{
    private const string PolicyKeyPath = @"Software\Policies\Microsoft\SystemCertificates\Root\ProtectedRoots";
    private const string FlagsValueName = "Flags";
    private const int SuppressInstallAndRemoveDialogs = 0x1;

    /// <summary>
    ///     Attempts to set the registry flag that suppresses the root-certificate install /
    ///     remove dialogs.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> when the flag is in place after this call (whether it was
    ///     newly written by this call or already present); <see langword="false" /> when the
    ///     write was rejected by an enterprise Group Policy lock.
    /// </returns>
    public static bool Suppress()
    {
        if (HasFlagSet())
        {
            return true;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(PolicyKeyPath, writable: true);
            key.SetValue(FlagsValueName, SuppressInstallAndRemoveDialogs, RegistryValueKind.DWord);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static bool HasFlagSet()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PolicyKeyPath, writable: false);
        if (key is null)
        {
            return false;
        }

        var raw = key.GetValue(FlagsValueName);
        if (raw is not int flags)
        {
            return false;
        }

        return (flags & SuppressInstallAndRemoveDialogs) == SuppressInstallAndRemoveDialogs;
    }
}
