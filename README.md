# ClencMP
Multi-platform app to cut and encode videos to a heavily-efficient video codec (VP9). Such encoded video is great in terms of quality/size. Perfect to upload to a video hosting websites, or social media platforms liek Discord.
The target of that project is to make a easy-to-use utility to encode videos, that will run on Windows, as well as on Ubuntu => Hence the name: Clenc-Multiplatform!
Therefore it is created using C# .NET 7.0 and uses FFMPEG as the backend.

## Screenshots
![image](https://github.com/dpho3n1x/ClencMP/assets/57898662/f268277d-629d-4ff7-94d3-8143ca8e1035)
![image](https://github.com/dpho3n1x/ClencMP/assets/57898662/6f275c37-577c-4d88-a137-32c54425084f)

## Supported systems
- Binary is provided for Ubuntu, and Windows in the GitHub releases. (Windows binary will be uploaded soon).
- Binaries should work on Windows 7 or newer (64bit), and any Linux distro supporting dotnet-runtime-7.0 package
- Software has been tested on Windows 11 and Ubuntu 22.04.
- If you want to run it on ARM based devices or on MacOS, you can compile it using `dotnet publish -c RELEASE` or in Visual Studio
