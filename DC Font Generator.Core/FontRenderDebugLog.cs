using System;
using System.Collections.Generic;

namespace DC_Font_Generator
{
    internal static class FontRenderDebugLog
    {
        private const int MaxEntries = 500;
        private static readonly object SyncRoot = new object();
        private static readonly List<string> Entries = new List<string>();

        public static void Clear()
        {
            lock (SyncRoot)
            {
                Entries.Clear();
            }
        }

        public static void Add(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            lock (SyncRoot)
            {
                if (Entries.Count < MaxEntries)
                {
                    Entries.Add(message);
                }
                else if (Entries.Count == MaxEntries)
                {
                    Entries.Add("[font-debug] further entries truncated");
                }
            }
        }

        public static void AddException(string stage, Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            Add($"[font-debug] {stage}: {ex.GetType().Name}: {ex.Message}");
        }

        public static IReadOnlyList<string> Snapshot()
        {
            lock (SyncRoot)
            {
                return Entries.ToArray();
            }
        }
    }
}
