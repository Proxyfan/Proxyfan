using System;
using System.Threading;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Mutable scripting configuration. Holds the request- and response-phase source code,
///     an enabled flag, and the most recently compiled <see cref="IUserScript" /> reference.
///     Designed to be edited live from the user interface; the
///     <see cref="ActiveScript" /> getter exposes a lock-free snapshot to the pipeline.
/// </summary>
public sealed class MutableScriptingConfiguration
{
    /// <summary>
    ///     Raised whenever the enabled flag, the source text, or the compiled script changes.
    /// </summary>
    public event MutableScriptingConfigurationChanged? Changed;

    private readonly Lock _mutationLock;
    private volatile IUserScript? _activeScript;
    private volatile bool _isEnabled;
    private string _requestSource;
    private string _responseSource;

    /// <summary>
    ///     Gets the currently active compiled script, or <see langword="null" /> when no script
    ///     has been compiled successfully.
    /// </summary>
    public IUserScript? ActiveScript => _activeScript;

    /// <summary>
    ///     Gets a value indicating whether scripting is enabled.
    /// </summary>
    public bool IsEnabled => _isEnabled;

    /// <summary>
    ///     Gets the request-phase script source text.
    /// </summary>
    public string RequestSource
    {
        get
        {
            lock (_mutationLock)
            {
                return _requestSource;
            }
        }
    }

    /// <summary>
    ///     Gets the response-phase script source text.
    /// </summary>
    public string ResponseSource
    {
        get
        {
            lock (_mutationLock)
            {
                return _responseSource;
            }
        }
    }

    /// <summary>
    ///     Initializes a new <see cref="MutableScriptingConfiguration" /> with the supplied
    ///     <paramref name="isEnabled" /> flag and empty source text on both phases.
    /// </summary>
    /// <param name="isEnabled">Whether the configuration starts enabled.</param>
    public MutableScriptingConfiguration(bool isEnabled)
    {
        _isEnabled = isEnabled;
        _requestSource = string.Empty;
        _responseSource = string.Empty;
        var mutationLock = new Lock();
        _mutationLock = mutationLock;
    }

    /// <summary>
    ///     Clears the active compiled script. Used after editing the source text to ensure
    ///     stale compilations don't continue to run until the user explicitly recompiles.
    /// </summary>
    public void ClearActiveScript()
    {
        lock (_mutationLock)
        {
            if (_activeScript is null)
            {
                return;
            }

            _activeScript = null;
        }

        Changed?.Invoke();
    }

    /// <summary>
    ///     Replaces the active compiled script with <paramref name="script" />. Pass
    ///     <see langword="null" /> to clear.
    /// </summary>
    /// <param name="script">The compiled script reference (may be null).</param>
    public void SetActiveScript(IUserScript? script)
    {
        lock (_mutationLock)
        {
            if (ReferenceEquals(_activeScript, script))
            {
                return;
            }

            _activeScript = script;
        }

        Changed?.Invoke();
    }

    /// <summary>
    ///     Sets the enabled flag, clearing the active script reference as a side effect when
    ///     disabling so the pipeline immediately stops running it.
    /// </summary>
    /// <param name="isEnabled">The new enabled state.</param>
    public void SetEnabled(bool isEnabled)
    {
        lock (_mutationLock)
        {
            if (_isEnabled == isEnabled)
            {
                return;
            }

            _isEnabled = isEnabled;
        }

        Changed?.Invoke();
    }

    /// <summary>
    ///     Replaces the request-phase source code. Editing the source clears the active
    ///     compiled script reference so the previous compilation is not silently retained.
    /// </summary>
    /// <param name="source">The new request-phase script source.</param>
    public void SetRequestSource(string source)
    {
        lock (_mutationLock)
        {
            if (string.Equals(_requestSource, source, StringComparison.Ordinal))
            {
                return;
            }

            _requestSource = source;
            _activeScript = null;
        }

        Changed?.Invoke();
    }

    /// <summary>
    ///     Replaces the response-phase source code. Editing the source clears the active
    ///     compiled script reference so the previous compilation is not silently retained.
    /// </summary>
    /// <param name="source">The new response-phase script source.</param>
    public void SetResponseSource(string source)
    {
        lock (_mutationLock)
        {
            if (string.Equals(_responseSource, source, StringComparison.Ordinal))
            {
                return;
            }

            _responseSource = source;
            _activeScript = null;
        }

        Changed?.Invoke();
    }
}
