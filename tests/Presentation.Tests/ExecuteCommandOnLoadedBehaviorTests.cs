using Avalonia.Controls;
using Avalonia.Interactivity;
using Proxyfan.Presentation;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Proxyfan.Presentation.Tests;

/// <summary>
///     Tests for <see cref="ExecuteCommandOnLoadedBehavior" />.
/// </summary>
[NotInParallel]
public sealed class ExecuteCommandOnLoadedBehaviorTests
{
    static ExecuteCommandOnLoadedBehaviorTests()
    {
        AvaloniaHeadlessFixture.EnsureInitialized();
    }

    /// <summary>
    ///     Verifies that <see cref="ExecuteCommandOnLoadedBehavior.CommandProperty" /> is registered.
    /// </summary>
    [Test]
    public async Task CommandProperty_AfterStaticInit_IsRegistered()
    {
        await Assert.That(ExecuteCommandOnLoadedBehavior.CommandProperty).IsNotNull();
        await Assert.That(ExecuteCommandOnLoadedBehavior.CommandProperty.Name).IsEqualTo("Command");
    }

    /// <summary>
    ///     Verifies that <see cref="ExecuteCommandOnLoadedBehavior.SetCommand" /> stores the
    ///     command on the target control so <see cref="ExecuteCommandOnLoadedBehavior.GetCommand" />
    ///     returns the same instance.
    /// </summary>
    [Test]
    public async Task SetCommand_WithCommand_StoresValueOnControl()
    {
        var control = new ContentControl();
        var command = new StubCommand();

        ExecuteCommandOnLoadedBehavior.SetCommand(control, command);

        await Assert.That(ExecuteCommandOnLoadedBehavior.GetCommand(control)).IsSameReferenceAs(command);
    }

    /// <summary>
    ///     Verifies that setting <see langword="null" /> clears the attached property.
    /// </summary>
    [Test]
    public async Task SetCommand_WithNull_ClearsAttachedProperty()
    {
        var control = new ContentControl();
        var command = new StubCommand();
        ExecuteCommandOnLoadedBehavior.SetCommand(control, command);

        ExecuteCommandOnLoadedBehavior.SetCommand(control, null);

        await Assert.That(ExecuteCommandOnLoadedBehavior.GetCommand(control)).IsNull();
    }

    /// <summary>
    ///     Verifies that when the control raises <see cref="Control.LoadedEvent" /> the
    ///     bound command is executed exactly once.
    /// </summary>
    [Test]
    public async Task SetCommand_ControlRaisesLoadedEvent_ExecutesCommandOnce()
    {
        var control = new ContentControl();
        var command = new StubCommand();
        ExecuteCommandOnLoadedBehavior.SetCommand(control, command);

        control.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));

        await Assert.That(command.ExecuteCallCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that when the command returns <see langword="false" /> from
    ///     <see cref="ICommand.CanExecute" /> the command is not executed.
    /// </summary>
    [Test]
    public async Task SetCommand_CommandCannotExecute_DoesNotExecuteCommand()
    {
        var control = new ContentControl();
        var command = new StubCommand(canExecute: false);
        ExecuteCommandOnLoadedBehavior.SetCommand(control, command);

        control.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));

        await Assert.That(command.ExecuteCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that after clearing the command the <see cref="Control.LoadedEvent" />
    ///     no longer executes the previously bound command.
    /// </summary>
    [Test]
    public async Task SetCommand_CommandClearedBeforeLoad_DoesNotExecutePreviousCommand()
    {
        var control = new ContentControl();
        var command = new StubCommand();
        ExecuteCommandOnLoadedBehavior.SetCommand(control, command);

        ExecuteCommandOnLoadedBehavior.SetCommand(control, null);
        control.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));

        await Assert.That(command.ExecuteCallCount).IsEqualTo(0);
    }

    private sealed class StubCommand : ICommand
    {
        private readonly bool _canExecute;
        private int _executeCallCount;

        public int ExecuteCallCount => _executeCallCount;

        public StubCommand(bool canExecute = true)
        {
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter)
        {
            return _canExecute;
        }

        public void Execute(object? parameter)
        {
            _executeCallCount++;
        }
    }
}
