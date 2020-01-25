# Mapsui - TourPickr edition

I use this customized Mapsui library in [TourPickr Android app](https://tourpickr.com). It has a few performance improvements and a few new classes/interfaces that provide better customization.

Namely:
* CustomRenderedFeature class + IRendererFactory interface - use this to render custom shapes directly on Skia canvas
* IFeatureCustomHitTest interface - use this to perform custom hit tests (useful with CustomRenderedFeature)

Notes:
* Tested only on Android devices
* WPF rendering support removed
* There is no NuGet, clone this repo and pack your own
* Based on 1.4.* version of Mapsui

Original Read Me:

<p align="left"><img src="Docs/Images/logo/icon.png" alt="Mapsui" height="180px"></p>

| What  | Status  | 
| ------------- |:-------------:|
| Nuget   | [![NuGet Status](http://img.shields.io/nuget/v/Mapsui.svg?style=flat)](https://www.nuget.org/packages/Mapsui/) |
| Build on Windows    | [![Build status](https://ci.appveyor.com/api/projects/status/p20w43qv4ixkkftp?svg=true)](https://ci.appveyor.com/project/pauldendulk/mapsui) |
| Build on Mac | [![Build Status](https://www.bitrise.io/app/119dabd1302841a1/status.svg?token=KH9mbi7R6uLBz0iUZjbvJw&branch=master)](https://www.bitrise.io/app/119dabd1302841a1) |

## Mapsui (pronounced map-su-wii)

Mapsui is a C# map component for apps

- Supported platforms: WPF, UWP, Android, iOS
- The core PCL is Profile 111
- Designed to be fast and responsive (see [architecture](https://github.com/pauldendulk/Mapsui/wiki/Async-Fetching))
- Started as a fork of SharpMap

## News
- 2018 April 21: Moved the repository from pauldendulk/Mapsui to Mapsui/Mapsui. 

## Getting Started
[Here](https://github.com/pauldendulk/Mapsui/wiki/Getting-Started-with-Mapsui) is a getting started for WPF.

## Documentation
There is limited documentation, please take a look at the [wiki](https://github.com/pauldendulk/Mapsui/wiki). Let us know what information you are missing for your projects. 

## Samples
The best way to get going with Mapsui is by using the Samples. If you clone the project there is a Samples folder with a Mapsui.Samples.Wpf project with the list of samples. The accompanying code can be found [here](https://github.com/pauldendulk/Mapsui/tree/master/Samples/Mapsui.Samples.Common/Maps).

## Questions
If you have a question please submit an issue [here](https://github.com/pauldendulk/Mapsui/issues)

## Contributing
See the [guidelines](CONTRIBUTING.md)

## Thanks go to
- ReSharper for providing free open source licenses for Mapsui

## Supported Platforms

- **WPF** - Windows Desktop on .NET 4.5.2
- **UWP** - Windows Store on Windows 10 build 10586
- **Android** - Xamarin.Android on API Level 19 (v4.4 - Kit Kat)
- **iOS** - Xamarin.iOS

## License 

[LGPL](https://raw.githubusercontent.com/pauldendulk/Mapsui/master/LICENSE.md)
