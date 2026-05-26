using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Presentation;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Tests;

/// <summary>
///     Tests for <see cref="ContainerLocator" />.
///     Covers initialization, reset behavior, and lazy factory evaluation.
/// </summary>
[NotInParallel]
public sealed class ContainerLocatorTests
{
    /// <summary>
    ///     Verifies that <see cref="ContainerLocator.Current" /> is <see langword="null" /> before initialization.
    ///     Also verifies that the uninitialized state can be observed safely.
    /// </summary>
    [Test]
    public async Task Current_BeforeSet_IsNull()
    {
        ContainerLocator.Reset();

        try
        {
            await Assert.That(ContainerLocator.Current).IsNull();
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that <see cref="ContainerLocator.Reset" /> clears a previously resolved provider.
    ///     Also verifies that the locator returns to its uninitialized state.
    /// </summary>
    [Test]
    public async Task Reset_WhenCalled_ClearsCurrentServiceProvider()
    {
        ContainerLocator.Reset();

        try
        {
            var provider = CreateServiceProvider();
            ContainerLocator.Set(() => provider);
            _ = ContainerLocator.Current;

            ContainerLocator.Reset();

            await Assert.That(ContainerLocator.Current).IsNull();
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that <see cref="ContainerLocator.Set" /> stores the provided service-provider factory.
    ///     Also verifies that the resolved provider becomes available through <see cref="ContainerLocator.Current" />.
    /// </summary>
    [Test]
    public async Task Set_WhenCalled_SetsCurrentServiceProvider()
    {
        ContainerLocator.Reset();

        try
        {
            var provider = CreateServiceProvider();
            ContainerLocator.Set(() => provider);
            var current = ContainerLocator.Current;

            await Assert.That(current).IsSameReferenceAs(provider);
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that the registered factory is evaluated lazily on first access.
    ///     Also verifies that the resolved provider is cached across subsequent accesses.
    /// </summary>
    [Test]
    public async Task Current_AccessedTwice_InvokesFactoryOnce()
    {
        ContainerLocator.Reset();

        try
        {
            var invocationCount = 0;
            var provider = CreateServiceProvider();
            ContainerLocator.Set(() =>
            {
                invocationCount++;
                return provider;
            });

            _ = ContainerLocator.Current;
            _ = ContainerLocator.Current;

            await Assert.That(invocationCount).IsEqualTo(1);
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        return provider;
    }
}