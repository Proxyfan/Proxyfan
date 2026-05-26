using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IProxyConnection" /> backed by two in-process
///     <see cref="Pipe" /> instances so that both input (from test to handler) and output
///     (from handler to test) can be observed in tests.
/// </summary>
public sealed class StubFullDuplexProxyConnection : IProxyConnection
{
    private readonly Pipe _inputPipe;
    private readonly Pipe _outputPipe;

    /// <summary>
    ///     Gets the <see cref="PipeWriter" /> that feeds data into this connection's transport input.
    /// </summary>
    public PipeWriter InputWriter => _inputPipe.Writer;

    /// <summary>
    ///     Gets the <see cref="PipeReader" /> that reads data written to this connection's transport output.
    /// </summary>
    public PipeReader OutputReader => _outputPipe.Reader;

    /// <inheritdoc />
    public EndPoint RemoteEndPoint { get; }

    /// <inheritdoc />
    public IDuplexPipe Transport { get; }

    /// <summary>
    ///     Initializes a new <see cref="StubFullDuplexProxyConnection" />.
    /// </summary>
    public StubFullDuplexProxyConnection()
    {
        _inputPipe = new Pipe();
        _outputPipe = new Pipe();
        RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
        var transport = new FullDuplexPipe(_inputPipe.Reader, _outputPipe.Writer);
        Transport = transport;
    }

    /// <summary>
    ///     Reads all bytes written to the output by the handler under test and returns them as a byte array.
    ///     The output pipe writer must have been completed before calling this method.
    /// </summary>
    /// <returns>All bytes written to the output transport.</returns>
    public async Task<byte[]> ReadAllOutputAsync()
    {
        using var memoryStream = new MemoryStream();
        var outputStream = _outputPipe.Reader.AsStream();
        await outputStream.CopyToAsync(memoryStream).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private sealed class FullDuplexPipe : IDuplexPipe
    {
        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public FullDuplexPipe(PipeReader input, PipeWriter output)
        {
            Input = input;
            Output = output;
        }
    }
}
