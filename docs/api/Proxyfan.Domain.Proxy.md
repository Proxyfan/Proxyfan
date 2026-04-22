#### [Domain\.Proxy](index.md 'index')

## Proxyfan\.Domain\.Proxy Namespace

| Classes | |
| :--- | :--- |
| [ProxyAlreadyRunningError](Proxyfan.Domain.Proxy.ProxyAlreadyRunningError.md 'Proxyfan\.Domain\.Proxy\.ProxyAlreadyRunningError') | Error raised when `StartAsync` is called while the proxy is already running or starting\. |
| [ProxyBindError](Proxyfan.Domain.Proxy.ProxyBindError.md 'Proxyfan\.Domain\.Proxy\.ProxyBindError') | Error raised when the proxy listener fails to bind to the configured port\. |
| [ProxyBindException](Proxyfan.Domain.Proxy.ProxyBindException.md 'Proxyfan\.Domain\.Proxy\.ProxyBindException') | The exception thrown when the proxy listener fails to bind to the configured port, for example because the port is already in use or access is denied\. |
| [ProxyError](Proxyfan.Domain.Proxy.ProxyError.md 'Proxyfan\.Domain\.Proxy\.ProxyError') | Base record for all proxy\-specific domain errors\. |
| [ProxyFaultedError](Proxyfan.Domain.Proxy.ProxyFaultedError.md 'Proxyfan\.Domain\.Proxy\.ProxyFaultedError') | Error raised when a lifecycle operation fails due to an unexpected exception\. |
| [ProxyNotRunningError](Proxyfan.Domain.Proxy.ProxyNotRunningError.md 'Proxyfan\.Domain\.Proxy\.ProxyNotRunningError') | Error raised when `StopAsync` is called while the proxy is already stopped or stopping\. |
| [ProxyOptions](Proxyfan.Domain.Proxy.ProxyOptions.md 'Proxyfan\.Domain\.Proxy\.ProxyOptions') | Strongly\-typed configuration options for the proxy listener, bound from the `proxy` section of the application configuration\. |
| [ProxyOptionsValidator](Proxyfan.Domain.Proxy.ProxyOptionsValidator.md 'Proxyfan\.Domain\.Proxy\.ProxyOptionsValidator') | Validates [ProxyOptions](Proxyfan.Domain.Proxy.ProxyOptions.md 'Proxyfan\.Domain\.Proxy\.ProxyOptions') at application startup to surface configuration errors before the proxy attempts to bind\. |
| [ProxyServer](Proxyfan.Domain.Proxy.ProxyServer.md 'Proxyfan\.Domain\.Proxy\.ProxyServer') | Aggregate root that manages the complete proxy server lifecycle: configuration, start, stop, restart, and status reporting\. |

| Interfaces | |
| :--- | :--- |
| [IConnectionDispatcher](Proxyfan.Domain.Proxy.IConnectionDispatcher.md 'Proxyfan\.Domain\.Proxy\.IConnectionDispatcher') | Defines the contract for a component that accepts an incoming connection, detects its protocol, and routes it to the appropriate handler\. |
| [IConnectionHandler](Proxyfan.Domain.Proxy.IConnectionHandler.md 'Proxyfan\.Domain\.Proxy\.IConnectionHandler') | Defines the contract for a component that handles an incoming proxy connection for a specific protocol\. |
| [IProxyConnection](Proxyfan.Domain.Proxy.IProxyConnection.md 'Proxyfan\.Domain\.Proxy\.IProxyConnection') | Represents an accepted TCP connection before protocol detection\. Provides duplex pipe transport and remote endpoint information\. |
| [IProxyListener](Proxyfan.Domain.Proxy.IProxyListener.md 'Proxyfan\.Domain\.Proxy\.IProxyListener') | Defines the lifecycle contract for a TCP proxy listener that binds to a port, accepts incoming connections, and dispatches each connection via a callback\. |

| Enums | |
| :--- | :--- |
| [ProxyStatus](Proxyfan.Domain.Proxy.ProxyStatus.md 'Proxyfan\.Domain\.Proxy\.ProxyStatus') | Represents the current lifecycle state of the proxy server\. |
