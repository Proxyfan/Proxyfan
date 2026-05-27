using Avalonia.Controls;

namespace Proxyfan.Client.Tools.Views;

/// <summary>
///     User control for the Breakpoint tool. Hosts the editor for live breakpoint configuration
///     and the inspector for any currently-paused requests or responses.
/// </summary>
public partial class BreakpointView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of <see cref="BreakpointView" />.
    /// </summary>
    public BreakpointView()
    {
        InitializeComponent();
    }
}
