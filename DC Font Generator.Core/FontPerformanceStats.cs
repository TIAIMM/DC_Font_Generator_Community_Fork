using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DC_Font_Generator
{
    public sealed class FontPerformanceStats
    {
        private readonly List<FontPerformanceEntry> entries = new List<FontPerformanceEntry>();

        public IReadOnlyList<FontPerformanceEntry> Entries => entries;

        public void Add(string stage, TimeSpan elapsed)
        {
            if (string.IsNullOrEmpty(stage))
            {
                return;
            }

            entries.Add(new FontPerformanceEntry(stage, elapsed));
        }

        public string ToLogLine()
        {
            if (entries.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder("Performance: ");
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(entries[i].Stage);
                builder.Append('=');
                builder.Append(entries[i].Elapsed.TotalMilliseconds.ToString("0.##", CultureInfo.InvariantCulture));
                builder.Append("ms");
            }

            return builder.ToString();
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
