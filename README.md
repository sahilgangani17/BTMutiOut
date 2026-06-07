# BT MultiOut 🔊

**Stream your Windows system audio to multiple Bluetooth devices simultaneously.**

BT MultiOut is a lightweight Windows utility that captures your system audio and broadcasts it to 2–3 Bluetooth speakers or headphones at once—perfect for creating a multi-room or immersive audio experience for free.

---

## ✨ Key Features

- **Simultaneous Streaming**: Play audio to multiple Bluetooth devices at the same time.
- **System Tray Support**: Minimise to the tray to keep streaming in the background.
- **Low Latency**: Performance-optimised audio capture and broadcast.
- **Easy Setup**: Includes a bundled installer for the application and required drivers.

---

## 📂 Project Structure

This repository is divided into two main components:

- **[`BTMultiOut/`](./BTMultiOut)**: The core C#/.NET 8 application source code.
- **[`BTMultiOut-Installer/`](./BTMultiOut-Installer)**: Scripts and resources to build the NSIS installer.

---

## 🚀 Getting Started

### For Users
1. Download the latest `BTMultiOut-Setup.exe` from the Releases page (if available) or build it manually.
2. Run the installer. It will install the **VB-CABLE** virtual driver and the **BT MultiOut** app.
3. Restart your PC if prompted.
4. Open BT MultiOut, select your Bluetooth devices, and click **▶ Start Streaming**.

### For Developers
#### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [NSIS 3.x](https://nsis.sourceforge.io/Download) (for building the installer)
- [VB-CABLE Setup](https://vb-audio.com/Cable/) (to be placed in the installer folder)

#### Build the App
```powershell
cd BTMultiOut
dotnet build
```

#### Build the Installer
1. Place `VBCABLE_Setup_x64.exe` in the `BTMultiOut-Installer/` directory.
2. Run the build script:
```powershell
cd BTMultiOut-Installer
.\build-installer.bat
```

---

## 🛠 How it Works

```mermaid
graph TD
    A[Windows System Audio] --> B[WASAPI Loopback Capture]
    B --> C[BTMultiOut Engine]
    C --> D[Device 1 (BT Speaker)]
    C --> E[Device 2 (BT Headphones)]
    C --> F[Device 3 (etc.)]
```

NAudio's `WasapiLoopbackCapture` taps into the system audio stream. The engine then broadcasts the audio buffer to each selected `WasapiOut` device in parallel, handling format conversion automatically.

---

## 🤝 Dependencies & Credits

- [**NAudio**](https://github.com/naudio/NAudio): Audio processing library (MIT License).
- [**VB-CABLE**](https://vb-audio.com/Cable/): Virtual audio driver (Freeware/Donationware).
- [**NSIS**](https://nsis.sourceforge.io/): Professional open-source system to create Windows installers.

---

## 🛡 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details (if applicable).
*Note: VB-CABLE is redistributed per its own terms.*
