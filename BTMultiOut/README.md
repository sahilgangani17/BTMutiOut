# BT MultiOut 🔊

Play your Windows system audio on **2–3 Bluetooth devices simultaneously** — for free.

---

## Requirements

| Requirement | Details |
|---|---|
| OS | Windows 10 or 11 (64-bit) |
| .NET SDK | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Bluetooth | Devices must be **already paired & connected** in Windows |

---

## Build & Run

### Option A — Visual Studio
1. Open `BTMultiOut.csproj` in Visual Studio 2022+
2. Press `F5` to build and run

### Option B — Command Line
```
cd BTMultiOut
dotnet run
```

### Option C — Build a single .exe
```
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
The `.exe` will appear in `bin\Release\net8.0-windows\win-x64\publish\`

---

## How to Use

1. **Connect** your Bluetooth speakers/headphones in Windows Settings → Bluetooth
2. **Launch** BTMultiOut
3. **Tick** the devices you want audio sent to
4. Click **▶ Start Streaming**
5. Play anything on your PC — it streams to all ticked devices

Minimising the window sends it to the **system tray**; streaming continues in the background.

---

## How it Works

```
Windows System Audio
       ↓
  WASAPI Loopback Capture  (captures whatever your PC is playing)
       ↓
  BTMultiOut Engine
  ┌────┴──────┐────────┐
  ↓           ↓        ↓
Device 1   Device 2  Device 3
(BT Speaker)(BT Headphones)(etc.)
```

NAudio's `WasapiLoopbackCapture` taps into the system audio stream.  
The same buffer is written to each selected `WasapiOut` device in parallel.  
Format conversion (sample rate, bit depth) is handled automatically.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| Device not in list | Pair it in Windows first, then click **Refresh** |
| No sound on a device | Make sure it's connected (not just paired) before pressing Start |
| Audio crackling | Increase the buffer (change `200` in `WasapiOut` to `400` in `MultiOutputEngine.cs`) |
| Build errors | Run `dotnet restore` then try again |
| "Access denied" error | Run as Administrator |

---

## Dependencies

- [NAudio](https://github.com/naudio/NAudio) — MIT License — the only external dependency
