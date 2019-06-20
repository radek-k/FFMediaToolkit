
# FFMediaToolkit

[![Build status](https://ci.appveyor.com/api/projects/status/9vaaqchtx1d5nldj?svg=true)](https://ci.appveyor.com/project/radek41/ffmediatoolkit) ![Nuget](https://img.shields.io/nuget/v/FFMediaToolkit.svg)
![License](https://img.shields.io/github/license/radek-k/FFMediaToolkit.svg)

FFMediaToolkit is a .NET Standard library for creating and reading video files. It uses native FFmpeg libraries by the [FFmpeg.Autogen](https://github.com/Ruslan-B/FFmpeg.AutoGen) bindings.

## Features

- **Decoding/encoding videos** in any format supported by FFmpeg.
- **Fast, accurate acces to any video frame** - useful in video editors.
- **Create videos from images** - with any .NET graphics library.
- **Configurable** - supports pixel format, bitrate, FPS, GoP, dimensions and custom codec flags settings.
- **Simple, object-oriented, easy-to-use API** with inline documentation.
- **Cross-platform** - works on **Linux**, **Windows** and **MacOS** - with **.NET Core** or .NET Framework.

## Code samples

- Video Decoding

    ````c#
    var file = MediaFile.Open(@"C:\videos\movie.mp4", new MediaOptions()); // Opens the multimedia file
    var frame = file.Video.ReadFrame(frameNumber: 102); // Gets a frame by its number.
    var file.Video.ReadFrame(TimeSpan.FromSeconds(5)) // Gets the frame at 5th second of the video.
    ````

- Video extraction

    ````c#
    // Extract all video frames as PNG files.
    var file = MediaFile.Open(@"C:\videos\movie.mp4", new MediaOptions());
    for (int i = 0; i < file.Video.Info.FrameCount; i++)
    {
        file.Video.ReadNextFrame().ToBitmapUnsafe().Save($"C:/videos/frame_{i}.png");
    }

    ````

## Setup

- Install the **FFMediaToolkit** package from from NuGet.

    ````shell
    dotnet add package FFMediaToolkit
    ````

    ````Package Manager Console
    PM> Install-Package FFMediaToolkit
    ````

- To use it, you need the **FFmpeg v4.1.3 shared** build binaries. You can download it from the [Zeranoe](https://ffmpeg.zeranoe.com/builds/) site.
    - **Windows** - Place the binaries (7 DLLs) in the `.\ffmpeg\x86\` (32 bit) or `.\ffmpeg\x86_64\`(for 64bit apps) in the application output directory.
    - **Linux** - FFmpeg is pre-installed on many desktop Linux. The default path is `/usr/lib/x86_64-linux-gnu/`
    - **MacOS** - You can install FFmpeg via MacPorts or download `.dylib` files from the [Zeranoe](https://ffmpeg.zeranoe.com/builds/) site. The default path is `/opt/local/lib/`.

    If you want to use other directory, you can specify a path to it by the  `MediaCore.FFmpegPath` property.

## Usage

FFMediaToolkit uses the lightweight *ref struct* `BitmapData` for bitmap images. It uses the `Span<byte>` for pixels data. It is stack-only type, so it can't be stored in a class field. `BitmapData` can be converted to any other graphics object that supports creation from `Span<byte>`, byte array or memory pointer.

## Example BitmapData conversion methods

- For `System.Drawing.Bitmap`:

    ````c#
    // BitmapData -> Bitmap (unsafe)
    public static unsafe Bitmap ToBitmapUnsafe(this BitmapData bitmap)
    {
        fixed(byte* = bitmap.Data)
        {
            return new Bitmap(bitmap.ImageSize.Width, bitmap.ImageSize.Height, bitmap.Stride, PixelFormat.Format24bppRgb, new IntPtr(bitmap.Data));
        }
    }

    // Bitmap -> BitmapData (safe)
    public static BitmapData ToBitmapData(this Bitmap bitmap)
    {
        var bitLock = bitmap.LockBits(
            new Rectangle(Point.Empty, bitmap.Size),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        var bitmapData = BitmapData.FromPointer(bitLock.Scan0, bitmap.Size, ImagePixelFormat.BGR24);
        bitmap.UnlockBits(bitLock);
        return bitmapData;
    }
    ````

- For WPF's `System.Windows.Media.Imaging.BitmapSource`:

    ````c#
    // BitmapData -> BitmapSource (unsafe)
    public static unsafe BitmapSource ToBitmap(this BitmapData bitmapData)
    {
        fixed(byte* ptr = bitmapData.Data)
        {
            return BitmapSource.Create(
                bitmapData.ImageSize.Width,
                bitmapData.ImageSize.Height,
                96, 96,
                PixelFormats.Bgr32,
                null,
                new IntPtr(ptr),
                bitmapData.Data.Length,
                bitmapData.Stride);
        }
    }

    // BitmapSource -> BitmapData (safe)
    public static BitmapData ToBitmapData(this BitmapSource bitmap)
    {
        var wb = new WriteableBitmap(bitmap);
        var size = new System.Drawing.Size(wb.PixelWidth, wb.PixelHeight);
        return BitmapData.FromPointer(wb.BackBuffer, size,ImagePixelFormat.BGRA32);
    }
    ````

## Licensing

This project is licensed under the [MIT](https://github.com/radek-k/FFMediaToolkit/blob/master/LICENSE) license.
