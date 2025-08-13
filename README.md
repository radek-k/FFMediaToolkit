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

## Code samples

- Extract all video frames as PNG files
 
    ```c#
    // Open video file
    using var file = MediaFile.Open(@"D:\example\movie.mp4", new MediaOptions() { VideoPixelFormat = ImagePixelFormat.Rgba32 });
    
    // Get pixel buffer from SkiaSharp bitmap
    using var bitmap = new SKBitmap(file.Video.Info.FrameSize.Width, file.Video.Info.FrameSize.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
    var pixelBuffer = bitmap.GetPixelSpan();

    int i = 0;
    // Iterate over all frames in the video - decoded frame will be written to the buffer
    while (file.Video.TryGetNextFrame(pixelBuffer))
    {
        // Save image as PNG file
        using var fs = File.OpenWrite($@"D:\example\frame_{i++}.png");
        bitmap.Encode(fs, SKEncodedImageFormat.Png, 100);
    }
    ```
    >This example uses [SkiaSharp](https://github.com/mono/SkiaSharp) to save decoded frames. See [Usage details](#usage-details) section for examples with other graphics libraries.
  

- Video decoding
  
    ```c#
    // Open a multimedia file
    // You can use the MediaOptions properties to set decoder options
    var file = MediaFile.Open(@"C:\videos\movie.mp4");
    
    // Get the frame at 5th second of the video
    var frame = file.Video.GetFrame(TimeSpan.FromSeconds(5));
    // ...
    // Frame should be disposed when no longer needed
    frame.Dispose(); 
    
    // Print information about the video stream
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
    // You can set codec, bitrate, frame rate and many other options here
    var settings = new VideoEncoderSettings(width: 1920, height: 1080, framerate: 30, codec: VideoCodec.H264) {
        EncoderPreset = EncoderPreset.Fast,
        CRF = 17,
    };
    // Create output file
    using var file = MediaBuilder.CreateContainer(@"D:\example\video.mp4").WithVideo(settings).Create();
    for(int i = 0; i < 300; i++)
    {
        // Load image using SkiaSharp (other libraries are also supported if provide access to pixel buffer)
        using var bmp = SKBitmap.Decode($@"D:\example\frame_{i}.png");
        // Encode the video frame
        file.Video.AddFrame(new ImageData(bmp.GetPixelSpan(), ImagePixelFormat.Rgba32, bmp.Width, bmp.Height));
    }
    ```

## Setup

Install the **FFMediaToolkit** package from [NuGet](https://www.nuget.org/packages/FFMediaToolkit/).

```shell
dotnet add package FFMediaToolkit
```


**FFmpeg libraries are not included in the package.** To use FFMediaToolkit, you need the **FFmpeg shared build** binaries: `avcodec` (v61), `avformat` (v61), `avutil` (v59), `swresample` (v5), `swscale` (v8).

> Supported FFmpeg version: 7.x (shared build)

- **Windows** - You can download it from the [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases) or [gyan.dev](https://www.gyan.dev/ffmpeg/builds/). You only need `*.dll` files from the `.\bin` directory (**not `.\lib`**) of the ZIP package. Place the binaries in `.\runtimes\win-[x64\arm64]\native\` in the application output directory or set `FFmpegLoader.FFmpegPath`.
- **Linux** - Download FFmpeg using your package manager. Default path is `/usr/lib/*-linux-gnu`
- **macOS**, **iOS**, **Android** - Not supported.

**You need to set `FFmpegLoader.FFmpegPath` with a full path to FFmpeg libraries.**

In .NET Framework projects you have to disable the *Build* -> *Prefer 32-bit* option in Visual Studio project properties.

## Usage details

FFMediaToolkit supports decoding video frames into pixel buffers which can be `Span<byte>`, `byte[]` or unmanaged memory. You can specify target pixel format by setting the `MediaOptions.VideoPixelFormat` property. The default format is `Bgr24`.

If you want to process or save the decoded frame, you can pass it to another graphics library, as shown in the examples below.

- For **[SkiaSharp](https://github.com/mono/SkiaSharp) library:**
    - Video decoding
      ```c#
      using var file = MediaFile.Open(@"D:\example\video.mp4", new MediaOptions() {
          StreamsToLoad = MediaMode.Video, 
          VideoPixelFormat = ImagePixelFormat.Rgba32
      });
      using var bitmap = new SKBitmap(file.Video.Info.FrameSize.Width, file.Video.Info.FrameSize.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
      var buffer = bitmap.GetPixelSpan();
      
      while(file.Video.TryGetNextFrame(buffer)) {
          // do something
      }
      ```
  - Video encoding
    ```c#
    using var bmp = SKBitmap.Decode($@"D:\example\frame.png");
    mediaFile.Video.AddFrame(new ImageData(bmp.GetPixelSpan(), ImagePixelFormat.Rgba32, bmp.Width, bmp.Height));
    ```

- For **[ImageSharp](https://github.com/SixLabors/ImageSharp) library:**
  - Video decoding
    ```c#
    var buffer = new byte[file.Video.FrameByteCount];
    var bmp = Image.WrapMemory<Bgr24>(buffer, file.Video.Info.FrameSize.Width, file.Video.Info.FrameSize.Height);
    
    while(file.Video.TryGetNextFrame(buffer)) {
        // do something
    }
    ```

- **For GDI+ `System.Drawing.Bitmap` (Windows only):**
  - Video decoding
      ```c#
      // Create bitmap once
      var rect = new Rectangle(Point.Empty, file.Video.Info.FrameSize);
      var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
      // ...
      // Read next frame
      var bitLock = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
      file.Video.TryGetNextFrame(bitLock.Scan0, bitLock.Stride);
      bitmap.UnlockBits(bitLock);
      ```
  - Video encoding
    ```c#
    var rect = new Rectangle(Point.Empty, bitmap.Size);
    var bitLock = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
    
    var bitmapData = ImageData.FromPointer(bitLock.Scan0, ImagePixelFormat.Bgr24, bitmap.Size);
    mediaFile.Video.AddFrame(bitmapData); // Encode the frame
    
    bitmap.UnlockBits(bitLock); // UnlockBits() must be called after encoding the frame
    ```
  
- **For desktop apps with WPF UI (Windows only):**
  - Video decoding 
  
      ```c#
      using System.Windows.Media.Imaging;
      
    // Create bitmap once
    var bmp = new WriteableBitmap(media.Video.Info.FrameSize.Width, media.Video.Info.FrameSize.Height, 96, 96, PixelFormats.Bgr24, null);
    // ...
    // Read next frame
    bmp.Lock();
    var success = media.Video.TryGetNextFrame(bmp.BackBuffer, bmp.BackBufferStride);
    if(success) {
          bmp.AddDirtyRect(new Int32Rect(0, 0, media.Video.Info.FrameSize.Width, media.Video.Info.FrameSize.Height));
    }
    bmp.Unlock();
    ```
  - Video encoding
    ```c#
    var bitmapSource = new BitmapImage(new Uri(@"D:\example\image.png"));
    var wb = new WriteableBitmap(bitmap);
    mediaFile.Video.AddFrame(ImageData.FromPointer(wb.BackBuffer, ImagePixelFormat.Bgra32, wb.PixelWidth, wb.PixelHeight));
      ```

## Visual Basic usage
Writing decoded bitmap directly to the WPF `WriteableBitmap` buffer:
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
