using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Presentation;
using Proxyfan.Presentation.Localization;
using System.Globalization;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Tests;

/// <summary>
///     Tests for <see cref="LocalizeExtension" />.
/// </summary>
[NotInParallel]
public sealed class LocalizeExtensionTests
{
    /// <summary>
    ///     Verifies that providing a value with no key returns an empty string.
    /// </summary>
    [Test]
    public async Task ProvideValue_WithNoKey_ReturnsEmptyString()
    {
        ContainerLocator.Reset();

        try
        {
            var extension = new LocalizeExtension();
            var value = extension.ProvideValue(null!);

            await Assert.That(value).IsEqualTo(string.Empty);
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that providing a value with a whitespace key returns an empty string.
    /// </summary>
    [Test]
    public async Task ProvideValue_WithWhitespaceKey_ReturnsEmptyString()
    {
        ContainerLocator.Reset();

        try
        {
            var extension = new LocalizeExtension { Key = "   " };
            var value = extension.ProvideValue(null!);

            await Assert.That(value).IsEqualTo(string.Empty);
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that providing a value without a registered container returns the raw key.
    /// </summary>
    [Test]
    public async Task ProvideValue_WithKeyButNoContainer_ReturnsKey()
    {
        ContainerLocator.Reset();

        try
        {
            var extension = new LocalizeExtension { Key = "Some_Key" };
            var value = extension.ProvideValue(null!);

            await Assert.That(value).IsEqualTo("Some_Key");
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that providing a value without a registered <see cref="LocalizationService" />
    ///     returns the raw key.
    /// </summary>
    [Test]
    public async Task ProvideValue_WithContainerButNoService_ReturnsKey()
    {
        ContainerLocator.Reset();

        try
        {
            var services = new ServiceCollection();
            ServiceProvider provider = services.BuildServiceProvider();
            ContainerLocator.Set(() => provider);
            var extension = new LocalizeExtension { Key = "Some_Key" };

            var value = extension.ProvideValue(null!);

            await Assert.That(value).IsEqualTo("Some_Key");
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }

    /// <summary>
    ///     Verifies that providing a value with a registered service returns an Avalonia binding.
    /// </summary>
    [Test]
    public async Task ProvideValue_WithRegisteredService_ReturnsBinding()
    {
        ContainerLocator.Reset();

        try
        {
            var services = new ServiceCollection();
            var localizationService = new LocalizationService(CultureInfo.InvariantCulture);
            services.AddSingleton(localizationService);
            ServiceProvider provider = services.BuildServiceProvider();
            ContainerLocator.Set(() => provider);
            var extension = new LocalizeExtension { Key = "Some_Key" };

            var value = extension.ProvideValue(null!);

            await Assert.That(value).IsTypeOf<Avalonia.Data.Binding>();
        }
        finally
        {
            ContainerLocator.Reset();
        }
    }
}