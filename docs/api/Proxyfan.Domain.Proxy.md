## Proxyfan\.Domain\.Proxy Namespace

| Classes | |
| :--- | :--- |
| [ProxyBindException](Proxyfan.Domain.Proxy.ProxyBindException.md 'Proxyfan\.Domain\.Proxy\.ProxyBindException') | The exception thrown when the proxy listener fails to bind to the configured port, for example because the port is already in use or access is denied\. |
| [ProxyOptions](Proxyfan.Domain.Proxy.ProxyOptions.md 'Proxyfan\.Domain\.Proxy\.ProxyOptions') | Strongly\-typed configuration options for the proxy listener, bound from the `proxy` section of the application configuration\. |
| [ProxyOptionsValidator](Proxyfan.Domain.Proxy.ProxyOptionsValidator.md 'Proxyfan\.Domain\.Proxy\.ProxyOptionsValidator') | Validates [ProxyOptions](Proxyfan.Domain.Proxy.ProxyOptions.md 'Proxyfan\.Domain\.Proxy\.ProxyOptions') at application startup to surface configuration errors before the proxy attempts to bind\. |

| Interfaces | |
| :--- | :--- |
| [IConnectionHandler](Proxyfan.Domain.Proxy.IConnectionHandler.md 'Proxyfan\.Domain\.Proxy\.IConnectionHandler') | Defines the contract for a component that handles an incoming proxy connection for a specific protocol\. |
| [IProxyConnection](Proxyfan.Domain.Proxy.IProxyConnection.md 'Proxyfan\.Domain\.Proxy\.IProxyConnection') | Represents an accepted TCP connection before protocol detection\. Provides duplex pipe transport and remote endpoint information\. |
| [IProxyListener](Proxyfan.Domain.Proxy.IProxyListener.md 'Proxyfan\.Domain\.Proxy\.IProxyListener') | Defines the lifecycle contract for a TCP proxy listener that binds to a port, accepts incoming connections, and dispatches each connection via a callback\. |
