using System;

namespace DC_Font_Generator
{
    internal static class GameFontMetricQuantizer
    {
        public static int ToGameInt(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return 0;
            }

            return (int)value;
        }

        public static int ToNearestGameInt(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return 0;
            }

            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        public static int SelectAdvance(float desiredAdvance, int minimumAdvance, bool isDoubleByte)
        {
            int advance = isDoubleByte
                ? ToNearestGameInt(desiredAdvance)
                : ToGameInt(desiredAdvance);

            if (advance < minimumAdvance)
            {
                advance = minimumAdvance;
            }

            return Math.Max(1, advance);
        }

        public static float SpacingForAdvance(float width, int targetAdvance)
        {
            return targetAdvance - width;
        }
    }
}
