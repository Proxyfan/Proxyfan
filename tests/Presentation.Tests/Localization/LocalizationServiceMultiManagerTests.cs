using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Tests.Stubs;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Tests.Localization;

/// <summary>
///     Additional tests for <see cref="LocalizationService" /> covering manager iteration branches.
/// </summary>
public sealed class LocalizationServiceMultiManagerTests
{
    /// <summary>
    ///     Verifies that with two registered managers, the second manager's value is returned
    ///     for keys not found in the first.
    /// </summary>
    [Test]
    public async Task Indexer_MultipleManagers_FallsThroughToSecondManagerForUnknownKey()
    {
        var firstValues = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en-US"] = new Dictionary<string, string> { ["KeyA"] = "From First" },
        };
        var secondValues = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en-US"] = new Dictionary<string, string> { ["KeyB"] = "From Second" },
        };
        var firstManager = new StubResourceManager(firstValues);
        var secondManager = new StubResourceManager(secondValues);
        var service = new LocalizationService(CultureInfo.GetCultureInfo("en-US"));
        service.RegisterManager(firstManager);
        service.RegisterManager(secondManager);

        await Assert.That(service["KeyA"]).IsEqualTo("From First");
        await Assert.That(service["KeyB"]).IsEqualTo("From Second");
        await Assert.That(service["MissingKey"]).IsEqualTo("MissingKey");
    }
}
