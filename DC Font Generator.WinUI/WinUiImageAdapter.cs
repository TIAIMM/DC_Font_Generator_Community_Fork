using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using DC_Font_Generator;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DC_Font_Generator.WinUI;

public static class WinUiImageAdapter
{
    public static WriteableBitmap ToWriteableBitmap(Bgra32Image image)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));

        WriteableBitmap bitmap = new WriteableBitmap(image.Width, image.Height);
        byte[] premultiplied = PremultiplyAlpha(image.Pixels);
        using (Stream stream = bitmap.PixelBuffer.AsStream())
        {
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(premultiplied, 0, premultiplied.Length);
        }

        bitmap.Invalidate();
        return bitmap;
    }

    public static WriteableBitmap ToAtlasPreviewWriteableBitmap(Bgra32Image image)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));

        PreviewBackground background = ChooseTransparentPreviewBackground(image.Pixels);
        if (!background.Enabled)
        {
            return ToWriteableBitmap(image);
        }

        WriteableBitmap bitmap = new WriteableBitmap(image.Width, image.Height);
        byte[] composited = CompositeOverBackground(image.Pixels, background.Blue, background.Green, background.Red);
        using (Stream stream = bitmap.PixelBuffer.AsStream())
        {
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(composited, 0, composited.Length);
        }

        bitmap.Invalidate();
        return bitmap;
    }

    private static byte[] PremultiplyAlpha(byte[] source)
    {
        byte[] target = new byte[source.Length];
        for (int i = 0; i < source.Length; i += 4)
        {
            byte alpha = source[i + 3];
            if (alpha == 0)
            {
                continue;
            }

            if (alpha == 255)
            {
                target[i] = source[i];
                target[i + 1] = source[i + 1];
                target[i + 2] = source[i + 2];
                target[i + 3] = alpha;
                continue;
            }

            target[i] = (byte)((source[i] * alpha + 127) / 255);
            target[i + 1] = (byte)((source[i + 1] * alpha + 127) / 255);
            target[i + 2] = (byte)((source[i + 2] * alpha + 127) / 255);
            target[i + 3] = alpha;
        }

        return target;
    }

    private static byte[] CompositeOverBackground(byte[] source, byte blue, byte green, byte red)
    {
        byte[] target = new byte[source.Length];
        for (int i = 0; i < source.Length; i += 4)
        {
            byte alpha = source[i + 3];
            target[i] = (byte)((source[i] * alpha + blue * (255 - alpha) + 127) / 255);
            target[i + 1] = (byte)((source[i + 1] * alpha + green * (255 - alpha) + 127) / 255);
            target[i + 2] = (byte)((source[i + 2] * alpha + red * (255 - alpha) + 127) / 255);
            target[i + 3] = 255;
        }

        return target;
    }

    private static PreviewBackground ChooseTransparentPreviewBackground(byte[] pixels)
    {
        long weightedLuminance = 0;
        long weight = 0;
        int transparent = 0;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte alpha = pixels[i + 3];
            if (alpha < 250)
            {
                transparent++;
            }

            if (alpha <= 8)
            {
                continue;
            }

            int blue = pixels[i];
            int green = pixels[i + 1];
            int red = pixels[i + 2];
            int luminance = (red * 299 + green * 587 + blue * 114) / 1000;
            weightedLuminance += (long)luminance * alpha;
            weight += alpha;
        }

        if (transparent == 0 || weight == 0)
        {
            return default;
        }

        long average = weightedLuminance / weight;
        return average >= 128
            ? new PreviewBackground(true, 32, 32, 32)
            : new PreviewBackground(true, 255, 255, 255);
    }

    private readonly struct PreviewBackground
    {
        public PreviewBackground(bool enabled, byte blue, byte green, byte red)
        {
            Enabled = enabled;
            Blue = blue;
            Green = green;
            Red = red;
        }

        public bool Enabled { get; }
        public byte Blue { get; }
        public byte Green { get; }
        public byte Red { get; }
    }
}
