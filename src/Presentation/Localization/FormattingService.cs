using System;
using System.ComponentModel;
using System.Globalization;

namespace Proxyfan.Presentation.Localization;

/// <summary>
///     Provides locale-aware formatting helpers for dates, numbers, file sizes and
///     durations. Listens to <see cref="LocalizationService" /> culture changes and
///     raises <see cref="INotifyPropertyChanged.PropertyChanged" /> so that
///     value converters bound to the service can refresh formatted values.
/// </summary>
public sealed class FormattingService : INotifyPropertyChanged, IDisposable
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private const ulong BytesPerGigabyte = 1024UL * 1024UL * 1024UL;
    private const ulong BytesPerKilobyte = 1024UL;
    private const ulong BytesPerMegabyte = 1024UL * 1024UL;
    private const ulong BytesPerTerabyte = 1024UL * 1024UL * 1024UL * 1024UL;
    private const string DefaultDateTimeFormat = "G";
    private const string IndexerPropertyName = "Item[]";
    private readonly LocalizationService _localizationService;

    /// <summary>
    ///     Gets the culture used for all formatting operations.
    /// </summary>
    public CultureInfo CurrentCulture => _localizationService.CurrentCulture;

    /// <summary>
    ///     Initializes a new <see cref="FormattingService" />.
    /// </summary>
    /// <param name="localizationService">
    ///     The localization service whose culture drives formatting.
    /// </param>
    public FormattingService(LocalizationService localizationService)
    {
        _localizationService = localizationService;
        _localizationService.PropertyChanged += OnLocalizationChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _localizationService.PropertyChanged -= OnLocalizationChanged;
    }

    /// <summary>
    ///     Formats a <see cref="DateTime" /> using the active culture's general date/time
    ///     pattern. Returns an empty string for <see cref="DateTime.MinValue" />.
    /// </summary>
    /// <param name="value">The date/time value to format.</param>
    /// <returns>The locale-formatted representation.</returns>
    public string FormatDateTime(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return string.Empty;
        }

        return value.ToString(DefaultDateTimeFormat, CurrentCulture);
    }

    /// <summary>
    ///     Formats a <see cref="DateTimeOffset" /> using the active culture's general
    ///     date/time pattern. The local-time representation is used.
    /// </summary>
    /// <param name="value">The date/time value to format.</param>
    /// <returns>The locale-formatted representation.</returns>
    public string FormatDateTimeOffset(DateTimeOffset value)
    {
        if (value == DateTimeOffset.MinValue)
        {
            return string.Empty;
        }

        return value.LocalDateTime.ToString(DefaultDateTimeFormat, CurrentCulture);
    }

    /// <summary>
    ///     Formats a <see cref="TimeSpan" /> as a human-readable duration string,
    ///     selecting hours/minutes/seconds/milliseconds units based on magnitude.
    ///     The numeric component is formatted with the active culture's decimal
    ///     separator. Always returns a non-negative magnitude prefixed with
    ///     <c>-</c> when <paramref name="value" /> is negative.
    /// </summary>
    /// <param name="value">The duration to format.</param>
    /// <returns>The locale-formatted duration.</returns>
    public string FormatDuration(TimeSpan value)
    {
        var sign = value.Ticks < 0 ? "-" : string.Empty;
        var magnitude = value.Ticks < 0 ? value.Negate() : value;
        if (magnitude.TotalMilliseconds < 1.0)
        {
            return sign + magnitude.TotalMilliseconds.ToString("F2", CurrentCulture) + " ms";
        }

        if (magnitude.TotalSeconds < 1.0)
        {
            return sign + magnitude.TotalMilliseconds.ToString("F2", CurrentCulture) + " ms";
        }

        if (magnitude.TotalMinutes < 1.0)
        {
            return sign + magnitude.TotalSeconds.ToString("F2", CurrentCulture) + " s";
        }

        if (magnitude.TotalHours < 1.0)
        {
            return sign + magnitude.TotalMinutes.ToString("F2", CurrentCulture) + " min";
        }

        return sign + magnitude.TotalHours.ToString("F2", CurrentCulture) + " h";
    }

    /// <summary>
    ///     Formats a byte count using binary units (B/KB/MB/GB/TB). Values below 1 KB
    ///     are reported as an integer count of bytes; larger values use one decimal
    ///     place formatted with the active culture's decimal separator. Negative
    ///     values are formatted with a leading minus sign.
    /// </summary>
    /// <param name="bytes">The byte count to format.</param>
    /// <returns>The locale-formatted byte count.</returns>
    public string FormatFileSize(long bytes)
    {
        var sign = bytes < 0 ? "-" : string.Empty;
        var magnitude = bytes < 0 ? unchecked((ulong)~bytes + 1UL) : (ulong)bytes;
        if (magnitude < BytesPerKilobyte)
        {
            return sign + magnitude.ToString("N0", CurrentCulture) + " B";
        }

        if (magnitude < BytesPerMegabyte)
        {
            var kilobytes = magnitude / (double)BytesPerKilobyte;
            return sign + kilobytes.ToString("N1", CurrentCulture) + " KB";
        }

        if (magnitude < BytesPerGigabyte)
        {
            var megabytes = magnitude / (double)BytesPerMegabyte;
            return sign + megabytes.ToString("N1", CurrentCulture) + " MB";
        }

        if (magnitude < BytesPerTerabyte)
        {
            var gigabytes = magnitude / (double)BytesPerGigabyte;
            return sign + gigabytes.ToString("N1", CurrentCulture) + " GB";
        }

        var terabytes = magnitude / (double)BytesPerTerabyte;
        return sign + terabytes.ToString("N1", CurrentCulture) + " TB";
    }

    /// <summary>
    ///     Formats an integer value using the active culture's thousand-separator
    ///     conventions.
    /// </summary>
    /// <param name="value">The integer value to format.</param>
    /// <returns>The locale-formatted integer.</returns>
    public string FormatNumber(long value)
    {
        return value.ToString("N0", CurrentCulture);
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        if (propertyChangedEventArgs.PropertyName != nameof(LocalizationService.CurrentCulture))
        {
            return;
        }

        OnPropertyChanged(nameof(CurrentCulture));
        OnPropertyChanged(IndexerPropertyName);
    }

    private void OnPropertyChanged(string propertyName)
    {
        var propertyChangedEventArgs = new PropertyChangedEventArgs(propertyName);
        PropertyChanged?.Invoke(this, propertyChangedEventArgs);
    }
}
