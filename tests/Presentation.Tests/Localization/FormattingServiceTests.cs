using Proxyfan.Presentation.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Tests.Localization;

/// <summary>
///     Tests for <see cref="FormattingService" />.
/// </summary>
[NotInParallel]
public sealed class FormattingServiceTests
{
    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDateTime" /> returns an empty
    ///     string for <see cref="DateTime.MinValue" />.
    /// </summary>
    [Test]
    public async Task FormatDateTime_MinValue_ReturnsEmptyString()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatDateTime(DateTime.MinValue);
        await Assert.That(formatted).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDateTime" /> uses the active
    ///     culture's general date/time pattern.
    /// </summary>
    [Test]
    [Arguments("en-US", "1/2/2024 3:04:05 AM")]
    [Arguments("de-DE", "02.01.2024 03:04:05")]
    public async Task FormatDateTime_GivenValue_UsesActiveCulturePattern(string cultureName, string expected)
    {
        var formattingService = CreateFormattingService(cultureName);
        var dateTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        var formatted = formattingService.FormatDateTime(dateTime);
        await Assert.That(formatted).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDateTimeOffset" /> returns an
    ///     empty string for <see cref="DateTimeOffset.MinValue" />.
    /// </summary>
    [Test]
    public async Task FormatDateTimeOffset_MinValue_ReturnsEmptyString()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatDateTimeOffset(DateTimeOffset.MinValue);
        await Assert.That(formatted).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDateTimeOffset" /> returns a
    ///     non-empty string for a real offset value.
    /// </summary>
    [Test]
    public async Task FormatDateTimeOffset_GivenValue_ReturnsNonEmpty()
    {
        var formattingService = CreateFormattingService("en-US");
        var offset = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
        var formatted = formattingService.FormatDateTimeOffset(offset);
        await Assert.That(formatted).IsNotEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatNumber" /> uses the active
    ///     culture's thousands separator.
    /// </summary>
    [Test]
    [Arguments("en-US", 1234567L, "1,234,567")]
    [Arguments("de-DE", 1234567L, "1.234.567")]
    [Arguments("fr-FR", 1234567L, "1\u202F234\u202F567")]
    public async Task FormatNumber_GivenValue_UsesActiveCultureSeparator(string cultureName, long value, string expected)
    {
        var formattingService = CreateFormattingService(cultureName);
        var formatted = formattingService.FormatNumber(value);
        await Assert.That(formatted).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatNumber" /> renders zero
    ///     without separators.
    /// </summary>
    [Test]
    public async Task FormatNumber_Zero_ReturnsZeroString()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatNumber(0L);
        await Assert.That(formatted).IsEqualTo("0");
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatFileSize" /> renders bytes
    ///     below 1 KB using whole-number formatting.
    /// </summary>
    [Test]
    [Arguments(0L, "0 B")]
    [Arguments(1L, "1 B")]
    [Arguments(1023L, "1,023 B")]
    public async Task FormatFileSize_Bytes_RendersUnitB(long bytes, string expected)
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatFileSize(bytes);
        await Assert.That(formatted).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatFileSize" /> rounds to one
    ///     decimal place in KB and uses the active culture's decimal separator.
    /// </summary>
    [Test]
    [Arguments("en-US", 1024L, "1.0 KB")]
    [Arguments("en-US", 1536L, "1.5 KB")]
    [Arguments("de-DE", 1536L, "1,5 KB")]
    public async Task FormatFileSize_Kilobytes_RendersUnitKB(string cultureName, long bytes, string expected)
    {
        var formattingService = CreateFormattingService(cultureName);
        var formatted = formattingService.FormatFileSize(bytes);
        await Assert.That(formatted).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatFileSize" /> rolls over into
    ///     MB, GB and TB units at the correct binary thresholds.
    /// </summary>
    [Test]
    [Arguments(1L * 1024L * 1024L, "1.0 MB")]
    [Arguments(1L * 1024L * 1024L * 1024L, "1.0 GB")]
    [Arguments(1L * 1024L * 1024L * 1024L * 1024L, "1.0 TB")]
    public async Task FormatFileSize_LargeUnits_PicksCorrectUnit(long bytes, string expected)
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatFileSize(bytes);
        await Assert.That(formatted).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatFileSize" /> prefixes a
    ///     leading minus sign for negative values.
    /// </summary>
    [Test]
    public async Task FormatFileSize_Negative_PrefixesMinus()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatFileSize(-2048L);
        await Assert.That(formatted).IsEqualTo("-2.0 KB");
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatFileSize" /> handles
    ///     <see cref="long.MinValue" /> without overflowing the absolute-value
    ///     calculation, formatting the full magnitude with a leading minus sign.
    /// </summary>
    [Test]
    public async Task FormatFileSize_LongMinValue_FormatsFullMagnitude()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatFileSize(long.MinValue);
        await Assert.That(formatted).IsEqualTo("-8,388,608.0 TB");
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDuration" /> reports
    ///     sub-second values in milliseconds.
    /// </summary>
    [Test]
    [Arguments("en-US", 0.5, "0.50 ms")]
    [Arguments("en-US", 12.5, "12.50 ms")]
    [Arguments("de-DE", 12.5, "12,50 ms")]
    public async Task FormatDuration_Milliseconds_UsesMsUnit(string cultureName, double milliseconds, string expected)
    {
        var formattingService = CreateFormattingService(cultureName);
        var formatted = formattingService.FormatDuration(TimeSpan.FromMilliseconds(milliseconds));
        await Assert.That(formatted).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDuration" /> reports
    ///     sub-minute values in seconds.
    /// </summary>
    [Test]
    public async Task FormatDuration_Seconds_UsesSUnit()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatDuration(TimeSpan.FromMilliseconds(2500));
        await Assert.That(formatted).IsEqualTo("2.50 s");
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDuration" /> reports
    ///     sub-hour values in minutes.
    /// </summary>
    [Test]
    public async Task FormatDuration_Minutes_UsesMinUnit()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatDuration(TimeSpan.FromSeconds(90));
        await Assert.That(formatted).IsEqualTo("1.50 min");
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDuration" /> reports values
    ///     of one hour and above in hours.
    /// </summary>
    [Test]
    public async Task FormatDuration_Hours_UsesHUnit()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatDuration(TimeSpan.FromMinutes(150));
        await Assert.That(formatted).IsEqualTo("2.50 h");
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDuration" /> prefixes a
    ///     leading minus sign for negative durations.
    /// </summary>
    [Test]
    public async Task FormatDuration_Negative_PrefixesMinus()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatDuration(TimeSpan.FromMilliseconds(-250));
        await Assert.That(formatted).IsEqualTo("-250.00 ms");
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.FormatDuration" /> handles
    ///     <see cref="TimeSpan.MinValue" /> without throwing
    ///     <see cref="OverflowException" />, since its absolute value exceeds
    ///     <see cref="TimeSpan.MaxValue" /> by one tick.
    /// </summary>
    [Test]
    public async Task FormatDuration_MinValue_DoesNotThrow()
    {
        var formattingService = CreateFormattingService("en-US");
        var formatted = formattingService.FormatDuration(TimeSpan.MinValue);
        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).StartsWith("-");
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.CurrentCulture" /> tracks the
    ///     underlying <see cref="LocalizationService" /> culture.
    /// </summary>
    [Test]
    public async Task CurrentCulture_AfterServiceCultureChange_TracksLocalizationService()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var localizationService = CreateLocalizationService("en-US");
            var formattingService = new FormattingService(localizationService);
            localizationService.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            await Assert.That(formattingService.CurrentCulture.Name).IsEqualTo("de-DE");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService" /> raises
    ///     <see cref="INotifyPropertyChanged.PropertyChanged" /> for the culture and
    ///     indexer when the underlying culture changes.
    /// </summary>
    [Test]
    public async Task PropertyChanged_AfterServiceCultureChange_RaisesEvents()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var localizationService = CreateLocalizationService("en-US");
            var formattingService = new FormattingService(localizationService);
            var raisedProperties = new List<string>();
            formattingService.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName ?? string.Empty);
            localizationService.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            await Assert.That(raisedProperties).Contains(nameof(FormattingService.CurrentCulture));
            await Assert.That(raisedProperties).Contains("Item[]");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService" /> does not raise events for
    ///     unrelated property changes on the underlying <see cref="LocalizationService" />.
    /// </summary>
    [Test]
    public async Task PropertyChanged_UnrelatedSourcePropertyName_IsIgnored()
    {
        var localizationService = CreateLocalizationService("en-US");
        var formattingService = new FormattingService(localizationService);
        var raised = false;
        formattingService.PropertyChanged += (_, _) => raised = true;
        InvokePrivatePropertyChanged(localizationService, "Unrelated");
        await Assert.That(raised).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="FormattingService.Dispose" /> detaches the underlying
    ///     event handler so culture changes no longer raise events.
    /// </summary>
    [Test]
    public async Task Dispose_AfterCall_DetachesFromLocalizationService()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var localizationService = CreateLocalizationService("en-US");
            var formattingService = new FormattingService(localizationService);
            formattingService.Dispose();
            var raised = false;
            formattingService.PropertyChanged += (_, _) => raised = true;
            localizationService.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            await Assert.That(raised).IsFalse();
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private static FormattingService CreateFormattingService(string cultureName)
    {
        var localizationService = CreateLocalizationService(cultureName);
        return new FormattingService(localizationService);
    }

    private static LocalizationService CreateLocalizationService(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        return new LocalizationService(culture);
    }

    private static void InvokePrivatePropertyChanged(LocalizationService localizationService, string propertyName)
    {
        var eventField = typeof(LocalizationService).GetField(nameof(LocalizationService.PropertyChanged),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var handler = (PropertyChangedEventHandler?)eventField?.GetValue(localizationService);
        handler?.Invoke(localizationService, new PropertyChangedEventArgs(propertyName));
    }
}
