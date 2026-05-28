using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Presentation.Dialogs;

namespace Proxyfan.Presentation.Tests.Dialogs;

/// <summary>
///     Tests for <see cref="TextPromptRequest" />.
/// </summary>
public sealed class TextPromptRequestTests
{
    /// <summary>
    ///     Verifies that all required properties are surfaced as initialised.
    /// </summary>
    [Test]
    public async Task Construct_AllProperties_AreAccessible()
    {
        var request = new TextPromptRequest
        {
            InitialValue = "hello",
            Label = "Label:",
            Title = "Window title",
        };

        await Assert.That(request.InitialValue).IsEqualTo("hello");
        await Assert.That(request.Label).IsEqualTo("Label:");
        await Assert.That(request.Title).IsEqualTo("Window title");
    }

    /// <summary>
    ///     Verifies that null initial values are honoured.
    /// </summary>
    [Test]
    public async Task Construct_NullInitialValue_IsAllowed()
    {
        var request = new TextPromptRequest
        {
            InitialValue = null,
            Label = "Label",
            Title = "Title",
        };

        await Assert.That(request.InitialValue).IsNull();
    }

    /// <summary>
    ///     Verifies that a stub <see cref="ITextPromptService" /> can echo back a configured value.
    /// </summary>
    [Test]
    public async Task PromptAsync_StubReturnsValue_ProvidesAccepted()
    {
        var promptService = new StubTextPromptService("entered");
        var request = new TextPromptRequest
        {
            InitialValue = null,
            Label = "Comment:",
            Title = "Comment",
        };

        var result = await promptService.PromptAsync(request, CancellationToken.None);

        await Assert.That(result).IsEqualTo("entered");
        await Assert.That(promptService.LastRequest).IsSameReferenceAs(request);
    }

    private sealed class StubTextPromptService : ITextPromptService
    {
        private readonly string? _response;

        public StubTextPromptService(string? response)
        {
            _response = response;
        }

        public TextPromptRequest? LastRequest { get; private set; }

        public Task<string?> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }
}
