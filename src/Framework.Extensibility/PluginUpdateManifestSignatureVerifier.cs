using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Verifies detached signatures on plugin update manifests using a pinned trust root.
/// </summary>
public static class PluginUpdateManifestSignatureVerifier
{
    /// <summary>
    ///     SubjectPublicKeyInfo (DER, base64) for the trusted plugin update manifest signer.
    ///     Rotate this value only when introducing a new signing key and coordinating manifest
    ///     generation to use the matching private key.
    /// </summary>
    private const string ManifestSigningPublicKeySubjectPublicKeyInfoBase64 = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEs9Mbiyk07V9A+u11gJ/+61+byvkJBTy0u4ZxBegXSW+5jvrpXtmZMD2RVUcxyh+1PlaMyfm3o4CNmYbxOmrM3Q==";

    /// <summary>
    ///     Returns a value indicating whether the manifest contains a valid detached signature.
    /// </summary>
    /// <param name="root">The root JSON manifest object.</param>
    /// <returns><see langword="true" /> when the signature is present and valid.</returns>
    public static bool HasValidSignature(JsonElement root)
    {
        if (!root.TryGetProperty("signature", out var signatureElement)
            || signatureElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        if (!root.TryGetProperty("plugins", out var pluginsElement))
        {
            return false;
        }

        var signatureBase64 = signatureElement.GetString();
        if (string.IsNullOrWhiteSpace(signatureBase64))
        {
            return false;
        }

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(signatureBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        var signedPayload = Encoding.UTF8.GetBytes(pluginsElement.GetRawText());
        var publicKeyBytes = Convert.FromBase64String(ManifestSigningPublicKeySubjectPublicKeyInfoBase64);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
        return ecdsa.VerifyData(signedPayload, signatureBytes, HashAlgorithmName.SHA256);
    }
}
