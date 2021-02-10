# FFMediaToolkit

[![Build status](https://ci.appveyor.com/api/projects/status/9vaaqchtx1d5nldj?svg=true)](https://ci.appveyor.com/project/radek-k/ffmediatoolkit) [![Nuget](https://img.shields.io/nuget/v/FFMediaToolkit.svg)](https://www.nuget.org/packages/FFMediaToolkit/)
[![License](https://img.shields.io/github/license/radek-k/FFMediaToolkit.svg)](https://github.com/radek-k/FFMediaToolkit/blob/master/LICENSE)

**FFMediaToolkit** is a .NET library for creating and reading multimedia files. It uses native FFmpeg libraries by the [FFmpeg.Autogen](https://github.com/Ruslan-B/FFmpeg.AutoGen) bindings.

## Features

- Decoding/encoding audio-video files in many formats supported by FFmpeg.
- Extracting audio data as floating point arrays.
- Access to any video frame by timestamp.
- Creating videos from images with metadata, pixel format, bitrate, CRF, FPS, GoP, dimensions and other codec settings.
- Supports reading multimedia chapters and metadata.
- Cross-platform - works on Linux, Windows, and macOS - with .NET Core or .NET Framework projects.

## Code samples

- Extract all video frames as PNG files
  
    ```c#
    int i = 0;
    var file = MediaFile.Open(@"C:\videos\movie.mp4");
    while(file.Video.TryGetNextFrame(out var imageData))
    {
        imageData.ToBitmap().Save($@"C:\images\frame_{i++}.png");
        // See the #Usage details for example .ToBitmap() implementation
        // The .Save() method may be different depending on your graphics library
    }
    ```

- Video decoding
  
    ```c#
    // Opens a multimedia file.
    // You can use the MediaOptions properties to set decoder options.
    var file = MediaFile.Open(@"C:\videos\movie.mp4");
    
     // Gets the frame at 5th second of the video.
    var frame5s = file.Video.GetFrame(TimeSpan.FromSeconds(5));
    
    // Print informations about the video stream.
    Console.WriteLine($"Bitrate: {file.Info.Bitrate / 1000.0} kb/s");
    var info = file.Video.Info;
    Console.WriteLine($"Duration: {info.Duration}");
    Console.WriteLine($"Frames count: {info.NumberOfFrames ?? "N/A"}");
    var frameRateInfo = info.IsVariableFrameRate ? "average" : "constant";
    Console.WriteLine($"Frame rate: {info.AvgFrameRate} fps ({frameRateInfo})");
    Console.WriteLine($"Frame size: {info.FrameSize}");
    Console.WriteLine($"Pixel format: {info.PixelFormat}");
    Console.WriteLine($"Codec: {info.CodecName}");
    Console.WriteLine($"Is interlaced: {info.IsInterlaced}");
    ```

- Encode video from images.
  
    ```c#
    // You can set there codec, bitrate, frame rate and many other options.
    var settings = new VideoEncoderSettings(width: 1920, height: 1080, framerate: 30, codec: VideoCodec.H264);
    settings.EncoderPreset = EncoderPreset.Fast;
    settings.CRF = 17;
    using(var file = MediaBuilder.CreateContainer(@"C:\videos\example.mp4").WithVideo(settings).Create())
    {
        while(file.Video.FramesCount < 300)
        {
            file.Video.AddFrame(/*Your code*/);
        }
    }
    ```

## Setup

Install the **FFMediaToolkit** package from [NuGet](https://www.nuget.org/packages/FFMediaToolkit/).

```shell
dotnet add package FFMediaToolkit
```
  
```Package
PM> Install-Package FFMediaToolkit
```

**FFmpeg libraries are not included in the package.** To use FFMediaToolkit, you need the **FFmpeg shared build** binaries: `avcodec`, `avformat`, `avutil`, `swresample`, `swscale`.

- **Windows** - You can download it from the [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases) or [gyan.dev](https://www.gyan.dev/ffmpeg/builds/). You only need `*.dll` files from the `.\bin` directory (**not `.\lib`**) of the ZIP package. Place the binaries in the `.\ffmpeg\x86_64\`(64bit) in the application output directory or set `FFmpegLoader.FFmpegPath`.
- **Linux** - Download FFmpeg using your package manager.
- **macOS** - Install FFmpeg via [Homebrew](https://formulae.brew.sh/formula/ffmpeg).

**You need to set `FFmpegLoader.FFmpegPath` with a full path to FFmpeg libraries.**
> If you want to use 64-bit FFmpeg, you have to disable the *Build* -> *Prefer 32-bit* option in Visual Studio project properties.

## Usage details

FFMediaToolkit uses the [*ref struct*](https://docs.microsoft.com/pl-pl/dotnet/csharp/language-reference/keywords/ref#ref-struct-types) `ImageData` for bitmap images. The `.Data` property contains pixels data in a [`Span<byte>`](https://docs.microsoft.com/pl-pl/dotnet/api/system.span-1?view=netstandard-2.1).

> **If you want to process or save the `ImageData`, you should convert it to another graphics object, using one of the following methods.**

> **These methods are not included in the program to avoid additional dependencies and provide compatibility with many graphics libraries.**

- **For [ImageSharp](https://github.com/SixLabors/ImageSharp) library (.NET Standard/Core - cross-platform):**
  
    ```c#
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    ...
    public static Image<Bgr24> ToBitmap(this ImageData imageData)
    {
        return Image.LoadPixelData<Bgr24>(imageData.Data, imageData.ImageSize.Width, imageData.ImageSize.Height);
    }
    ```

- **For .NET Framework `System.Drawing.Bitmap` (Windows only):**
  
    ```c#
    // ImageData -> Bitmap (unsafe)
    public static unsafe Bitmap ToBitmap(this ImageData bitmap)
    {
        fixed(byte* p = bitmap.Data)
        {
            return new Bitmap(bitmap.ImageSize.Width, bitmap.ImageSize.Height, bitmap.Stride, PixelFormat.Format24bppRgb, new IntPtr(p));
        }
    }
  
    // Bitmap -> ImageData (safe)
    public static ImageData ToImageData(this Bitmap bitmap)
    {
        var rect = new Rectangle(Point.Empty, bitmap.Size);
        var bitLock = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
  
        var bitmapData = ImageData.FromPointer(bitLock.Scan0, bitmap.Size, ImagePixelFormat.Bgr24);
        bitmap.UnlockBits(bitLock);
        return bitmapData;
    }
    ```

- **For .NET Framework/Core desktop apps with WPF UI. (Windows only):**
  
    ```c#
    using System.Windows.Media.Imaging;
    ...
    // ImageData -> BitmapSource (unsafe)
    public static unsafe BitmapSource ToBitmap(this ImageData bitmapData)
    {
        fixed(byte* ptr = bitmapData.Data)
        {
            return BitmapSource.Create(bitmapData.ImageSize.Width, bitmapData.ImageSize.Height, 96, 96, PixelFormats.Bgr32, null, new IntPtr(ptr), bitmapData.Data.Length, bitmapData.Stride);
        }
    }
  
    // BitmapSource -> ImageData (safe)
    public static ImageData ToImageData(this BitmapSource bitmap)
    {
        var wb = new WriteableBitmap(bitmap);
        return ImageData.FromPointer(wb.BackBuffer, ImagePixelFormat.Bgra32, wb.PixelWidth, wb.PixelHeight);
    }
    ```

- **FFMediaToolkit will also work with any other graphics library that supports creating images from `Span<byte>`, byte array or memory pointer**

## Visual Basic usage
Writing decoded bitmap directly to the WPF `WriteableBitmap` buffer using the `TryReadFrameToPointer` method:
````vb
Dim file As FileStream = New FileStream("path to the video file", FileMode.Open, FileAccess.Read)
Dim media As MediaFile = MediaFile.Load(file)
Dim bmp As WriteableBimap = New WriteableBitmap(media.Video.Info.FrameSize.Width, media.Video.Info.FrameSize.Height, 96, 96, PixelFormats.Bgr24, Nothing)
bmp.Lock()
Dim decoded As Boolean = media.Video.TryGetFrame(TimeSpan.FromMinutes(1), bmp.BackBuffer, bmp.BackBufferStride)
If decoded Then
    bmp.AddDirtyRect(New Int32Rect(0, 0, media.Video.Info.FrameSize.Width, media.Video.Info.FrameSize.Height))
End If
bmp.Unlock()
imageBox.Source = bmp
````
Converting `ImageData` to a byte array:
````vb
Dim data() As Byte = media.Video.GetNextFrame().Data.ToArray()
````
## Licensing

This project is licensed under the [MIT](https://github.com/radek-k/FFMediaToolkit/blob/master/LICENSE) license.
