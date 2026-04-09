using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
#if NET8_0_OR_GREATER
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
#endif
#if NETFRAMEWORK
using System.Windows.Forms;
#endif

namespace DesktopManager;

/// <summary>
/// Provides methods for capturing screenshots of the desktop.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ScreenshotService {
    /// <summary>
    /// Captures a screenshot of the entire virtual screen.
    /// </summary>
    /// <returns>A <see cref="Bitmap"/> containing the screenshot.</returns>
    public static Bitmap CaptureScreen() {
        // Use the system-reported virtual screen bounds instead of calculating from individual monitors
        // This ensures we capture exactly what Windows considers the virtual screen
#if NETFRAMEWORK
        var bounds = SystemInformation.VirtualScreen;
#else
        var bounds = GetVirtualScreenBounds();
#endif
        
        return CaptureRegion(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Captures a screenshot of the specified monitor.
    /// </summary>
    /// <param name="index">Monitor index starting at 0.</param>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="deviceName">Monitor device name.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureMonitor(int? index = null, string? deviceId = null, string? deviceName = null) {
        Monitors monitors = new();
        var monitor = monitors.GetMonitors(index: index, deviceId: deviceId, deviceName: deviceName).FirstOrDefault();
        if (monitor == null) {
            string requested = !string.IsNullOrEmpty(deviceId)
                ? $"DeviceId '{deviceId}'"
                : !string.IsNullOrEmpty(deviceName)
                    ? $"DeviceName '{deviceName}'"
                    : "the specified criteria";
            throw new ArgumentException($"Monitor not found for {requested}");
        }

        return CaptureRegion(
            monitor.PositionLeft,
            monitor.PositionTop,
            monitor.PositionRight - monitor.PositionLeft,
            monitor.PositionBottom - monitor.PositionTop);
    }

    /// <summary>
    /// Captures a screenshot of an arbitrary region of the desktop.
    /// </summary>
    /// <param name="region">Rectangle describing region to capture.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureRegion(Rectangle region) {
        return CaptureRegion(region.Left, region.Top, region.Width, region.Height);
    }

    /// <summary>
    /// Captures a screenshot of an arbitrary region.
    /// </summary>
    /// <param name="left">Left coordinate.</param>
    /// <param name="top">Top coordinate.</param>
    /// <param name="width">Width of the region.</param>
    /// <param name="height">Height of the region.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureRegion(int left, int top, int width, int height) {
        if (width <= 0 || height <= 0) {
            throw new ArgumentException("Width and height must be greater than zero");
        }

        Rectangle bounds;
#if NETFRAMEWORK
        bounds = SystemInformation.VirtualScreen;
#else
        bounds = GetVirtualScreenBounds();
#endif
        // Check if the requested region is within the virtual screen bounds
        int requestedRight = left + width;
        int requestedBottom = top + height;
        int boundsRight = bounds.Left + bounds.Width;
        int boundsBottom = bounds.Top + bounds.Height;
        
        // First try to capture as-is if it's within bounds
        bool isWithinBounds = left >= bounds.Left && top >= bounds.Top && 
                             requestedRight <= boundsRight && requestedBottom <= boundsBottom;
        
        if (!isWithinBounds) {
            // For monitor capture, try to intersect with virtual screen bounds to handle coordinate system mismatches
            int adjustedLeft = Math.Max(left, bounds.Left);
            int adjustedTop = Math.Max(top, bounds.Top);
            int adjustedRight = Math.Min(requestedRight, boundsRight);
            int adjustedBottom = Math.Min(requestedBottom, boundsBottom);
            
            // If there's still a valid intersection, use it
            if (adjustedLeft < adjustedRight && adjustedTop < adjustedBottom) {
                left = adjustedLeft;
                top = adjustedTop;
                width = adjustedRight - adjustedLeft;
                height = adjustedBottom - adjustedTop;
            } else {
                throw new ArgumentOutOfRangeException(nameof(left), 
                    $"Region ({left}, {top}, {width}x{height}) is outside the bounds of the virtual screen ({bounds.Left}, {bounds.Top}, {bounds.Width}x{bounds.Height})");
            }
        }

        Bitmap bitmap = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(left, top, 0, 0, new Size(width, height));
        return bitmap;
    }

    /// <summary>
    /// Captures a screenshot of a window.
    /// </summary>
    /// <param name="hwnd">Window handle.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureWindow(IntPtr hwnd) {
        if (hwnd == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle", nameof(hwnd));
        }

        if (!MonitorNativeMethods.GetWindowRect(hwnd, out RECT rect)) {
            throw new InvalidOperationException("Failed to get window bounds");
        }

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) {
            throw new InvalidOperationException("The target window does not have a visible size.");
        }

        Bitmap? bitmap = TryPrintWindow(hwnd, width, height);
        if (bitmap != null) {
            if (LooksSuspiciouslyBlack(bitmap)) {
                Bitmap fallbackBitmap = CaptureRegion(rect.Left, rect.Top, width, height);
                if (!LooksSuspiciouslyBlack(fallbackBitmap)) {
                    bitmap.Dispose();
                    return fallbackBitmap;
                }

                fallbackBitmap.Dispose();
            }

            return bitmap;
        }

        return CaptureRegion(rect.Left, rect.Top, width, height);
    }

    /// <summary>
    /// Captures a screenshot of a window control.
    /// </summary>
    /// <param name="hwnd">Control handle.</param>
    /// <returns>Bitmap with the screenshot.</returns>
    public static Bitmap CaptureControl(IntPtr hwnd) {
        return CaptureWindow(hwnd);
    }

    /// <summary>
    /// Reads OCR text from a bitmap capture.
    /// </summary>
    /// <param name="bitmap">Bitmap to inspect.</param>
    /// <param name="languageTag">Optional language tag such as en-US.</param>
    /// <returns>Recognized OCR text and bounds.</returns>
    public static DesktopOcrReadResult ReadText(Bitmap bitmap, string? languageTag = null) {
        if (bitmap == null) {
            throw new ArgumentNullException(nameof(bitmap));
        }

#if NET8_0_OR_GREATER
        return ReadTextCoreAsync(bitmap, languageTag).GetAwaiter().GetResult();
#else
        throw new PlatformNotSupportedException("OCR text extraction requires the modern Windows DesktopManager targets.");
#endif
    }

#if !NETFRAMEWORK
    private static Rectangle GetVirtualScreenBounds() {
        int left = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_XVIRTUALSCREEN);
        int top = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_YVIRTUALSCREEN);
        int width = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CXVIRTUALSCREEN);
        int height = MonitorNativeMethods.GetSystemMetrics(MonitorNativeMethods.SM_CYVIRTUALSCREEN);
        return new Rectangle(left, top, width, height);
    }
#endif

    private static Bitmap? TryPrintWindow(IntPtr hwnd, int width, int height) {
        Bitmap bitmap = new Bitmap(width, height);
        using Graphics graphics = Graphics.FromImage(bitmap);

        IntPtr hdc = graphics.GetHdc();
        try {
            if (MonitorNativeMethods.PrintWindow(hwnd, hdc, MonitorNativeMethods.PW_RENDERFULLCONTENT) ||
                MonitorNativeMethods.PrintWindow(hwnd, hdc, 0)) {
                return bitmap;
            }
        } finally {
            graphics.ReleaseHdc(hdc);
        }

        bitmap.Dispose();
        return null;
    }

#if NET8_0_OR_GREATER
#pragma warning disable CA1416
    private static async Task<DesktopOcrReadResult> ReadTextCoreAsync(Bitmap bitmap, string? languageTag) {
        OcrEngine engine = CreateOcrEngine(languageTag);
        using MemoryStream memory = new();
        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;

        using InMemoryRandomAccessStream randomAccess = new();
        Stream output = randomAccess.AsStreamForWrite();
        memory.CopyTo(output);
        output.Flush();

        randomAccess.Seek(0);
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccess).AsTask().ConfigureAwait(false);
        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied).AsTask().ConfigureAwait(false);
        OcrResult result = await engine.RecognizeAsync(softwareBitmap).AsTask().ConfigureAwait(false);

        DesktopOcrLine[] lines = result.Lines
            .Select(CreateOcrLine)
            .ToArray();

        return new DesktopOcrReadResult {
            LanguageTag = engine.RecognizerLanguage?.LanguageTag ?? NormalizeLanguageTag(languageTag) ?? string.Empty,
            Text = NormalizeRecognizedText(result.Text),
            Lines = lines
        };
    }

    private static OcrEngine CreateOcrEngine(string? languageTag) {
        string? normalizedLanguageTag = NormalizeLanguageTag(languageTag);
        OcrEngine? engine = null;
        if (!string.IsNullOrWhiteSpace(normalizedLanguageTag)) {
            try {
                engine = OcrEngine.TryCreateFromLanguage(new Language(normalizedLanguageTag));
            } catch (Exception ex) {
                throw new InvalidOperationException($"The OCR language tag '{normalizedLanguageTag}' is not valid.", ex);
            }

            if (engine == null) {
                throw new InvalidOperationException($"Windows OCR does not support the requested language '{normalizedLanguageTag}'.");
            }

            return engine;
        }

        engine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine != null) {
            return engine;
        }

        engine = OcrEngine.TryCreateFromLanguage(new Language("en-US"));
        return engine ?? throw new InvalidOperationException("Windows OCR is unavailable on this machine.");
    }

    private static DesktopOcrLine CreateOcrLine(OcrLine line) {
        DesktopOcrWord[] words = line.Words
            .Select(CreateOcrWord)
            .ToArray();
        Rectangle bounds = GetCombinedBounds(words);
        return new DesktopOcrLine {
            Text = NormalizeRecognizedText(line.Text),
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            Words = words
        };
    }

    private static DesktopOcrWord CreateOcrWord(OcrWord word) {
        Rectangle bounds = NormalizeBounds(word.BoundingRect);
        return new DesktopOcrWord {
            Text = NormalizeRecognizedText(word.Text),
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height
        };
    }

    private static Rectangle GetCombinedBounds(IReadOnlyList<DesktopOcrWord> words) {
        if (words.Count == 0) {
            return Rectangle.Empty;
        }

        int left = words.Min(word => word.X);
        int top = words.Min(word => word.Y);
        int right = words.Max(word => word.X + word.Width);
        int bottom = words.Max(word => word.Y + word.Height);
        return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    private static Rectangle NormalizeBounds(Windows.Foundation.Rect bounds) {
        int left = Math.Max(0, (int)Math.Floor(bounds.X));
        int top = Math.Max(0, (int)Math.Floor(bounds.Y));
        int right = Math.Max(left + 1, (int)Math.Ceiling(bounds.X + bounds.Width));
        int bottom = Math.Max(top + 1, (int)Math.Ceiling(bounds.Y + bounds.Height));
        return new Rectangle(left, top, right - left, bottom - top);
    }

    private static string NormalizeRecognizedText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }

        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }

    private static string? NormalizeLanguageTag(string? languageTag) {
        return string.IsNullOrWhiteSpace(languageTag) ? null : languageTag.Trim();
    }
#pragma warning restore CA1416
#endif

    internal static bool LooksSuspiciouslyBlack(Bitmap bitmap) {
        if (bitmap == null) {
            throw new ArgumentNullException(nameof(bitmap));
        }

        if (bitmap.Width <= 0 || bitmap.Height <= 0) {
            return true;
        }

        int horizontalStep = Math.Max(1, bitmap.Width / 64);
        int verticalStep = Math.Max(1, bitmap.Height / 64);
        int sampleCount = 0;
        int darkSampleCount = 0;
        long totalLuminance = 0;

        for (int y = 0; y < bitmap.Height; y += verticalStep) {
            for (int x = 0; x < bitmap.Width; x += horizontalStep) {
                Color pixel = bitmap.GetPixel(x, y);
                int luminance = (pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;
                totalLuminance += luminance;
                if (luminance <= 12) {
                    darkSampleCount++;
                }

                sampleCount++;
            }
        }

        if (sampleCount == 0) {
            return true;
        }

        double averageLuminance = totalLuminance / (double)sampleCount;
        double darkSampleRatio = darkSampleCount / (double)sampleCount;
        return darkSampleRatio >= 0.98 && averageLuminance <= 12.0;
    }

    internal static DesktopVisualDifferenceMetrics CompareBitmaps(Bitmap baseline, Bitmap current, int differenceThreshold = 24, int maxSampleColumns = 64, int maxSampleRows = 64) {
        if (baseline == null) {
            throw new ArgumentNullException(nameof(baseline));
        }

        if (current == null) {
            throw new ArgumentNullException(nameof(current));
        }

        if (differenceThreshold < 0 || differenceThreshold > 255) {
            throw new ArgumentOutOfRangeException(nameof(differenceThreshold), "Difference threshold must be between 0 and 255.");
        }

        if (maxSampleColumns <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxSampleColumns), "The maximum sampled column count must be greater than zero.");
        }

        if (maxSampleRows <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxSampleRows), "The maximum sampled row count must be greater than zero.");
        }

        if (baseline.Width <= 0 || baseline.Height <= 0 || current.Width <= 0 || current.Height <= 0) {
            return new DesktopVisualDifferenceMetrics {
                SampleCount = 1,
                ChangedSampleCount = 1,
                ChangedSampleRatio = 1.0,
                AverageDifference = 255.0,
                DifferenceThreshold = differenceThreshold,
                SizeChanged = true
            };
        }

        bool sizeChanged = baseline.Width != current.Width || baseline.Height != current.Height;
        if (sizeChanged) {
            return new DesktopVisualDifferenceMetrics {
                SampleCount = 1,
                ChangedSampleCount = 1,
                ChangedSampleRatio = 1.0,
                AverageDifference = 255.0,
                DifferenceThreshold = differenceThreshold,
                SizeChanged = true
            };
        }

        int sampleColumns = Math.Min(maxSampleColumns, baseline.Width);
        int sampleRows = Math.Min(maxSampleRows, baseline.Height);
        int sampleCount = 0;
        int changedSampleCount = 0;
        long totalDifference = 0;

        for (int row = 0; row < sampleRows; row++) {
            int y = ResolveSampleCoordinate(row, sampleRows, baseline.Height);
            for (int column = 0; column < sampleColumns; column++) {
                int x = ResolveSampleCoordinate(column, sampleColumns, baseline.Width);
                Color beforePixel = baseline.GetPixel(x, y);
                Color afterPixel = current.GetPixel(x, y);
                int difference = (
                    Math.Abs(beforePixel.R - afterPixel.R)
                    + Math.Abs(beforePixel.G - afterPixel.G)
                    + Math.Abs(beforePixel.B - afterPixel.B)) / 3;
                totalDifference += difference;
                if (difference >= differenceThreshold) {
                    changedSampleCount++;
                }

                sampleCount++;
            }
        }

        if (sampleCount == 0) {
            return new DesktopVisualDifferenceMetrics {
                SampleCount = 1,
                ChangedSampleCount = 0,
                ChangedSampleRatio = 0,
                AverageDifference = 0,
                DifferenceThreshold = differenceThreshold,
                SizeChanged = false
            };
        }

        return new DesktopVisualDifferenceMetrics {
            SampleCount = sampleCount,
            ChangedSampleCount = changedSampleCount,
            ChangedSampleRatio = changedSampleCount / (double)sampleCount,
            AverageDifference = totalDifference / (double)sampleCount,
            DifferenceThreshold = differenceThreshold,
            SizeChanged = false
        };
    }

    internal static DesktopVisualBitmapMatch FindBestBitmapMatch(Bitmap template, Bitmap searchSpace, int differenceThreshold = 24, int scanStep = 8, int maxSampleColumns = 12, int maxSampleRows = 12) {
        if (template == null) {
            throw new ArgumentNullException(nameof(template));
        }

        if (searchSpace == null) {
            throw new ArgumentNullException(nameof(searchSpace));
        }

        if (differenceThreshold < 0 || differenceThreshold > 255) {
            throw new ArgumentOutOfRangeException(nameof(differenceThreshold), "Difference threshold must be between 0 and 255.");
        }

        if (scanStep <= 0) {
            throw new ArgumentOutOfRangeException(nameof(scanStep), "Scan step must be greater than zero.");
        }

        if (maxSampleColumns <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxSampleColumns), "The maximum sampled column count must be greater than zero.");
        }

        if (maxSampleRows <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxSampleRows), "The maximum sampled row count must be greater than zero.");
        }

        if (template.Width <= 0 || template.Height <= 0 || searchSpace.Width <= 0 || searchSpace.Height <= 0 || template.Width > searchSpace.Width || template.Height > searchSpace.Height) {
            return new DesktopVisualBitmapMatch {
                RelativeX = 0,
                RelativeY = 0,
                Width = template.Width,
                Height = template.Height,
                ScanStep = scanStep,
                EvaluatedPositionCount = 0,
                Metrics = new DesktopVisualDifferenceMetrics {
                    SampleCount = 1,
                    ChangedSampleCount = 1,
                    ChangedSampleRatio = 1.0,
                    AverageDifference = 255.0,
                    DifferenceThreshold = differenceThreshold,
                    SizeChanged = true
                }
            };
        }

        int maxX = searchSpace.Width - template.Width;
        int maxY = searchSpace.Height - template.Height;
        int sampleColumns = Math.Min(maxSampleColumns, template.Width);
        int sampleRows = Math.Min(maxSampleRows, template.Height);
        DesktopVisualBitmapMatch? bestMatch = null;
        int coarseEvaluatedCount = 0;

        foreach (int y in EnumerateCandidateOffsets(maxY, scanStep)) {
            foreach (int x in EnumerateCandidateOffsets(maxX, scanStep)) {
                coarseEvaluatedCount++;
                DesktopVisualDifferenceMetrics metrics = CompareTemplateAtOffset(template, searchSpace, x, y, differenceThreshold, sampleColumns, sampleRows);
                if (bestMatch == null || IsBetterMatch(metrics, bestMatch.Metrics)) {
                    bestMatch = new DesktopVisualBitmapMatch {
                        RelativeX = x,
                        RelativeY = y,
                        Width = template.Width,
                        Height = template.Height,
                        ScanStep = scanStep,
                        EvaluatedPositionCount = coarseEvaluatedCount,
                        Metrics = metrics
                    };
                }
            }
        }

        if (bestMatch == null) {
            throw new InvalidOperationException("At least one bitmap-match candidate should have been evaluated.");
        }

        int refinedCount = 0;
        int refineStartX = Math.Max(0, bestMatch.RelativeX - scanStep + 1);
        int refineEndX = Math.Min(maxX, bestMatch.RelativeX + scanStep - 1);
        int refineStartY = Math.Max(0, bestMatch.RelativeY - scanStep + 1);
        int refineEndY = Math.Min(maxY, bestMatch.RelativeY + scanStep - 1);

        for (int y = refineStartY; y <= refineEndY; y++) {
            for (int x = refineStartX; x <= refineEndX; x++) {
                refinedCount++;
                DesktopVisualDifferenceMetrics metrics = CompareTemplateAtOffset(template, searchSpace, x, y, differenceThreshold, sampleColumns, sampleRows);
                if (IsBetterMatch(metrics, bestMatch.Metrics)) {
                    bestMatch = new DesktopVisualBitmapMatch {
                        RelativeX = x,
                        RelativeY = y,
                        Width = template.Width,
                        Height = template.Height,
                        ScanStep = scanStep,
                        EvaluatedPositionCount = coarseEvaluatedCount + refinedCount,
                        Metrics = metrics
                    };
                }
            }
        }

        bestMatch.EvaluatedPositionCount = coarseEvaluatedCount + refinedCount;
        return bestMatch;
    }

    private static IEnumerable<int> EnumerateCandidateOffsets(int maxOffset, int step) {
        if (maxOffset <= 0) {
            yield return 0;
            yield break;
        }

        int current = 0;
        while (current <= maxOffset) {
            yield return current;
            current += step;
        }

        if ((maxOffset % step) != 0) {
            yield return maxOffset;
        }
    }

    private static DesktopVisualDifferenceMetrics CompareTemplateAtOffset(Bitmap template, Bitmap searchSpace, int offsetX, int offsetY, int differenceThreshold, int sampleColumns, int sampleRows) {
        int sampleCount = 0;
        int changedSampleCount = 0;
        long totalDifference = 0;

        for (int row = 0; row < sampleRows; row++) {
            int y = ResolveSampleCoordinate(row, sampleRows, template.Height);
            for (int column = 0; column < sampleColumns; column++) {
                int x = ResolveSampleCoordinate(column, sampleColumns, template.Width);
                Color templatePixel = template.GetPixel(x, y);
                Color searchPixel = searchSpace.GetPixel(offsetX + x, offsetY + y);
                int difference = (
                    Math.Abs(templatePixel.R - searchPixel.R)
                    + Math.Abs(templatePixel.G - searchPixel.G)
                    + Math.Abs(templatePixel.B - searchPixel.B)) / 3;
                totalDifference += difference;
                if (difference >= differenceThreshold) {
                    changedSampleCount++;
                }

                sampleCount++;
            }
        }

        if (sampleCount == 0) {
            return new DesktopVisualDifferenceMetrics {
                SampleCount = 1,
                ChangedSampleCount = 1,
                ChangedSampleRatio = 1.0,
                AverageDifference = 255.0,
                DifferenceThreshold = differenceThreshold,
                SizeChanged = true
            };
        }

        return new DesktopVisualDifferenceMetrics {
            SampleCount = sampleCount,
            ChangedSampleCount = changedSampleCount,
            ChangedSampleRatio = changedSampleCount / (double)sampleCount,
            AverageDifference = totalDifference / (double)sampleCount,
            DifferenceThreshold = differenceThreshold,
            SizeChanged = false
        };
    }

    private static bool IsBetterMatch(DesktopVisualDifferenceMetrics candidate, DesktopVisualDifferenceMetrics currentBest) {
        if (candidate.AverageDifference < currentBest.AverageDifference) {
            return true;
        }

        if (candidate.AverageDifference > currentBest.AverageDifference) {
            return false;
        }

        return candidate.ChangedSampleRatio < currentBest.ChangedSampleRatio;
    }

    private static int ResolveSampleCoordinate(int index, int sampleCount, int dimension) {
        if (sampleCount <= 1 || dimension <= 1) {
            return 0;
        }

        return (int)Math.Round(index * (dimension - 1d) / (sampleCount - 1d));
    }
}
