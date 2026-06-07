; ============================================================
;  BT MultiOut - Combined Installer
;  Packages: VB-CABLE driver + BTMultiOut application
;  Requires: NSIS 3.x  (https://nsis.sourceforge.io)
;
;  HOW TO BUILD:
;    1. Place these files in the same folder as this .nsi:
;         - VBCABLE_Setup_x64.exe   (from vb-audio.com/Cable)
;         - BTMultiOut.exe          (dotnet publish output)
;    2. Right-click installer.nsi → "Compile NSIS Script"
;       OR run:  makensis installer.nsi
;    Output: BTMultiOut-Setup.exe
; ============================================================

!define APP_NAME        "BT MultiOut"
!define APP_VERSION     "1.0.0"
!define APP_EXE         "BTMultiOut.exe"
!define VBCABLE_DIR     "VBCABLE_Driver_Pack45"
!define VBCABLE_EXE     "VBCABLE_Setup_x64.exe"
!define INSTALL_DIR     "$PROGRAMFILES64\BTMultiOut"
!define UNINSTALL_KEY   "Software\Microsoft\Windows\CurrentVersion\Uninstall\BTMultiOut"

; ── Basic metadata ────────────────────────────────────────────────────────────
Name              "${APP_NAME} ${APP_VERSION}"
OutFile           "BTMultiOut-Setup.exe"
InstallDir        "${INSTALL_DIR}"
InstallDirRegKey  HKLM "${UNINSTALL_KEY}" "InstallLocation"
RequestExecutionLevel admin
SetCompressor     /SOLID lzma
Unicode           True

; ── Modern UI ─────────────────────────────────────────────────────────────────
!include "MUI2.nsh"

!define MUI_ABORTWARNING
!define MUI_ICON   "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

!define MUI_BGCOLOR          "0F111A"
!define MUI_TEXTCOLOR        "00C8A0"

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE   "LICENSE.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; ── Version info ──────────────────────────────────────────────────────────────
VIProductVersion                    "1.0.0.0"
VIAddVersionKey "ProductName"       "${APP_NAME}"
VIAddVersionKey "ProductVersion"    "${APP_VERSION}"
VIAddVersionKey "FileDescription"   "${APP_NAME} Installer"
VIAddVersionKey "LegalCopyright"    "Open Source"

; ═════════════════════════════════════════════════════════════════════════════
;  INSTALL — Application
; ═════════════════════════════════════════════════════════════════════════════
Section "BT MultiOut Application" SecApp
  SectionIn RO

  SetOutPath "${INSTALL_DIR}"
  File "${APP_EXE}"
  WriteUninstaller "${INSTALL_DIR}\Uninstall.exe"

  WriteRegStr   HKLM "${UNINSTALL_KEY}" "DisplayName"     "${APP_NAME}"
  WriteRegStr   HKLM "${UNINSTALL_KEY}" "DisplayVersion"  "${APP_VERSION}"
  WriteRegStr   HKLM "${UNINSTALL_KEY}" "Publisher"       "Open Source"
  WriteRegStr   HKLM "${UNINSTALL_KEY}" "InstallLocation" "${INSTALL_DIR}"
  WriteRegStr   HKLM "${UNINSTALL_KEY}" "UninstallString" '"${INSTALL_DIR}\Uninstall.exe"'
  WriteRegStr   HKLM "${UNINSTALL_KEY}" "DisplayIcon"     "${INSTALL_DIR}\${APP_EXE}"
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoModify"        1
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoRepair"        1

  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut  "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "${INSTALL_DIR}\${APP_EXE}"
  CreateShortcut  "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk"   "${INSTALL_DIR}\Uninstall.exe"
  CreateShortcut  "$DESKTOP\${APP_NAME}.lnk"                "${INSTALL_DIR}\${APP_EXE}"

SectionEnd

; ═════════════════════════════════════════════════════════════════════════════
;  INSTALL — VB-CABLE driver
; ═════════════════════════════════════════════════════════════════════════════
Section "VB-CABLE Virtual Audio Driver" SecVBCable

  ; ── Save current default audio device before installing ──────────────────
  ReadRegStr $1 HKCU "Software\Microsoft\Multimedia\Sound Mapper" "Playback"
  DetailPrint "Saving current default audio device: $1"

  ; ── Extract VB-CABLE installer (full driver pack — .inf/.sys/.cat required) ──
  SetOutPath "$TEMP\BTMultiOut-VBCable"
  File /r "${VBCABLE_DIR}\*.*"

  ; ── Inform user ───────────────────────────────────────────────────────────
  MessageBox MB_ICONINFORMATION|MB_OK \
    "VB-CABLE driver will now be installed.$\n$\n\
Windows will show a security dialog asking:$\n\
  'Would you like to install this device software?'$\n$\n\
Click INSTALL to continue. This is required for silent audio routing.$\n$\n\
Your default audio device will be restored after installation."

  ; ── Run installer ─────────────────────────────────────────────────────────
  ; NSIS already has admin rights via RequestExecutionLevel, so ExecWait
  ; inherits them — no extra elevation step needed.
  DetailPrint "Installing VB-CABLE driver..."
  ExecWait '"$TEMP\BTMultiOut-VBCable\${VBCABLE_EXE}" -i -h' $0
  Sleep 2000

  ${If} $0 == 0
    DetailPrint "VB-CABLE installed successfully."
  ${ElseIf} $0 == 1
    DetailPrint "VB-CABLE already installed — skipping."
  ${Else}
    ; Fallback — open installer visibly so user can click through manually
    MessageBox MB_ICONINFORMATION|MB_OK \
      "Automatic install failed (code $0).$\n$\n\
The VB-CABLE installer will now open.$\n\
Please click 'Install Driver' when it appears."
    ExecWait '"$TEMP\BTMultiOut-VBCable\${VBCABLE_EXE}"'
  ${EndIf}

  ; ── Restore original default audio device ────────────────────────────────
  ; Uses BTMultiOut.exe --set-default which calls IPolicyConfig COM interface
  ; — the only reliable way to set default audio device programmatically.
  ${If} $1 != ""
    DetailPrint "Restoring default audio device: $1"
    ExecWait '"${INSTALL_DIR}\${APP_EXE}" --set-default "$1"'
    DetailPrint "Default audio device restored."
  ${EndIf}

  ; ── Cleanup ───────────────────────────────────────────────────────────────
  RMDir /r "$TEMP\BTMultiOut-VBCable"

SectionEnd

; ═════════════════════════════════════════════════════════════════════════════
;  Post-install reboot prompt
; ═════════════════════════════════════════════════════════════════════════════
Section "-FinishReboot" SecReboot
  MessageBox MB_ICONQUESTION|MB_YESNO \
    "Installation complete!$\n$\n\
A restart is recommended for the VB-CABLE driver to take effect.$\n\
Restart now?" \
    IDNO done

  Reboot

  done:
SectionEnd

; ═════════════════════════════════════════════════════════════════════════════
;  UNINSTALL
; ═════════════════════════════════════════════════════════════════════════════
Section "Uninstall"

  Delete "${INSTALL_DIR}\${APP_EXE}"
  Delete "${INSTALL_DIR}\Uninstall.exe"
  RMDir  "${INSTALL_DIR}"

  Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk"
  RMDir  "$SMPROGRAMS\${APP_NAME}"
  Delete "$DESKTOP\${APP_NAME}.lnk"

  DeleteRegKey HKLM "${UNINSTALL_KEY}"

  MessageBox MB_ICONINFORMATION|MB_OK \
    "${APP_NAME} has been removed.$\n$\n\
VB-CABLE driver was NOT removed (it may be used by other apps).$\n\
To remove it: Device Manager → Sound → CABLE Output → Uninstall device."

SectionEnd
