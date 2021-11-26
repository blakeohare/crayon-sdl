using System;
using System.Collections.Generic;
using System.Linq;

namespace Interpreter
{
    internal class CrayonSdlBridge
    {

        private static Dictionary<int, IntPtr> nativePtrs = new Dictionary<int, IntPtr>();
        private static Dictionary<IntPtr, int> nativePtrsReverse = new Dictionary<IntPtr, int>();
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
                    return new DrawRect(int.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]), int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]), int.Parse(args[6]), int.Parse(args[7]), int.Parse(args[8]));

                case "sdl-render-present":
                    return new RenderPresent(int.Parse(args[0]));

                default: return null;
            }
        }

        private class RenderPresent : AbstractSdlAction
        {
            private IntPtr renderer;

            public RenderPresent(int rendererId)
            {
                this.renderer = nativePtrs[rendererId];
            }

            public override void Run()
            {
                SDL2.SDL.SDL_RenderPresent(this.renderer);
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
            private IntPtr renderer;

            public DrawRect(int rendererId, int x, int y, int width, int height, int red, int green, int blue, int alpha)
            {
                this.renderer = nativePtrs[rendererId];
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
                SDL2.SDL.SDL_SetRenderDrawColor(this.renderer, this.red, this.green, this.blue, this.alpha);
                SDL2.SDL.SDL_Rect rect = new SDL2.SDL.SDL_Rect() { x = this.x, y = this.y, w = this.width, h = this.height };
                SDL2.SDL.SDL_RenderDrawRect(this.renderer, ref rect);
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

                IntPtr windowRenderer = SDL2.SDL.SDL_CreateRenderer(window, -1, SDL2.SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

                SDL2.SDL.SDL_SetRenderDrawColor(windowRenderer, 0, 0, 0, 255);

                this.MarkAsCompleted(new string[] {
                    PtrToInt(window) + "",
                    PtrToInt(windowSurface) + "" ,
                    PtrToInt(windowRenderer) + "",
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
