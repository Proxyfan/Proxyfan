using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     A single row in the <see cref="KeyboardShortcutsViewModel" /> grid. Bundles the
///     <see cref="ShortcutAction" /> identifier with its human-readable label and the
///     formatted text of the currently bound gesture.
/// </summary>
public sealed partial class KeyboardShortcutEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _actionLabel;
    [ObservableProperty]
    private string _gestureText;
    [ObservableProperty]
    private bool _isRecording;

    /// <summary>
    ///     Gets the action identifier.
    /// </summary>
    public ShortcutAction Action { get; }

    /// <summary>
    ///     Initializes a new <see cref="KeyboardShortcutEntryViewModel" /> for the supplied
    ///     action with the supplied gesture text.
    /// </summary>
    /// <param name="action">The action identifier.</param>
    /// <param name="actionLabel">The human-readable label.</param>
    /// <param name="gestureText">The formatted gesture text.</param>
    public KeyboardShortcutEntryViewModel(ShortcutAction action, string actionLabel, string gestureText)
    {
        Action = action;
        _actionLabel = actionLabel;
        _gestureText = gestureText;
        _isRecording = false;
    }

    /// <summary>
    ///     Updates the displayed gesture text from the supplied gesture.
    /// </summary>
    /// <param name="gesture">The bound gesture.</param>
    public void UpdateGesture(KeyboardGesture gesture)
    {
        GestureText = gesture.ToString();
    }
}
