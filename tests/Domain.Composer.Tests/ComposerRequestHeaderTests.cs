using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Composer;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.Composer.Tests;

public sealed class ComposerRequestHeaderTests
{
    [Test]
    public async Task Constructor_WhitespaceName_Throws()
    {
        await Assert.That(() => new ComposerRequestHeader("  ", "value"))
            .Throws<System.ArgumentException>();
    }

    [Test]
    public async Task Constructor_EmptyValue_AcceptsEmpty()
    {
        var header = new ComposerRequestHeader("X-Empty", string.Empty);

        await Assert.That(header.Name).IsEqualTo("X-Empty");
        await Assert.That(header.Value).IsEqualTo(string.Empty);
    }
}
