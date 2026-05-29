using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginContextResults" />.
/// </summary>
public sealed class PluginContextResultsTests
{
    /// <summary>
    ///     Verifies that Success wraps the supplied context.
    /// </summary>
    [Test]
    public async Task Success_GivenContext_IsSuccessTrue()
    {
        var hostAssemblyPath = typeof(PluginContextResultsTests).Assembly.Location;
        var context = new PluginLoadContext(hostAssemblyPath);
        try
        {
            var result = PluginContextResults.Success(context);

            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Context).IsSameReferenceAs(context);
            await Assert.That(result.ErrorMessage).IsNull();
        }
        finally
        {
            PluginLoadContextUnloader.Unload(context);
        }
    }

    /// <summary>
    ///     Verifies that Failure carries the supplied error message.
    /// </summary>
    [Test]
    public async Task Failure_GivenError_IsSuccessFalse()
    {
        var result = PluginContextResults.Failure("boom");

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Context).IsNull();
        await Assert.That(result.ErrorMessage).IsEqualTo("boom");
    }
}
