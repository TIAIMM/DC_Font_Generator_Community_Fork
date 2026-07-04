using System;
using System.Drawing;
using System.Runtime.InteropServices;
using SkiaSharp;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace DC_Font_Generator
{
    internal enum FontRenderBackend
    {
        Auto,
        Cpu,
        Direct3D12
    }

    internal interface IGlyphRenderSurfaceFactory : IDisposable
    {
        FontRenderBackend Backend { get; }
        IGlyphRenderSurface CreateSurface(int width, int height);
    }

    internal interface IGlyphRenderSurface : IDisposable
    {
        SKCanvas Canvas { get; }
        void Flush();
        byte[] ReadPixels();
    }

    internal sealed class GlyphRenderContext : IDisposable
    {
        private readonly IGlyphRenderSurfaceFactory factory;
        private IGlyphRenderSurface surface;
        private int width;
        private int height;

        public GlyphRenderContext(IGlyphRenderSurfaceFactory factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public FontRenderBackend Backend => factory.Backend;

        public SKCanvas PrepareCanvas(int width, int height, Color background)
        {
            if (surface == null || this.width != width || this.height != height)
            {
                surface?.Dispose();
                surface = factory.CreateSurface(width, height);
                this.width = width;
                this.height = height;
            }

            surface.Canvas.Clear(SkiaBitmapInterop.ToSKColor(background));
            return surface.Canvas;
        }

        public byte[] ReadPixels()
        {
            surface.Flush();
            return surface.ReadPixels();
        }

        public void Dispose()
        {
            surface?.Dispose();
            factory.Dispose();
        }
    }

    internal static class FontRenderBackendSelector
    {
        public static FontRenderBackend ReadRequestedBackend()
        {
            string value = Environment.GetEnvironmentVariable("DCFGCF_RENDER_BACKEND");
            if (string.Equals(value, "cpu", StringComparison.OrdinalIgnoreCase))
                return FontRenderBackend.Cpu;
            if (string.Equals(value, "d3d12", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "direct3d12", StringComparison.OrdinalIgnoreCase))
                return FontRenderBackend.Direct3D12;
            return FontRenderBackend.Auto;
        }

        public static IGlyphRenderSurfaceFactory CreateFactory(FontRenderBackend backend)
        {
            if (backend == FontRenderBackend.Direct3D12)
            {
                return new D3D12SkiaRenderSurfaceFactory();
            }

            return new CpuSkiaRenderSurfaceFactory();
        }
    }

    internal sealed class CpuSkiaRenderSurfaceFactory : IGlyphRenderSurfaceFactory
    {
        public FontRenderBackend Backend => FontRenderBackend.Cpu;

        public IGlyphRenderSurface CreateSurface(int width, int height)
        {
            return new CpuGlyphRenderSurface(width, height);
        }

        public void Dispose()
        {
        }
    }

    internal sealed class CpuGlyphRenderSurface : IGlyphRenderSurface
    {
        private readonly SKSurface surface;
        private readonly int width;
        private readonly int height;
        private byte[] pixels;
        private GCHandle pixelsHandle;

        public CpuGlyphRenderSurface(int width, int height)
        {
            this.width = width;
            this.height = height;
            SKImageInfo imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            surface = SKSurface.Create(imageInfo);
            pixels = new byte[width * height * 4];
            pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        }

        public SKCanvas Canvas => surface.Canvas;

        public void Flush()
        {
            surface.Canvas.Flush();
        }

        public byte[] ReadPixels()
        {
            SKImageInfo readInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            if (!surface.ReadPixels(readInfo, pixelsHandle.AddrOfPinnedObject(), width * 4, 0, 0))
            {
                throw new InvalidOperationException("Unable to read CPU glyph surface pixels.");
            }

            return pixels;
        }

        public void Dispose()
        {
            if (pixelsHandle.IsAllocated)
            {
                pixelsHandle.Free();
            }

            surface.Dispose();
        }
    }

    internal sealed class D3D12SkiaRenderSurfaceFactory : IGlyphRenderSurfaceFactory
    {
        private readonly IDXGIFactory6 dxgiFactory;
        private readonly IDXGIAdapter1 adapter;
        private readonly ID3D12Device device;
        private readonly ID3D12CommandQueue queue;
        private readonly GRD3DBackendContext backendContext;
        private readonly GRContext grContext;

        public D3D12SkiaRenderSurfaceFactory()
        {
            dxgiFactory = DXGI.CreateDXGIFactory2<IDXGIFactory6>(false);
            adapter = SelectAdapter(dxgiFactory);
            device = D3D12.D3D12CreateDevice<ID3D12Device>(adapter, FeatureLevel.Level_11_0);
            queue = device.CreateCommandQueue<ID3D12CommandQueue>(
                CommandListType.Direct,
                CommandQueuePriority.Normal,
                CommandQueueFlags.None,
                0);

            backendContext = new GRD3DBackendContext
            {
                Adapter = adapter.NativePointer,
                Device = device.NativePointer,
                Queue = queue.NativePointer,
                ProtectedContext = false
            };

            grContext = GRContext.CreateDirect3D(backendContext);
            if (grContext == null)
            {
                throw new InvalidOperationException("Unable to create Skia Direct3D 12 context.");
            }
        }

        public FontRenderBackend Backend => FontRenderBackend.Direct3D12;

        public IGlyphRenderSurface CreateSurface(int width, int height)
        {
            return new D3D12GlyphRenderSurface(grContext, width, height);
        }

        public void Dispose()
        {
            grContext?.Dispose();
            backendContext?.Dispose();
            queue?.Dispose();
            device?.Dispose();
            adapter?.Dispose();
            dxgiFactory?.Dispose();
        }

        private static IDXGIAdapter1 SelectAdapter(IDXGIFactory6 factory)
        {
            for (uint i = 0; i < 16; i++)
            {
                try
                {
                    IDXGIAdapter1 candidate = factory.EnumAdapterByGpuPreference<IDXGIAdapter1>(
                        i,
                        GpuPreference.HighPerformance);
                    AdapterDescription1 description = candidate.Description1;
                    if ((description.Flags & AdapterFlags.Software) == 0)
                    {
                        return candidate;
                    }

                    candidate.Dispose();
                }
                catch
                {
                    break;
                }
            }

            return factory.EnumAdapterByGpuPreference<IDXGIAdapter1>(0, GpuPreference.Unspecified);
        }
    }

    internal sealed class D3D12GlyphRenderSurface : IGlyphRenderSurface
    {
        private readonly GRContext context;
        private readonly SKSurface surface;
        private readonly int width;
        private readonly int height;
        private byte[] pixels;
        private GCHandle pixelsHandle;

        public D3D12GlyphRenderSurface(GRContext context, int width, int height)
        {
            this.context = context;
            this.width = width;
            this.height = height;
            SKImageInfo imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            surface = SKSurface.Create(context, true, imageInfo);
            if (surface == null)
            {
                throw new InvalidOperationException("Unable to create Skia Direct3D 12 glyph surface.");
            }

            pixels = new byte[width * height * 4];
            pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        }

        public SKCanvas Canvas => surface.Canvas;

        public void Flush()
        {
            surface.Canvas.Flush();
            context.Flush();
            context.Submit(true);
        }

        public byte[] ReadPixels()
        {
            SKImageInfo readInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            if (!surface.ReadPixels(readInfo, pixelsHandle.AddrOfPinnedObject(), width * 4, 0, 0))
            {
                throw new InvalidOperationException("Unable to read Direct3D 12 glyph surface pixels.");
            }

            return pixels;
        }

        public void Dispose()
        {
            if (pixelsHandle.IsAllocated)
            {
                pixelsHandle.Free();
            }

            surface.Dispose();
        }
    }
}
