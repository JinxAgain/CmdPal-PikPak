// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPalPikPak;

internal sealed partial class AddOfflineDownloadPage : ContentPage
{
    private readonly AddOfflineForm form = new();

    public override IContent[] GetContent() => [form];

    public AddOfflineDownloadPage()
    {
        Name = "Open";
        Title = "Add Offline Download";
        Icon = new IconInfo("\uE8A7"); // Link glyph
    }
}

internal sealed partial class AddOfflineForm : FormContent
{
    public AddOfflineForm()
    {
        TemplateJson = $$"""
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.6",
  "body": [
    {
      "type": "TextBlock",
      "size": "medium",
      "weight": "bolder",
      "text": "Add PikPak Offline Download",
      "wrap": true,
      "style": "heading"
    },
    {
      "type": "Input.Text",
      "label": "URL",
      "id": "url",
      "isRequired": true,
      "errorMessage": "URL is required",
      "placeholder": "Enter a http(s)/magnet/ed2k link",
      "value": "${defaults.url}"
    },
    {
      "type": "Input.Text",
      "label": "Save directory",
      "id": "dir",
      "placeholder": "e.g. /My Pack or Movies/2025",
      "value": "${defaults.dir}"
    },
    {
      "type": "Input.Text",
      "label": "Remote name",
      "id": "remote",
      "placeholder": "e.g. PikPak",
      "value": "${defaults.remote}"
    }
  ],
  "actions": [
    {
      "type": "Action.Submit",
      "title": "Add"
    }
  ]
}
""";

        DataJson = $$"""
{
  "defaults": {
    "url": "",
    "dir": "{{AppSettings.DefaultSaveDir}}",
    "remote": "{{AppSettings.RemoteName}}"
  }
}
""".Replace("{{AppSettings.DefaultSaveDir}}", EscapeForJson(AppSettings.DefaultSaveDir))
     .Replace("{{AppSettings.RemoteName}}", EscapeForJson(AppSettings.RemoteName));
    }

    public override CommandResult SubmitForm(string payload)
    {
        var obj = JsonNode.Parse(payload) as JsonObject;
        if (obj is null)
        {
            return CommandResult.ShowToast(new ToastArgs { Message = "Invalid form payload.", Result = CommandResult.KeepOpen() });
        }

        string url = ReadString(obj, "url") ?? string.Empty;
        string remote = ReadString(obj, "remote") ?? AppSettings.RemoteName;
        string dir = ReadString(obj, "dir") ?? AppSettings.DefaultSaveDir;

        url = url.Trim();
        remote = string.IsNullOrWhiteSpace(remote) ? AppSettings.RemoteName : remote.Trim();
        dir = NormalizeDir(string.IsNullOrWhiteSpace(dir) ? AppSettings.DefaultSaveDir : dir.Trim());

        if (string.IsNullOrWhiteSpace(url))
        {
            return CommandResult.ShowToast(new ToastArgs { Message = "URL is required.", Result = CommandResult.KeepOpen() });
        }

        var (ok, stdout, stderr) = RunRcloneAddUrl(AppSettings.RclonePath, remote, dir, url);
        if (ok)
        {
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = $"Submitted offline task to {remote}:{dir}.",
                Result = CommandResult.Hide()
            });
        }
        else
        {
            string msg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            if (string.IsNullOrWhiteSpace(msg)) msg = "rclone failed to start.";
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = $"Failed to add offline task: {TrimForToast(msg)}",
                Result = CommandResult.KeepOpen()
            });
        }
    }

    private static string NormalizeDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return "My Pack";
        dir = dir.Replace("\\", "/");
        while (dir.StartsWith("/")) dir = dir.Substring(1);
        if (string.IsNullOrWhiteSpace(dir)) dir = "My Pack";
        return dir;
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

    private static string ReadString(JsonObject obj, string key)
    {
        return obj.TryGetPropertyValue(key, out JsonNode? node) && node is not null
            ? node.GetValue<string>()
            : string.Empty;
    }

    private static string EscapeForJson(string s)
    {
        if (s is null) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string TrimForToast(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Replace("\r", " ").Replace("\n", " ");
        if (s.Length > 200) s = s.Substring(0, 200) + "...";
        return s;
    }
}
