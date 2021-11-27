using System;
using System.Collections.Generic;
using System.Linq;

namespace Interpreter
{
    internal class CrayonSdlBridge
    {
        private static Dictionary<int, IntPtr> nativePtrs = new Dictionary<int, IntPtr>();
        private static Dictionary<IntPtr, int> nativePtrsReverse = new Dictionary<IntPtr, int>();

        private static Dictionary<uint, IntPtr> pixelFormats = new Dictionary<uint, IntPtr>();
        private static Dictionary<uint, string> pixelFormatNames = new Dictionary<uint, string>();

        private static int nativePtrIdAlloc = 1;
        private static int PtrToInt(IntPtr ptr)
        {
            if (!nativePtrsReverse.ContainsKey(ptr))
            {
                int id = nativePtrIdAlloc++;
                nativePtrsReverse[ptr] = id;
                nativePtrs[id] = ptr;
            }
            return nativePtrsReverse[ptr];
        }

        private static IntPtr IntToPtr(int id)
        {
            if (!nativePtrs.ContainsKey(id)) return IntPtr.Zero;
            return nativePtrs[id];
        }

        private static void EnsurePixelFormatDataAvailable(uint formatId)
        {
            if (!pixelFormats.ContainsKey(formatId))
            {
                pixelFormats[formatId] = SDL2.SDL.SDL_AllocFormat(formatId);
                pixelFormatNames[formatId] = SDL2.SDL.SDL_GetPixelFormatName(formatId);
            }
        }

        internal static AbstractSdlAction CreateSdlAction(NativeTunnelMessageWrapper messageWrapper, string[] args)
        {
            switch (messageWrapper.Type)
            {
                case "sdl-init":
                    return new InitializeSdl();

                case "sdl-create-window":
                    return new SetVideoMode(string.Join(',', args.Skip(2)), int.Parse(args[0]), int.Parse(args[1]));

                case "sdl-poll-events":
                    return new PollEvent();

                case "sdl-draw-rect":
                    return new DrawRect(int.Parse(args[0]), args[1], int.Parse(args[2]), int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]), int.Parse(args[6]), int.Parse(args[7]), int.Parse(args[8]), int.Parse(args[9]));

                case "sdl-render-present":
                    return new RenderPresent(int.Parse(args[0]), int.Parse(args[1]));

                case "sdl-load-image":
                    return new LoadImage(System.Convert.FromBase64String(args[0]));

                case "sdl-surface-blit":
                    return new BlitSurface(int.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]), int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]), int.Parse(args[6]), int.Parse(args[7]), int.Parse(args[8]), int.Parse(args[9]));

                case "sdl-convert-surface-format":
                    return new ConvertSurfaceFormat(int.Parse(args[0]), int.Parse(args[1]), args[2]);

                default: return null;
            }
        }

        private class BlitSurface : AbstractSdlAction
        {
            private IntPtr srcSurface;
            private IntPtr dstSurface;
            private SDL2.SDL.SDL_Rect srcRect;
            private SDL2.SDL.SDL_Rect dstRect;
            private bool isScaled;

            public BlitSurface(int srcId, int srcX, int srcY, int srcWidth, int srcHeight, int dstId, int dstX, int dstY, int dstWidth, int dstHeight)
            {
                this.srcSurface = nativePtrs[srcId];
                this.dstSurface = nativePtrs[dstId];
                this.srcRect.x = srcX;
                this.srcRect.y = srcY;
                this.srcRect.w = srcWidth;
                this.srcRect.h = srcHeight;
                this.dstRect.x = dstX;
                this.dstRect.y = dstY;
                this.dstRect.w = dstWidth;
                this.dstRect.h = dstHeight;
                this.isScaled = srcWidth != dstWidth || srcHeight != dstHeight;
            }

            public override void Run()
            {
                int resultCode = this.isScaled
                    ? SDL2.SDL.SDL_BlitScaled(this.srcSurface, ref this.srcRect, this.dstSurface, ref this.dstRect)
                    : SDL2.SDL.SDL_BlitSurface(this.srcSurface, ref this.srcRect, this.dstSurface, ref this.dstRect);
                if (resultCode != 0)
                {
                    throw new Exception(); // TODO: pass error information back as a result argument
                }
                this.MarkAsCompleted();
            }
        }

        private class MarshalledByteArray : IDisposable
        {
            public IntPtr Data { get; private set; }

            public MarshalledByteArray(byte[] data)
            {
                int size = data.Length;
                this.Data = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
                System.Runtime.InteropServices.Marshal.Copy(data, 0, this.Data, size);
            }

            public void Dispose()
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(this.Data);
            }
        }

        private class LoadImage : AbstractSdlAction
        {
            private byte[] imageBytes;

            public LoadImage(byte[] imageBytes)
            {
                this.imageBytes = imageBytes;
            }

            public override void Run()
            {
                int size = this.imageBytes.Length;
                uint defaultFormat = SDL2.SDL.SDL_PIXELFORMAT_ARGB8888;

                EnsurePixelFormatDataAvailable(defaultFormat);

                IntPtr surfaceHandle;
                using (MarshalledByteArray imageData = new MarshalledByteArray(this.imageBytes))
                {
                    IntPtr imageDataMemStream = SDL2.SDL.SDL_RWFromMem(imageData.Data, size);
                    surfaceHandle = SDL2.SDL_image.IMG_Load_RW(imageDataMemStream, 0);
                }

                IntPtr convertedSurface = SDL2.SDL.SDL_ConvertSurfaceFormat(surfaceHandle, defaultFormat, 0);
                SDL2.SDL.SDL_FreeSurface(surfaceHandle);

                int surfaceId = PtrToInt(convertedSurface);
                this.MarkAsCompleted(new string[] { surfaceId + "", defaultFormat + "" });
            }
        }

        private class ConvertSurfaceFormat : AbstractSdlAction
        {
            private IntPtr surface;
            private IntPtr window;
            private uint format;

            public ConvertSurfaceFormat(int surfaceId, int windowId, string formatId)
            {
                // TODO: add support for passing in a manual pixel format.
                this.surface = nativePtrs[surfaceId];
                this.window = windowId == 0 ? IntPtr.Zero : nativePtrs[windowId];
                this.format = uint.Parse(formatId);
            }

            public override void Run()
            {
                uint pixelFormat = this.window == IntPtr.Zero
                    ? this.format
                    : SDL2.SDL.SDL_GetWindowPixelFormat(this.window);
                EnsurePixelFormatDataAvailable(pixelFormat);

                IntPtr newSurface = SDL2.SDL.SDL_ConvertSurfaceFormat(this.surface, pixelFormat, 0);

                SDL2.SDL.SDL_UnlockSurface(newSurface);
                int newSurfaceId = PtrToInt(newSurface);
                this.MarkAsCompleted(new string[] { newSurfaceId + "", pixelFormat + "" });
            }
        }

        private class RenderPresent : AbstractSdlAction
        {
            private IntPtr window;
            private IntPtr renderer;

            public RenderPresent(int windowId, int rendererId)
            {
                this.window = nativePtrs[windowId];
                this.renderer = nativePtrs[rendererId];
            }

            public override void Run()
            {
                int result = SDL2.SDL.SDL_UpdateWindowSurface(this.window);
                if (result != 0)
                {
                    throw new Exception(); // TODO: pass error information back as result args
                }
                this.MarkAsCompleted();
            }
        }

        private class DrawRect : AbstractSdlAction
        {
            private int x;
            private int y;
            private int width;
            private int height;
            private byte red;
            private byte green;
            private byte blue;
            private byte alpha;
            private IntPtr dstSurface;
            private uint pixelFormat;

            public DrawRect(int surfaceId, string pixelFormatId, int x, int y, int width, int height, int red, int green, int blue, int alpha)
            {
                this.dstSurface = nativePtrs[surfaceId];
                this.pixelFormat = uint.Parse(pixelFormatId);
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
                this.red = (byte)red;
                this.green = (byte)green;
                this.blue = (byte)blue;
                this.alpha = (byte)alpha;
            }

            public override void Run()
            {
                EnsurePixelFormatDataAvailable(this.pixelFormat);
                SDL2.SDL.SDL_Rect rect = new SDL2.SDL.SDL_Rect() { x = this.x, y = this.y, w = this.width, h = this.height };
                uint color = SDL2.SDL.SDL_MapRGBA(pixelFormats[this.pixelFormat], this.red, this.green, this.blue, this.alpha);
                SDL2.SDL.SDL_FillRect(this.dstSurface, ref rect, color);
                this.MarkAsCompleted();
            }
        }

        private class InitializeSdl : AbstractSdlAction
        {
            public override void Run()
            {
                SDL2.SDL.SDL_Init(SDL2.SDL.SDL_INIT_EVERYTHING);
                this.MarkAsCompleted();
            }
        }

        private class SetVideoMode : AbstractSdlAction
        {
            private string title;
            private int width;
            private int height;

            public SetVideoMode(string title, int width, int height) : base()
            {
                this.title = title;
                this.width = width;
                this.height = height;
            }

            public override void Run()
            {
                IntPtr window = SDL2.SDL.SDL_CreateWindow(
                    this.title,
                    SDL2.SDL.SDL_WINDOWPOS_UNDEFINED, SDL2.SDL.SDL_WINDOWPOS_UNDEFINED,
                    this.width, this.height,
                    SDL2.SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

                IntPtr windowSurface = SDL2.SDL.SDL_GetWindowSurface(window);
                uint pixelFormat = SDL2.SDL.SDL_GetWindowPixelFormat(window);
                EnsurePixelFormatDataAvailable(pixelFormat);

                IntPtr windowRenderer = SDL2.SDL.SDL_CreateRenderer(window, -1, SDL2.SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

                SDL2.SDL.SDL_SetRenderDrawColor(windowRenderer, 0, 0, 0, 255);

                this.MarkAsCompleted(new string[] {
                    PtrToInt(window) + "",
                    PtrToInt(windowSurface) + "" ,
                    PtrToInt(windowRenderer) + "",
                    pixelFormat + "",
                });
            }
        }

        private class PollEvent : AbstractSdlAction
        {
            public override void Run()
            {
                List<string> events = new List<string>();
                SDL2.SDL.SDL_Event ev;
                while (SDL2.SDL.SDL_PollEvent(out ev) != 0)
                {
                    switch (ev.type)
                    {
                        case SDL2.SDL.SDL_EventType.SDL_KEYDOWN:
                            events.Add("keydown");
                            events.Add((int)ev.key.keysym.sym + "");
                            break;

                        case SDL2.SDL.SDL_EventType.SDL_KEYUP:
                            events.Add("keyup");
                            events.Add((int)ev.key.keysym.sym + "");
                            break;

                        case SDL2.SDL.SDL_EventType.SDL_QUIT:
                            events.Add("quit");
                            events.Add("1");
                            break;
                    }
                }

                this.MarkAsCompleted(events);
            }
        }

    }
}
