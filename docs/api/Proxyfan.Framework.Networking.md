## Proxyfan\.Framework\.Networking Namespace

| Classes | |
| :--- | :--- |
| [ConnectionDispatcher](Proxyfan.Framework.Networking.ConnectionDispatcher.md 'Proxyfan\.Framework\.Networking\.ConnectionDispatcher') | Reads the first bytes of an accepted connection to detect the protocol and dispatches to the first registered [Proxyfan\.Domain\.Proxy\.IConnectionHandler](https://learn.microsoft.com/en-us/dotnet/api/proxyfan.domain.proxy.iconnectionhandler 'Proxyfan\.Domain\.Proxy\.IConnectionHandler') that accepts those bytes\. |
| [TcpProxyListener](Proxyfan.Framework.Networking.TcpProxyListener.md 'Proxyfan\.Framework\.Networking\.TcpProxyListener') | A TCP proxy listener that binds to a configurable port and accepts incoming connections asynchronously, handing each connection to a caller\-supplied callback for further processing\. |
