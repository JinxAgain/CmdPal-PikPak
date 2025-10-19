// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPalPikPak;

internal sealed partial class CmdPalPikPakPage : DynamicListPage
{
    public CmdPalPikPakPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "PikPak";
        Name = "Open";
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => RaiseItemsChanged();

    public override IListItem[] GetItems()
    {
        var remote = AppSettings.RemoteName;
        var dir = NormalizeDir(AppSettings.DefaultSaveDir);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var addCmd = new AddFromQueryCommand(this);
            return [
                new ListItem(addCmd)
                {
                    Title = "Add offline download",
                    Subtitle = $"{remote}:{dir} — {TrimForSubtitle(SearchText)}",
                },
                new ListItem(new AddOfflineDownloadPage()) { Title = "Add Offline Download (Form)" },
                new ListItem(new SettingsPage()) { Title = "Settings" },
            ];
        }

        return [
            new ListItem(new AddOfflineDownloadPage()) { Title = "Add Offline Download" },
            new ListItem(new SettingsPage()) { Title = "Settings" },
        ];
    }

    private static string NormalizeDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return "My Pack";
        dir = dir.Replace("\\", "/");
        while (dir.StartsWith("/")) dir = dir.Substring(1);
        if (string.IsNullOrWhiteSpace(dir)) dir = "My Pack";
        return dir;
    }

    private static string TrimForSubtitle(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length > 80 ? s.Substring(0, 80) + "…" : s;
    }

    private static (bool ok, string stdout, string stderr) RunRcloneAddUrl(string rclonePath, string remote, string dir, string url)
    {
        try
        {
            using var p = new Process();
            p.StartInfo = new ProcessStartInfo
            {
                FileName = string.IsNullOrWhiteSpace(rclonePath) ? "rclone" : rclonePath,
                Arguments = $"backend addurl \"{remote}:{dir}\" \"{url}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            p.Start();
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            bool ok = p.ExitCode == 0;
            return (ok, stdout, stderr);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    private sealed class AddFromQueryCommand : InvokableCommand
    {
        private readonly CmdPalPikPakPage _page;

        public AddFromQueryCommand(CmdPalPikPakPage page)
        {
            _page = page;
            Name = "Add from query";
            Icon = new IconInfo("\uE8A7");
        }

        public override CommandResult Invoke()
        {
            var url = _page.SearchText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                return CommandResult.ShowToast(new ToastArgs { Message = "URL is required.", Result = CommandResult.KeepOpen() });
            }

            var remote = AppSettings.RemoteName;
            var dir = NormalizeDir(AppSettings.DefaultSaveDir);
            var validateError = ValidateEnvironment(AppSettings.RclonePath, remote);
            if (!string.IsNullOrEmpty(validateError))
            {
                return CommandResult.ShowToast(new ToastArgs { Message = validateError, Result = CommandResult.KeepOpen() });
            }
            var (ok, stdout, stderr) = RunRcloneAddUrl(AppSettings.RclonePath, remote, dir, url);
            if (ok)
            {
                return CommandResult.ShowToast(new ToastArgs { Message = $"Submitted offline task to {remote}:{dir}.", Result = CommandResult.Hide() });
            }

            string msg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            if (string.IsNullOrWhiteSpace(msg)) msg = "rclone failed to start.";
            return CommandResult.ShowToast(new ToastArgs { Message = $"Failed to add offline task: {TrimForSubtitle(msg)}", Result = CommandResult.KeepOpen() });
        }
    }

    private static string? ValidateEnvironment(string rclonePath, string remote)
    {
        if (string.IsNullOrWhiteSpace(remote))
        {
            return "Remote name is empty. Open Settings to configure it.";
        }

        // If user specified an absolute path, check existence first
        if (!string.IsNullOrWhiteSpace(rclonePath) && (rclonePath.Contains('\\') || rclonePath.Contains('/')))
        {
            try
            {
                if (!File.Exists(rclonePath))
                {
                    return $"rclone executable not found: {rclonePath}";
                }
            }
            catch { /* ignore IO errors */ }
        }

        // Check rclone version quickly
        var (verOk, _, verErr) = RunSmall("version", rclonePath, timeoutMs: 3000);
        if (!verOk)
        {
            var hint = string.IsNullOrWhiteSpace(verErr) ? "Unable to execute rclone." : verErr;
            return $"Cannot run rclone. Check path in Settings. Details: {TrimForSubtitle(hint)}";
        }

        // Verify remote exists via listremotes
        var (lrOk, lrOut, _) = RunSmall("listremotes", rclonePath, timeoutMs: 4000);
        if (lrOk)
        {
            var target = remote.EndsWith(":", StringComparison.Ordinal) ? remote : remote + ":";
            var found = false;
            using (var reader = new StringReader(lrOut ?? string.Empty))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.Equals(line.Trim(), target, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                return $"Remote '{remote}' not found. Run 'rclone config' to create it, or update Remote name in Settings.";
            }
        }

        return null;
    }

    private static (bool ok, string stdout, string stderr) RunSmall(string subcommand, string rclonePath, int timeoutMs)
    {
        try
        {
            using var p = new Process();
            p.StartInfo = new ProcessStartInfo
            {
                FileName = string.IsNullOrWhiteSpace(rclonePath) ? "rclone" : rclonePath,
                Arguments = subcommand,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            p.Start();
            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(true); } catch { }
                return (false, string.Empty, "rclone timed out");
            }
            var outText = p.StandardOutput.ReadToEnd();
            var errText = p.StandardError.ReadToEnd();
            return (p.ExitCode == 0, outText, errText);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }
}
