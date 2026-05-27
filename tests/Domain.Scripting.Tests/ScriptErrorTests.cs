using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests for <see cref="ScriptError" />.
/// </summary>
public sealed class ScriptErrorTests
{
    /// <summary>
    ///     Verifies that the constructor stores the supplied code and message.
    /// </summary>
    [Test]
    public async Task Constructor_GivenCodeAndMessage_StoresCodeAndMessage()
    {
        var error = new ScriptError("SCRIPT_COMPILE", "compile error");

        await Assert.That(error.Code).IsEqualTo("SCRIPT_COMPILE");
        await Assert.That(error.Message).IsEqualTo("compile error");
    }
}
