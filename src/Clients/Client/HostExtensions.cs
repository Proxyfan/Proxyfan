using Microsoft.Extensions.Hosting;

namespace Proxyfan.Client;

/// <summary>
///     Extension methods for <see cref="IHost" /> to provide additional functionality, such as a synchronous Stop method.
/// </summary>
public static class HostExtensions
{
    /// <param name="host">The host to stop.</param>
    extension(IHost host)
    {
        /// <summary>
        ///     Stops the host synchronously by calling StopAsync and blocking until it completes.
        /// </summary>
        public void Stop()
        {
            host.StopAsync().GetAwaiter().GetResult();
        }
    }
}