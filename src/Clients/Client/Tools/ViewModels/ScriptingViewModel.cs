using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Scripting;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Scripting tool window. Binds to
///     <see cref="MutableScriptingConfiguration" /> and uses
///     <see cref="IUserScriptCompiler" /> to compile the request and response source
///     into an active <see cref="IUserScript" /> that the proxy pipeline will execute.
/// </summary>
public sealed partial class ScriptingViewModel : ObservableObject, IDisposable
{
    private const string ScriptDisplayName = "User Script";
    private readonly IUserScriptCompiler _compiler;
    private readonly MutableScriptingConfiguration _configuration;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _compilationStatus;
    [ObservableProperty]
    private bool _isCompilationSuccessful;
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private string _requestSource;
    [ObservableProperty]
    private string _responseSource;

    /// <summary>
    ///     Gets the observable collection of diagnostics emitted by the most recent compile.
    /// </summary>
    public ObservableCollection<ScriptDiagnosticViewModel> Diagnostics { get; }

    /// <summary>
    ///     Initializes a new <see cref="ScriptingViewModel" />.
    /// </summary>
    /// <param name="configuration">Mutable scripting configuration to bind to.</param>
    /// <param name="compiler">Compiler used to turn source text into a runnable script.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public ScriptingViewModel(
        MutableScriptingConfiguration configuration,
        IUserScriptCompiler compiler,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _configuration = configuration;
        _compiler = compiler;
        _userInterfaceScheduler = userInterfaceScheduler;
        _isEnabled = configuration.IsEnabled;
        _requestSource = configuration.RequestSource;
        _responseSource = configuration.ResponseSource;
        _compilationStatus = string.Empty;
        _isCompilationSuccessful = configuration.ActiveScript is not null;
        Diagnostics = [];
        _configuration.Changed += OnConfigurationChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _configuration.Changed -= OnConfigurationChanged;
    }

    [RelayCommand]
    private void ClearScript()
    {
        _configuration.ClearActiveScript();
        Diagnostics.Clear();
        IsCompilationSuccessful = false;
        CompilationStatus = string.Empty;
    }

    [RelayCommand]
    private void Compile()
    {
        var requestSource = RequestSource;
        var responseSource = ResponseSource;
        _configuration.SetRequestSource(requestSource);
        _configuration.SetResponseSource(responseSource);
        var result = _compiler.Compile(ScriptDisplayName, requestSource, responseSource);
        Diagnostics.Clear();
        foreach (var diagnostic in result.Diagnostics)
        {
            var viewModel = new ScriptDiagnosticViewModel(diagnostic);
            Diagnostics.Add(viewModel);
        }

        if (result.IsSuccess && result.Script is not null)
        {
            _configuration.SetActiveScript(result.Script);
            IsCompilationSuccessful = true;
            CompilationStatus = "OK";
        }
        else
        {
            _configuration.ClearActiveScript();
            IsCompilationSuccessful = false;
            CompilationStatus = "Failed";
        }
    }

    private void OnConfigurationChanged()
    {
        _userInterfaceScheduler.Post(SyncFromConfiguration);
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_configuration.IsEnabled == value)
        {
            return;
        }

        _configuration.SetEnabled(value);
    }

    private void SyncFromConfiguration()
    {
        if (IsEnabled != _configuration.IsEnabled)
        {
            IsEnabled = _configuration.IsEnabled;
        }

        if (RequestSource != _configuration.RequestSource)
        {
            RequestSource = _configuration.RequestSource;
        }

        if (ResponseSource != _configuration.ResponseSource)
        {
            ResponseSource = _configuration.ResponseSource;
        }
    }
}
