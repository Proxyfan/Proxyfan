#### [Domain\.Proxy](index.md 'index')
### [Proxyfan\.Domain\.Proxy](Proxyfan.Domain.Proxy.md 'Proxyfan\.Domain\.Proxy')

## ProxyStatus Enum

Represents the current lifecycle state of the proxy server\.

```csharp
public enum ProxyStatus
```
### Fields

<a name='Proxyfan.Domain.Proxy.ProxyStatus.Stopped'></a>

`Stopped` 0

The proxy is not running and no port is bound\.

<a name='Proxyfan.Domain.Proxy.ProxyStatus.Starting'></a>

`Starting` 1

The proxy is in the process of starting \(binding port, initializing\)\.

<a name='Proxyfan.Domain.Proxy.ProxyStatus.Running'></a>

`Running` 2

The proxy is actively listening for and accepting connections\.

<a name='Proxyfan.Domain.Proxy.ProxyStatus.Stopping'></a>

`Stopping` 3

The proxy is in the process of shutting down gracefully\.

<a name='Proxyfan.Domain.Proxy.ProxyStatus.Faulted'></a>

`Faulted` 4

The proxy encountered an unrecoverable error and is not operational\.
A subsequent call to `StartAsync` will attempt recovery\.