using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DC_Font_Generator
{
    public sealed class FontPerformanceStats
    {
        private readonly object syncRoot = new object();
        private readonly List<FontPerformanceEntry> entries = new List<FontPerformanceEntry>();
        private readonly List<string> debugEntries = new List<string>();
        private const int MaxDebugEntries = 500;

        public IReadOnlyList<FontPerformanceEntry> Entries => entries;
        public IReadOnlyList<string> DebugEntries => debugEntries;

        public void Add(string stage, TimeSpan elapsed)
        {
            if (string.IsNullOrEmpty(stage))
            {
                return;
            }

            lock (syncRoot)
            {
                entries.Add(new FontPerformanceEntry(stage, elapsed));
            }
        }

        public void AddDebug(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            lock (syncRoot)
            {
                if (debugEntries.Count < MaxDebugEntries)
                {
                    debugEntries.Add(message);
                }
                else if (debugEntries.Count == MaxDebugEntries)
                {
                    debugEntries.Add("[font-debug] further entries truncated");
                }
            }
        }

        public void AddDebugRange(IEnumerable<string> messages)
        {
            if (messages == null)
            {
                return;
            }

            foreach (string message in messages)
            {
                AddDebug(message);
            }
        }

        public string ToLogLine()
        {
            List<FontPerformanceEntry> performanceSnapshot;
            List<string> debugSnapshot;
            lock (syncRoot)
            {
                performanceSnapshot = new List<FontPerformanceEntry>(entries);
                debugSnapshot = new List<string>(debugEntries);
            }

            StringBuilder builder = new StringBuilder();
            if (performanceSnapshot.Count > 0)
            {
                builder.Append("Performance: ");
                for (int i = 0; i < performanceSnapshot.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(performanceSnapshot[i].Stage);
                    builder.Append('=');
                    builder.Append(performanceSnapshot[i].Elapsed.TotalMilliseconds.ToString("0.##", CultureInfo.InvariantCulture));
                    builder.Append("ms");
                }
            }

            if (debugSnapshot.Count > 0)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }
                builder.AppendLine("Font debug:");
                for (int i = 0; i < debugSnapshot.Count; i++)
                {
                    builder.AppendLine(debugSnapshot[i]);
                }
            }

            return builder.ToString().TrimEnd();
        }
    }

    public sealed class FontPerformanceEntry
    {
        public FontPerformanceEntry(string stage, TimeSpan elapsed)
        {
            Stage = stage;
            Elapsed = elapsed;
        }

        public string Stage { get; }
        public TimeSpan Elapsed { get; }
    }
}
