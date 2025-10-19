// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Windows.Storage;

namespace CmdPalPikPak;

internal static class AppSettings
{
    private static ApplicationDataContainer Local => ApplicationData.Current.LocalSettings;

    public static string RemoteName
    {
        get => GetString("RemoteName", "PikPak");
        set => SetString("RemoteName", string.IsNullOrWhiteSpace(value) ? "PikPak" : value.Trim());
    }

    public static string DefaultSaveDir
    {
        get => GetString("DefaultSaveDir", "/My Pack");
        set => SetString("DefaultSaveDir", string.IsNullOrWhiteSpace(value) ? "/My Pack" : value.Trim());
    }

    public static string RclonePath
    {
        get => GetString("RclonePath", "rclone");
        set => SetString("RclonePath", string.IsNullOrWhiteSpace(value) ? "rclone" : value.Trim());
    }

    private static string GetString(string key, string fallback)
    {
        try
        {
            if (Local.Values.TryGetValue(key, out object? v) && v is string s && !string.IsNullOrWhiteSpace(s))
            {
                return s;
            }
        }
        catch
        {
            // ignore and return fallback
        }
        return fallback;
    }

    private static void SetString(string key, string value)
    {
        try
        {
            Local.Values[key] = value;
        }
        catch
        {
            // ignore persistence errors
        }
    }
}
