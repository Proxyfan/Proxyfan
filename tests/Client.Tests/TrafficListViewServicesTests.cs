using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Client.Traffic.Views;
using Proxyfan.Presentation;
using Proxyfan.Presentation.Dialogs;
using Proxyfan.Presentation.Localization;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="TrafficListViewServices" />.
/// </summary>
[NotInParallel]
public sealed class TrafficListViewServicesTests
{
    /// <summary>
    ///     Verifies that the localization helper returns <c>null</c> when no DI container has been registered.
    /// </summary>
    [Test]
    public async Task ResolveLocalizationService_NoContainer_ReturnsNull()
    {
        ContainerLocator.Reset();

        var result = TrafficListViewServices.ResolveLocalizationService();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that the prompt helper returns <c>null</c> when no DI container has been registered.
    /// </summary>
    [Test]
    public async Task ResolvePromptService_NoContainer_ReturnsNull()
    {
        ContainerLocator.Reset();

        var result = TrafficListViewServices.ResolvePromptService();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that, when a container provides <see cref="LocalizationService" />, it is returned.
    /// </summary>
    [Test]
    public async Task ResolveLocalizationService_WithContainer_ReturnsRegisteredService()
    {
        ContainerLocator.Reset();
        var services = new ServiceCollection();
        var localization = new LocalizationService(CultureInfo.InvariantCulture);
        services.AddSingleton(localization);
        var provider = services.BuildServiceProvider();
        ContainerLocator.Set(() => provider);

        var result = TrafficListViewServices.ResolveLocalizationService();

        await Assert.That(result).IsSameReferenceAs(localization);
        ContainerLocator.Reset();
    }

    /// <summary>
    ///     Verifies that, when a container provides <see cref="ITextPromptService" />, it is returned.
    /// </summary>
    [Test]
    public async Task ResolvePromptService_WithContainer_ReturnsRegisteredService()
    {
        ContainerLocator.Reset();
        var services = new ServiceCollection();
        var prompt = new StubPromptService();
        services.AddSingleton<ITextPromptService>(prompt);
        var provider = services.BuildServiceProvider();
        ContainerLocator.Set(() => provider);

        var result = TrafficListViewServices.ResolvePromptService();

        await Assert.That(result).IsSameReferenceAs(prompt);
        ContainerLocator.Reset();
    }

    private sealed class StubPromptService : ITextPromptService
    {
        public System.Threading.Tasks.Task<string?> PromptAsync(TextPromptRequest request, System.Threading.CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult<string?>(null);
        }
    }
}
