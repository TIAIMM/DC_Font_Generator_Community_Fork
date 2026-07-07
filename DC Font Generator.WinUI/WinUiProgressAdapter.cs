using System;
using DC_Font_Generator;
using Microsoft.UI.Dispatching;

namespace DC_Font_Generator.WinUI;

public sealed class WinUiFontProgress
{
    public string Stage { get; set; }
    public int Value { get; set; }
    public int Maximum { get; set; }
    public double Percent => Maximum <= 0 ? 0d : (double)Value / Maximum;
}

public sealed class WinUiProgressAdapter : IProgress<FontProgress>
{
    private readonly DispatcherQueue dispatcherQueue;
    private readonly Action<WinUiFontProgress> handler;
    private readonly TimeSpan minimumInterval;
    private DateTime lastReportUtc = DateTime.MinValue;

    public WinUiProgressAdapter(
        DispatcherQueue dispatcherQueue,
        Action<WinUiFontProgress> handler,
        TimeSpan? minimumInterval = null)
    {
        this.dispatcherQueue = dispatcherQueue;
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        this.minimumInterval = minimumInterval ?? TimeSpan.FromMilliseconds(50);
    }

    public void Report(FontProgress value)
    {
        if (value == null) return;

        DateTime now = DateTime.UtcNow;
        bool isComplete = value.Maximum > 0 && value.Value >= value.Maximum;
        if (!isComplete && minimumInterval > TimeSpan.Zero && now - lastReportUtc < minimumInterval)
        {
            return;
        }

        lastReportUtc = now;
        WinUiFontProgress progress = new WinUiFontProgress
        {
            Stage = value.Stage,
            Value = value.Value,
            Maximum = value.Maximum
        };

        if (dispatcherQueue == null || dispatcherQueue.HasThreadAccess)
        {
            handler(progress);
            return;
        }

        dispatcherQueue.TryEnqueue(() => handler(progress));
    }
}
