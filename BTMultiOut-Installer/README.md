# BT MultiOut — Installer Builder

Produces a single `BTMultiOut-Setup.exe` that installs both:
- **VB-CABLE** virtual audio driver
- **BT MultiOut** application

---

## Prerequisites

| Tool | Download |
|---|---|
| .NET 8 SDK | https://dotnet.microsoft.com/download/dotnet/8.0 |
| NSIS 3.x | https://nsis.sourceforge.io/Download |
| VB-CABLE installer | https://vb-audio.com/Cable/ |

---

## Folder structure required

```
BTMultiOut\                   ← your C# source (existing)
BTMultiOut-Installer\         ← this folder
    installer.nsi
    build-installer.bat
    VBCABLE_Setup_x64.exe     ← YOU must place this here (see below)
```

---

## Step-by-step

### 1. Download VB-CABLE
- Go to https://vb-audio.com/Cable/
- Download the zip
- Extract it
- Copy **`VBCABLE_Setup_x64.exe`** into this `BTMultiOut-Installer\` folder

### 2. Install NSIS
- Download from https://nsis.sourceforge.io/Download
- Install it (default options are fine)
- Make sure `makensis.exe` is in your PATH  
  (NSIS installer usually adds it automatically)

### 3. Run the build script
```
build-installer.bat
```

That's it. It will:
1. `dotnet publish` BTMultiOut as a self-contained single `.exe`
2. Bundle it with `VBCABLE_Setup_x64.exe` into one NSIS installer
3. Output: **`BTMultiOut-Setup.exe`**

---

## What the installer does

When a user runs `BTMultiOut-Setup.exe`:

1. Shows welcome + license screen
2. Lets user choose install directory
3. Copies `BTMultiOut.exe` to `Program Files\BTMultiOut\`
4. Creates Start Menu + Desktop shortcuts
5. Runs VB-CABLE silent install (`-i -h` flags)
   - **One Windows security prompt is unavoidable** — Windows requires user approval for any kernel driver. The user clicks "Install".
6. Registers in Add/Remove Programs
7. Prompts for reboot (needed for driver)

---

## What the uninstaller does

- Removes BTMultiOut and shortcuts
- Does **not** remove VB-CABLE (it may be used by other apps)
- User can remove VB-CABLE manually via Device Manager if needed

---

## Notes

- The installer requires **Admin rights** (needed for driver install)
- Self-contained build means **no .NET runtime required** on target machine
- VB-CABLE is free/donationware — you are redistributing it as-is per their terms
