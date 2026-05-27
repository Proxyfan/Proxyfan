using Avalonia.Controls;

namespace Proxyfan.Client.Tools.Views;

/// <summary>
///     User control for the Remote Devices tool. Lists active remote devices connected
///     through the proxy with management actions (rename, disconnect, forget).
/// </summary>
public partial class RemoteDevicesView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of <see cref="RemoteDevicesView" />.
    /// </summary>
    public RemoteDevicesView()
    {
        InitializeComponent();
    }
}
