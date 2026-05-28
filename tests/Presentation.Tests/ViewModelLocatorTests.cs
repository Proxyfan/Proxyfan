using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Presentation;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Tests;

/// <summary>
///     Tests for <see cref="ViewModelLocator" />.
/// </summary>
[NotInParallel]
public sealed class ViewModelLocatorTests
{
    static ViewModelLocatorTests()
    {
        AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
            .SetupWithoutStarting();
    }

    /// <summary>
    ///     Verifies that <see cref="ViewModelLocator.DataContextProperty" /> is registered.
    /// </summary>
    [Test]
    public async Task DataContextProperty_AfterStaticInit_IsRegistered()
    {
        await Assert.That(ViewModelLocator.DataContextProperty).IsNotNull();
        await Assert.That(ViewModelLocator.DataContextProperty.Name).IsEqualTo("DataContext");
    }

    /// <summary>
    ///     Verifies that <see cref="ViewModelLocator.SetDataContext" /> stores a <see cref="Type" /> on the target control.
    /// </summary>
    [Test]
    public async Task SetDataContext_WithType_StoresValueOnControl()
    {
        ContainerLocator.Reset();
        try
        {
            var control = new ContentControl();

            ViewModelLocator.SetDataContext(control, typeof(string));

            await Assert.That(ViewModelLocator.GetDataContext(control)).IsEqualTo(typeof(string));
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that setting a null <see cref="Type" /> leaves the control's data context unchanged.
    /// </summary>
    [Test]
    public async Task SetDataContext_WithNullType_DoesNotResolveFromContainer()
    {
        ContainerLocator.Reset();
        try
        {
            var control = new ContentControl();

            ViewModelLocator.SetDataContext(control, null);

            await Assert.That(control.DataContext).IsNull();
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that setting a <see cref="Type" /> without a registered container is a safe no-op for the data context.
    /// </summary>
    [Test]
    public async Task SetDataContext_WithTypeButNoContainer_DoesNotResolveDataContext()
    {
        ContainerLocator.Reset();
        try
        {
            var control = new ContentControl();

            ViewModelLocator.SetDataContext(control, typeof(SampleViewModel));

            await Assert.That(control.DataContext).IsNull();
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that setting a <see cref="Type" /> with a registered container resolves and assigns the data context.
    /// </summary>
    [Test]
    public async Task SetDataContext_WithTypeAndContainer_AssignsResolvedInstance()
    {
        ContainerLocator.Reset();
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<SampleViewModel>();
            ServiceProvider provider = services.BuildServiceProvider();
            ContainerLocator.Set(() => provider);
            var control = new ContentControl();

            ViewModelLocator.SetDataContext(control, typeof(SampleViewModel));

            await Assert.That(control.DataContext).IsTypeOf<SampleViewModel>();
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that switching the data context from a <see cref="Type" /> back to
    ///     <see langword="null" /> takes the early-return branch in the change handler and
    ///     clears the previously assigned data context value.
    /// </summary>
    [Test]
    public async Task SetDataContext_TypeThenNull_ResetsAttachedProperty()
    {
        ContainerLocator.Reset();
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<SampleViewModel>();
            ServiceProvider provider = services.BuildServiceProvider();
            ContainerLocator.Set(() => provider);
            var control = new ContentControl();
            ViewModelLocator.SetDataContext(control, typeof(SampleViewModel));

            ViewModelLocator.SetDataContext(control, null);

            await Assert.That(ViewModelLocator.GetDataContext(control)).IsNull();
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    private sealed class TestApplication : Application
    {
    }

    private sealed class SampleViewModel
    {
    }
}