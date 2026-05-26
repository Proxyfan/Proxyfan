using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace Proxyfan.Presentation.Tests.Stubs;

internal sealed class StubResourceManager : ResourceManager
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _localizedValues;

    public StubResourceManager(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> localizedValues)
    {
        _localizedValues = localizedValues;
    }

    public override string? GetString(string name, CultureInfo? culture)
    {
        var cultureName = string.Empty;
        if (culture is not null)
        {
            cultureName = culture.Name;
        }

        if (_localizedValues.TryGetValue(cultureName, out var localizedValues) && localizedValues.TryGetValue(name, out var localizedValue))
        {
            return localizedValue;
        }

        if (_localizedValues.TryGetValue(string.Empty, out var invariantValues) && invariantValues.TryGetValue(name, out var invariantValue))
        {
            return invariantValue;
        }

        return null;
    }
}