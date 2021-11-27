# Crayon SDL

A demo project of using Crayon with SDL2 (only applicable when exporting to C#)

## Build Instructions

This is designed to work **only** with the C#-exported project. To run this project, export the project to the C# target and then run the followup copy command from the Python script:

```
C:\Stuff\crayon-sdl> crayon SdlDemo.build -target csharp

C:\Stuff\crayon-sdl> python copy.py full
```

The first command exports the Crayon project to C# with all the defaults
The second command copies specific files from the `assets/csharp/` directory and merges/overwrites them. This includes all the requisite SDL DLL files. The C# project can be built and run like normal. 

# Documentation

This is not a complete implementation of SDL and only provides limited functionality to a few of the SDL functions. However, it can be easily updated to add more functionality.

## General Methods

The following methods are in the `Sdl` namespace

`initialize()` - This must always be called first.

`pollEvents()` - Returns a list of `SdlEvent` objects that have occurred since the previous invocation.

## SdlWindow

`constructor(title, width, height)` - create a new SDL Window.

`.show()` - displays the SDL window

`.fill(r, g, b)` - clears the window with a color.

`.present()` - must be called to flush all updates to the window. Call this once you're done drawing to the window for the frame.

`.surface` - Gets a reference to the window's drawing surface. Use this SdlSurface reference to make changes to the window.

## SdlSurface 

`loadResource(path)` - **Static function**. Loads a project image resource as an SDL surface.

`.blit(destinationSurface, x, y)` - Draws the image to the given surface. 

`.blitAdvanced(srcX, srcY, srcW, srcH, destinationSurface, dstX, dstY, dstW, dstH)` - draws a portion of the surface to a portion of another surface. Can be stretched/cropped.

`.convertToWindowFormat(window)` - Creates a new surface that has an internal pixel format that is identical to the window format.

## SdlEvent

`.type` - a value in the `SdlEventType` enum indicating what kind of event this is. The values in this enum are currently `QUIT`, `KEY_UP`, and `KEY_DOWN`. 

`.key` - for keyboard events, this denotes which key was pressed. This is a value in the `SdlKey` enum.

> For `SdlKey` enum values, see `source/SDL/SdlKey.cry`. There are quite a few.
