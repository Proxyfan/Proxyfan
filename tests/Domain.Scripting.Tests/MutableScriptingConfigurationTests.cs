using Proxyfan.Domain.Scripting.Tests.Stubs;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests for <see cref="MutableScriptingConfiguration" />.
/// </summary>
public sealed class MutableScriptingConfigurationTests
{
    /// <summary>
    ///     Verifies that <see cref="MutableScriptingConfiguration.ClearActiveScript" /> is a no-op
    ///     when no script is currently active and does not raise the Changed event.
    /// </summary>
    [Test]
    public async Task ClearActiveScript_NoActiveScript_DoesNothing()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: false);
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.ClearActiveScript();

        await Assert.That(raised).IsFalse();
        await Assert.That(configuration.ActiveScript).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="MutableScriptingConfiguration.ClearActiveScript" /> clears
    ///     the active script reference and raises the Changed event.
    /// </summary>
    [Test]
    public async Task ClearActiveScript_WithActiveScript_ClearsAndRaises()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        var stubScript = new StubUserScript("test");
        configuration.SetActiveScript(stubScript);
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.ClearActiveScript();

        await Assert.That(configuration.ActiveScript).IsNull();
        await Assert.That(raised).IsTrue();
    }

    /// <summary>
    ///     Verifies that a freshly constructed configuration with the disabled flag is disabled.
    /// </summary>
    [Test]
    public async Task Constructor_DisabledFlag_StartsDisabled()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: false);

        await Assert.That(configuration.IsEnabled).IsFalse();
        await Assert.That(configuration.ActiveScript).IsNull();
        await Assert.That(configuration.RequestSource).IsEqualTo(string.Empty);
        await Assert.That(configuration.ResponseSource).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a freshly constructed configuration with the enabled flag is enabled.
    /// </summary>
    [Test]
    public async Task Constructor_EnabledFlag_StartsEnabled()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);

        await Assert.That(configuration.IsEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies that setting the same active script twice does not raise the event a second time.
    /// </summary>
    [Test]
    public async Task SetActiveScript_SameInstance_DoesNotRaiseAgain()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        var stubScript = new StubUserScript("script");
        configuration.SetActiveScript(stubScript);
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.SetActiveScript(stubScript);

        await Assert.That(raised).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="MutableScriptingConfiguration.SetActiveScript" /> stores
    ///     the supplied reference and raises the Changed event.
    /// </summary>
    [Test]
    public async Task SetActiveScript_WithScript_StoresAndRaises()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        var stubScript = new StubUserScript("script");
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.SetActiveScript(stubScript);

        await Assert.That(configuration.ActiveScript).IsSameReferenceAs(stubScript);
        await Assert.That(raised).IsTrue();
    }

    /// <summary>
    ///     Verifies that setting the enabled flag to the same value is a no-op.
    /// </summary>
    [Test]
    public async Task SetEnabled_SameValue_DoesNothing()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: false);
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.SetEnabled(false);

        await Assert.That(raised).IsFalse();
    }

    /// <summary>
    ///     Verifies that toggling the enabled flag updates the property and raises the event.
    /// </summary>
    [Test]
    public async Task SetEnabled_ToggledValue_UpdatesAndRaises()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: false);
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.SetEnabled(true);

        await Assert.That(configuration.IsEnabled).IsTrue();
        await Assert.That(raised).IsTrue();
    }

    /// <summary>
    ///     Verifies that disabling scripting clears the active compiled script so the pipeline
    ///     immediately stops running it and the snapshot is not retained for a later enable.
    /// </summary>
    [Test]
    public async Task SetEnabled_DisablingWithActiveScript_ClearsActiveScript()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript("script"));
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.SetEnabled(false);

        await Assert.That(configuration.IsEnabled).IsFalse();
        await Assert.That(configuration.ActiveScript).IsNull();
        await Assert.That(raised).IsTrue();
    }

    /// <summary>
    ///     Verifies that setting the request source clears the active script reference.
    /// </summary>
    [Test]
    public async Task SetRequestSource_NewSource_ClearsActiveScript()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript("old"));

        configuration.SetRequestSource("Request.Headers.Set(\"X-Test\", \"1\");");

        await Assert.That(configuration.ActiveScript).IsNull();
        await Assert.That(configuration.RequestSource).IsEqualTo("Request.Headers.Set(\"X-Test\", \"1\");");
    }

    /// <summary>
    ///     Verifies that setting the request source to the same value is a no-op.
    /// </summary>
    [Test]
    public async Task SetRequestSource_SameValue_DoesNothing()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetRequestSource("source");
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.SetRequestSource("source");

        await Assert.That(raised).IsFalse();
    }

    /// <summary>
    ///     Verifies that setting the response source clears the active script reference.
    /// </summary>
    [Test]
    public async Task SetResponseSource_NewSource_ClearsActiveScript()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetActiveScript(new StubUserScript("old"));

        configuration.SetResponseSource("Response.Headers.Set(\"X-Trace\", \"1\");");

        await Assert.That(configuration.ActiveScript).IsNull();
        await Assert.That(configuration.ResponseSource).IsEqualTo("Response.Headers.Set(\"X-Trace\", \"1\");");
    }

    /// <summary>
    ///     Verifies that setting the response source to the same value is a no-op.
    /// </summary>
    [Test]
    public async Task SetResponseSource_SameValue_DoesNothing()
    {
        var configuration = new MutableScriptingConfiguration(isEnabled: true);
        configuration.SetResponseSource("source");
        var raised = false;
        configuration.Changed += () => raised = true;

        configuration.SetResponseSource("source");

        await Assert.That(raised).IsFalse();
    }
}
