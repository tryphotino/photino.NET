## <span>NEW POLL!!</span>
Hello Photino Community! We have a new poll question, regarding where and how you use Photino:

[PHOTINO USAGE POLL](https://github.com/tryphotino/photino.NET/discussions/172)


# Build native, cross-platform desktop apps

https://tryphotino.io

Photino is a lightweight open-source framework for building native, cross-platform desktop applications with Web UI technology.

Photino enables developers to use fast, natively compiled languages like C#, C++, Java and more. Use your favorite development frameworks like .NET 6, and build desktop apps with Web UI frameworks, like Blazor, React, Angular, Vue, etc.!

Photino uses the OSs built-in WebKit-based browser control for Windows, macOS and Linux.
Photino is the lightest cross-platform framework. Compared to Electron, a Photino app is up to 110 times smaller! And it uses far less system memory too!


## <span>Photino.</span>NET

This project represents the .NET 6 wrapper for the Photino.Native project, which makes it available for all operating systems (Windows, macOS, Linux).
This library is used for all the sample projects provided by Photino, which include Blazor, Vue.JS, Angular, React, or the basic HTML app: 
https://github.com/tryphotino/photino.Samples

If you made changes to the Photino.Native project, or added new features to it, you will likely need this repo to hook it all up and expose the new system calls to the .NET wrapper.
In all other cases, you can just grab the nuget package for your projects:
https://www.nuget.org/packages/Photino.NET

## How to build this repo

If you want to build this library itself, you will need:
 * Windows 10+, Mac 10.15+, or Linux (Tested with Ubuntu 18.04+)
 * Make sure the Photino.Native Nuget package is added and up to date.
