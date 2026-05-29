using System.IO;
using System.Net.Security;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Unit tests for <see cref="TransportLayerSecurityInterceptionPipesFactory" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptionPipesFactoryTests
{
    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptionPipesFactory.Create" />
    ///     returns a non-null bundle with all four pipe endpoints initialised.
    /// </summary>
    [Test]
    public async Task Create_GivenTwoSslStreams_ReturnsBundleWithAllFourPipes()
    {
        using var clientInner = new MemoryStream();
        using var serverInner = new MemoryStream();
        using var clientSecureStream = new SslStream(clientInner, leaveInnerStreamOpen: true);
        using var serverSecureStream = new SslStream(serverInner, leaveInnerStreamOpen: true);

        var pipes = TransportLayerSecurityInterceptionPipesFactory.Create(clientSecureStream, serverSecureStream);

        await Assert.That(pipes).IsNotNull();
        await Assert.That(pipes.ClientReader).IsNotNull();
        await Assert.That(pipes.ClientWriter).IsNotNull();
        await Assert.That(pipes.ServerReader).IsNotNull();
        await Assert.That(pipes.ServerWriter).IsNotNull();
    }
}
