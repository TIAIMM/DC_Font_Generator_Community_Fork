using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DC_Font_Generator
{
    internal static class FntBinaryCodec
    {
        public static void WriteRecords(BinaryWriter writer, IList<Fnt_char> chars, int startIndex, int endIndex)
        {
            if (endIndex <= startIndex)
            {
                return;
            }

            int recordCount = endIndex - startIndex;
            byte[] records = new byte[recordCount * Fnt_char.SerializedSize];
            for (int i = 0; i < recordCount; i++)
            {
                Span<byte> record = records.AsSpan(i * Fnt_char.SerializedSize, Fnt_char.SerializedSize);
                chars[startIndex + i].WriteTo(record);
            }

            writer.Write(records);
        }

        public static int ReadRecords(
            Stream input,
            BinaryReader reader,
            IList<string> template,
            int id,
            IList<Fnt_char> target,
            Hashtable charCode)
        {
            long remainingBytes = input.Length - input.Position;
            int availableRecords = (int)Math.Min(remainingBytes / Fnt_char.SerializedSize, template.Count);
            byte[] records = reader.ReadBytes(availableRecords * Fnt_char.SerializedSize);
            int recordCount = records.Length / Fnt_char.SerializedSize;

            for (int count = 0; count < recordCount; count++)
            {
                Fnt_char fc = new Fnt_char();
                ReadOnlySpan<byte> record = records.AsSpan(count * Fnt_char.SerializedSize, Fnt_char.SerializedSize);
                fc.ReadFrom(record);

                string hex = template[count].Substring(2, 4);
                fc.ID = id;
                fc.HEX = hex;
                target.Add(fc);
                charCode[hex] = target[count];
            }

            return recordCount;
        }
    }
}
