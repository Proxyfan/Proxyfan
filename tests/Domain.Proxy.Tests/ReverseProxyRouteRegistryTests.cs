using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="ReverseProxyRouteRegistry" />.
/// </summary>
public sealed class ReverseProxyRouteRegistryTests
{
    /// <summary>
    ///     Verifies an empty registry returns no routes.
    /// </summary>
    [Test]
    public async Task Routes_NewRegistry_IsEmpty()
    {
        var registry = new ReverseProxyRouteRegistry();
        await Assert.That(registry.Routes).IsEmpty();
    }

    /// <summary>
    ///     Verifies adding a route returns true and the route is visible in Routes.
    /// </summary>
    [Test]
    public async Task CanAdd_NewRoute_ReturnsTrueAndStoresRoute()
    {
        var registry = new ReverseProxyRouteRegistry();
        var route = CreateRoute("api", listenPort: 9000);

        var added = registry.CanAdd(route);

        await Assert.That(added).IsTrue();
        await Assert.That(registry.Routes.Count).IsEqualTo(1);
        await Assert.That(registry.Routes[0].Identifier).IsEqualTo("api");
    }

    /// <summary>
    ///     Verifies adding a route with a duplicate identifier returns false.
    /// </summary>
    [Test]
    public async Task CanAdd_DuplicateIdentifier_ReturnsFalse()
    {
        var registry = new ReverseProxyRouteRegistry();
        registry.CanAdd(CreateRoute("api", listenPort: 9000));

        var addedAgain = registry.CanAdd(CreateRoute("api", listenPort: 9001));

        await Assert.That(addedAgain).IsFalse();
        await Assert.That(registry.Routes.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies adding a route with a duplicate listen port returns false.
    /// </summary>
    [Test]
    public async Task CanAdd_DuplicateListenPort_ReturnsFalse()
    {
        var registry = new ReverseProxyRouteRegistry();
        registry.CanAdd(CreateRoute("api", listenPort: 9000));

        var addedAgain = registry.CanAdd(CreateRoute("other", listenPort: 9000));

        await Assert.That(addedAgain).IsFalse();
        await Assert.That(registry.Routes.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies removing an existing route returns true.
    /// </summary>
    [Test]
    public async Task HasRemoved_ExistingRoute_ReturnsTrue()
    {
        var registry = new ReverseProxyRouteRegistry();
        registry.CanAdd(CreateRoute("api", listenPort: 9000));

        var removed = registry.HasRemoved("api");

        await Assert.That(removed).IsTrue();
        await Assert.That(registry.Routes).IsEmpty();
    }

    /// <summary>
    ///     Verifies removing a missing route returns false.
    /// </summary>
    [Test]
    public async Task HasRemoved_MissingRoute_ReturnsFalse()
    {
        var registry = new ReverseProxyRouteRegistry();

        var removed = registry.HasRemoved("missing");

        await Assert.That(removed).IsFalse();
    }

    /// <summary>
    ///     Adding two routes with different identifiers and different ports both succeed and
    ///     both are present in <see cref="ReverseProxyRouteRegistry.Routes" />. Exercises the
    ///     loop-falls-through branch in <see cref="ReverseProxyRouteRegistry.CanAdd" /> where
    ///     the foreach over existing routes completes without short-circuiting.
    /// </summary>
    [Test]
    public async Task CanAdd_TwoDistinctRoutes_BothSucceed()
    {
        var registry = new ReverseProxyRouteRegistry();
        registry.CanAdd(CreateRoute("alpha", listenPort: 9000));

        var secondAdded = registry.CanAdd(CreateRoute("beta", listenPort: 9001));

        await Assert.That(secondAdded).IsTrue();
        await Assert.That(registry.Routes.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Removing one of several routes leaves the rest in place. Exercises the
    ///     iterate-past-non-matching-entries branch in
    ///     <see cref="ReverseProxyRouteRegistry.HasRemoved" />.
    /// </summary>
    [Test]
    public async Task HasRemoved_SecondOfThreeRoutes_RemovesOnlyMatch()
    {
        var registry = new ReverseProxyRouteRegistry();
        registry.CanAdd(CreateRoute("a", listenPort: 9000));
        registry.CanAdd(CreateRoute("b", listenPort: 9001));
        registry.CanAdd(CreateRoute("c", listenPort: 9002));

        var removed = registry.HasRemoved("b");

        await Assert.That(removed).IsTrue();
        await Assert.That(registry.Routes.Count).IsEqualTo(2);
        await Assert.That(registry.Routes[0].Identifier).IsEqualTo("a");
        await Assert.That(registry.Routes[1].Identifier).IsEqualTo("c");
    }

    private static ReverseProxyRoute CreateRoute(string identifier, int listenPort)
    {
        return new ReverseProxyRoute(
            identifier,
            $"Route {identifier}",
            listenPort,
            "backend.local",
            8080,
            ReverseProxyTransportLayerSecurityMode.None);
    }
}
