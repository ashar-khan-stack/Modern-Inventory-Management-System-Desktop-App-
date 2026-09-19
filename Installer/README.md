# Windows Installer & Distribution Guide

This directory contains the production packaging and installation tooling for the **Modern Inventory Management System** desktop application.

---

## Packaging Architecture

The distribution pipeline creates two production-ready distribution formats:

1. **Windows Setup Wizard (`ModernInventory_Setup_v1.0.0.exe`)**
   - Built with Inno Setup 6.
   - Self-contained x64 Windows desktop executable.
   - Includes .NET 8 WPF Desktop Runtime & native SQLite (`e_sqlite3.dll`).
   - Supports both Standard (Per-User) and Administrator (Per-Machine) installation modes.
   - Start Menu and Desktop shortcuts.
   - Clean Windows registration in *Installed Apps* / *Control Panel* with dedicated uninstaller.
   - Auto-closes any running application instances before updates to prevent DLL file locks.

2. **Portable ZIP Archive (`ModernInventory_v1.0.0_Portable.zip`)**
   - Zero-installation needed.
   - Extract and double-click `ModernInventory.Desktop.exe`.
   - Stores data in `./Data` or `%LocalAppData%\ModernInventoryDesktop\Data` automatically.

---

## Local Database Safety & Preservation

- User transactions, inventory tables, and SQLite records are stored in:
  `%LocalAppData%\ModernInventoryDesktop\Data\modern_inventory.db`
- During version upgrades or uninstalls, the database and user data directories are **never** deleted.
- Automatic backups continue to be written to the local `Backups` folder.

---

## How to Build on Windows

### Quick Build (Command Prompt / Explorer)
Simply double-click:
```cmd
Installer\build-installer.bat
```

### PowerShell Build
```powershell
.\Installer\build-installer.ps1
```

### Inno Setup Prerequisite (for `.exe` setup wizard)
If Inno Setup is not already installed on the Windows build machine:
```cmd
winget install JRSoftware.InnoSetup
```
Or open `Installer\ModernInventory.iss` in the Inno Setup Compiler GUI and click **Build -> Compile**.
