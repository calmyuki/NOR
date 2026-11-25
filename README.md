<div align="center">

<img src="assets/logo.png" width="128" height="128" alt="NŌR Logo" style="border-radius: 26px; box-shadow: 0 8px 24px rgba(0,0,0,0.2);" />

# NŌR

### Modern, Hardware-Accelerated Dynamic Live Wallpaper Engine

[![macOS](https://img.shields.io/badge/macOS-14.0%2B-000000?style=for-the-badge&logo=apple&logoColor=white)](macOS/)
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge&logo=windows&logoColor=white)](Win/)
[![Swift](https://img.shields.io/badge/Swift-5.9%2B-F05138?style=for-the-badge&logo=swift&logoColor=white)](macOS/)
[![.NET](https://img.shields.io/badge/.NET-8.0%20(WPF)-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](Win/)
[![License](https://img.shields.io/badge/License-MIT-34D399?style=for-the-badge)](LICENSE)
[![Zero Idle CPU](https://img.shields.io/badge/Power-Zero%20Idle%20CPU-06B6D4?style=for-the-badge)](#-intelligent-power--audio-management)

<p align="center">
  <a href="#-platforms">Platforms</a> •
  <a href="#-key-features">Key Features</a> •
  <a href="#-building-from-source">Building</a> •
  <a href="#-project-directory-structure">Structure</a> •
  <a href="#-license">License</a>
</p>

</div>

---

## 🌟 Overview

**NŌR** is a lightweight, high-performance dynamic live wallpaper engine designed to render fluid animated wallpapers directly behind desktop icons with zero desktop click obstruction, hardware acceleration, zero-drain fullscreen suspension, and context-aware audio ducking.

---

## 🖥️ Platforms

| Platform | Location | Language / Framework | Desktop Injection Architecture |
|---|---|---|---|
| **macOS** | [`macOS/`](macOS/) | Swift 5.9+, SwiftUI, AppKit, AVFoundation | Direct window server integration via `kCGDesktopWindowLevel` |
| **Windows** | [`Win/`](Win/) | C# (.NET 8), WPF, Win32 Interop | Win32 Explorer `WorkerW` injection behind `SHELLDLL_DefView` |

---

## ✨ Key Features

### 🖥️ Native Desktop Canvas Integration
- Renders directly behind your desktop icons (`kCGDesktopWindowLevel` on macOS, `WorkerW` on Windows) without blocking clicks, selection rectangles, or file dragging.
- Preserves native gestures, virtual desktops, and multi-monitor workflows.

### ⚡ Intelligent Power & Audio Management
- **Zero-Drain Full-Screen Suspension**: Actively monitors the foreground window and instantly freezes video decoding when full-screen apps, games, or presentations are active—reducing idle GPU and CPU usage to **0%**.
- **Context-Aware Audio Ducking**: Plays ambient wallpaper sound only when you are admiring your clean desktop. When any app window appears on the screen, wallpaper audio is automatically muted to prevent distraction during work, calls, or media playback.

### ✂️ Built-In Video Trimmer & Crop Studio
- **Precision Trimming**: Non-destructively trim intro/outro segments or select seamless loop points with real-time frame scrubbing.
- **Aspect Ratio Cropping**: Quickly crop videos to perfectly match your screen (16:9, 16:10 laptop/Retina, 21:9 Ultrawide, or custom).

### 🧭 Status Bar / System Tray Companion
- Discreet status bar icon (macOS menu bar / Windows notification area) allowing you to toggle playback, mute audio, switch wallpapers, launch at login/startup, and access preferences with a single click.

---

## 🛠️ Building from Source

### Building macOS App
```bash
cd macOS
open "Wallpaper engine.xcodeproj"
# Or via CLI:
xcodebuild -project "Wallpaper engine.xcodeproj" -scheme "Wallpaper engine" -configuration Release build
```

### Building Windows App (.exe)
```bash
cd Win
# Run one-click script:
build.bat
# Or using dotnet CLI:
dotnet publish NOR.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../release/win-x64
```
The compiled Windows executable will be generated at:
```
release/win-x64/NOR.exe
```

---

## 🗂️ Project Directory Structure

```text
NOR/
├── .github/
│   └── workflows/
│       └── build.yml               # GitHub Actions CI building macOS & Windows
├── assets/
│   └── logo.png                    # High-resolution application icon
├── macOS/                          # macOS Native Swift Application
│   ├── Wallpaper engine/           # Swift source files
│   ├── Wallpaper engine.xcodeproj/ # Xcode project
│   └── README.md                   # macOS specific documentation
├── Win/                            # Windows Native .NET 8 / WPF Application
│   ├── NOR.sln                     # Visual Studio Solution
│   ├── NOR.csproj                  # .NET 8 WPF Project
│   ├── App.xaml (.cs)              # Application entry & minimized tray mode
│   ├── MainWindow.xaml (.cs)       # Fluent Studio UI & controls
│   ├── Interop/                    # Win32 WorkerW hook & fullscreen detection
│   ├── Models/                     # GalleryItem & AppSettings models
│   ├── Services/                   # Wallpaper controller, monitor, & tray
│   ├── Views/                      # Trimmer & Crop dialogs
│   ├── Assets/                     # Windows app.ico, tray.ico, logo.png
│   ├── build.bat / build.ps1       # One-click Windows build tools
│   └── README.md                   # Windows specific documentation
├── release/                        # Compiled distribution binaries
├── CHANGELOG.md                    # Release history & version notes
├── CONTRIBUTING.md                 # Contribution guidelines
├── LICENSE                         # MIT License
└── README.md                       # Repository documentation
```

---

## 📄 License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for more information.
