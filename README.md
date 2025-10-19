# CmdPal-PikPak

CmdPal-PikPak is a custom extension for the PowerToys Command Palette that lets you submit PikPak offline download tasks directly from the launcher. Paste a magnet, HTTP, or ED2K link, hit <kbd>Enter</kbd>, and the extension calls `rclone backend addurl` against your configured PikPak remote.

## Features
- **Quick-add shortcut**: Type or paste any supported link in the Command Palette and press <kbd>Enter</kbd> to create a PikPak offline download task.
- **Form-based flow**: An optional Adaptive Card form is available for cases where you want to review or override parameters before submitting.
- **Inline validation**: Verifies the rclone executable and PikPak remote before each submission and surfaces friendly error toasts.
- **Configurable settings**: Built-in settings page to adjust the rclone path, remote name, and default destination directory.

## Requirements
- Windows 11 with PowerToys Command Palette (v0.90 or later).
- `rclone` installed and accessible on `PATH`, with a PikPak backend configured (e.g. remote named `PikPak`).

## Usage
1. Open the PowerToys Command Palette and run the **PikPak** command.
2. To quick-add, paste a magnet/HTTP/ED2K link and press <kbd>Enter</kbd> on **Add offline download**.
3. Use the **Add Offline Download (Form)** entry when you need to specify a different remote or destination.
4. Configure defaults (remote name, default directory, rclone path) via the **Settings** entry.

## Project Structure
- `CmdPalPikPak/Pages/` contains the Command Palette page implementations (`CmdPalPikPakPage`, `AddOfflineDownloadPage`, `SettingsPage`).
- `CmdPalPikPak/Settings.cs` manages persistent configuration via `ApplicationData.LocalSettings`.
- `CmdPalPikPak/Assets/` includes logos used by the extension and app package.

## Roadmap
- Task history and retry commands.
- Optional support for direct PikPak API integration without rclone.
- Localization of UI strings.