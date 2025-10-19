// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPalPikPak;

internal sealed partial class SettingsPage : ContentPage
{
    private readonly SettingsForm form = new();

    public override IContent[] GetContent() => [form];

    public SettingsPage()
    {
        Name = "Open";
        Title = "Settings";
        Icon = new IconInfo("\uE713"); // Settings glyph
    }
}

internal sealed partial class SettingsForm : FormContent
{
    public SettingsForm()
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
      "text": "PikPak Settings",
      "wrap": true,
      "style": "heading"
    },
    {
      "type": "Input.Text",
      "label": "Remote name",
      "id": "remote",
      "placeholder": "e.g. PikPak",
      "value": "${defaults.remote}"
    },
    {
      "type": "Input.Text",
      "label": "Default save directory",
      "id": "dir",
      "placeholder": "e.g. /My Pack or Movies/2025",
      "value": "${defaults.dir}"
    },
    {
      "type": "Input.Text",
      "label": "rclone path",
      "id": "rclone",
      "placeholder": "e.g. rclone or C:/Tools/rclone.exe",
      "value": "${defaults.rclone}"
    }
  ]
}
""";

        DataJson = $$"""
{
  "defaults": {
    "remote": "{{AppSettings.RemoteName}}",
    "dir": "{{AppSettings.DefaultSaveDir}}",
    "rclone": "{{AppSettings.RclonePath}}"
  }
}
""".Replace("{{AppSettings.RemoteName}}", EscapeForJson(AppSettings.RemoteName))
     .Replace("{{AppSettings.DefaultSaveDir}}", EscapeForJson(AppSettings.DefaultSaveDir))
     .Replace("{{AppSettings.RclonePath}}", EscapeForJson(AppSettings.RclonePath));
    }

    public override CommandResult SubmitForm(string payload)
    {
        var obj = JsonNode.Parse(payload) as JsonObject;
        if (obj is null)
        {
            return CommandResult.ShowToast(new ToastArgs { Message = "Invalid form payload.", Result = CommandResult.KeepOpen() });
        }

        string remote = ReadString(obj, "remote");
        string dir = ReadString(obj, "dir");
        string rclone = ReadString(obj, "rclone");

        if (!string.IsNullOrWhiteSpace(remote)) AppSettings.RemoteName = remote.Trim();
        if (!string.IsNullOrWhiteSpace(dir)) AppSettings.DefaultSaveDir = dir.Trim();
        if (!string.IsNullOrWhiteSpace(rclone)) AppSettings.RclonePath = rclone.Trim();

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = "Settings saved.",
            Result = CommandResult.GoBack()
        });
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
}
