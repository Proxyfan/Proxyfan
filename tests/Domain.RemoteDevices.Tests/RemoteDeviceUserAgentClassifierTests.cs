using System.Threading.Tasks;
using Proxyfan.Domain.RemoteDevices;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.RemoteDevices.Tests;

public sealed class RemoteDeviceUserAgentClassifierTests
{
    [Test]
    public async Task Classify_Null_ReturnsUnknown()
    {
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(null)).IsEqualTo(RemoteDeviceKind.Unknown);
    }

    [Test]
    public async Task Classify_EmptyString_ReturnsUnknown()
    {
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify("")).IsEqualTo(RemoteDeviceKind.Unknown);
    }

    [Test]
    public async Task Classify_WhitespaceString_ReturnsUnknown()
    {
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify("   ")).IsEqualTo(RemoteDeviceKind.Unknown);
    }

    [Test]
    public async Task Classify_iPhoneUserAgent_ReturnsIos()
    {
        const string userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.Ios);
    }

    [Test]
    public async Task Classify_iPadUserAgent_ReturnsIos()
    {
        const string userAgent = "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X)";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.Ios);
    }

    [Test]
    public async Task Classify_iPodUserAgent_ReturnsIos()
    {
        const string userAgent = "Mozilla/5.0 (iPod touch; CPU iPhone OS)";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.Ios);
    }

    [Test]
    public async Task Classify_AndroidUserAgent_ReturnsAndroid()
    {
        const string userAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 8)";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.Android);
    }

    [Test]
    public async Task Classify_WindowsUserAgent_ReturnsWindows()
    {
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.Windows);
    }

    [Test]
    public async Task Classify_MacOsUserAgent_ReturnsMacOs()
    {
        const string userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4)";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.MacOs);
    }

    [Test]
    public async Task Classify_LinuxX11UserAgent_ReturnsLinux()
    {
        const string userAgent = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64) Gecko/20100101 Firefox/120.0";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.Linux);
    }

    [Test]
    public async Task Classify_UnknownUserAgent_ReturnsUnknown()
    {
        const string userAgent = "curl/8.5.0";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.Unknown);
    }

    [Test]
    public async Task Classify_AndroidWebView_PrefersAndroidOverLinux()
    {
        const string userAgent = "Mozilla/5.0 (Linux; U; Android 12)";
        await Assert.That(RemoteDeviceUserAgentClassifier.Classify(userAgent)).IsEqualTo(RemoteDeviceKind.Android);
    }
}
