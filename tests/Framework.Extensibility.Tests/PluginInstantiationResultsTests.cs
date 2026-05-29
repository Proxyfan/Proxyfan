using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginInstantiationResults" />.
/// </summary>
public sealed class PluginInstantiationResultsTests
{
    /// <summary>
    ///     Verifies that <see cref="PluginInstantiationResults.Success" /> produces a success result.
    /// </summary>
    [Test]
    public async Task Success_GivenPlugin_ProducesIsSuccessTrue()
    {
        var plugin = new StubPlugin();

        var result = PluginInstantiationResults.Success(plugin, null);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Plugin).IsSameReferenceAs(plugin);
        await Assert.That(result.ErrorMessage).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="PluginInstantiationResults.Failure" /> produces a failure result.
    /// </summary>
    [Test]
    public async Task Failure_GivenError_ProducesIsSuccessFalse()
    {
        var result = PluginInstantiationResults.Failure("boom");

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Plugin).IsNull();
        await Assert.That(result.ErrorMessage).IsEqualTo("boom");
    }

    private sealed class StubPlugin : IProxyfanPlugin
    {
        public PluginMetadata Metadata { get; } = new("id", "n", "v", "a", "d", "1.0");

        public void Initialize(IPluginHost host)
        {
        }
    }
}
