// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace CmdPalPikPak;

internal static class RcloneHelpers
{
    public static void DelayedRefresh(string rclonePath, string remote, string dir, string rcAddr)
    {
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000); // 5s delay

                using var p = new Process();
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = string.IsNullOrWhiteSpace(rclonePath) ? "rclone" : rclonePath,
                    // rclone rc vfs/refresh dir="My Pack" --rc-addr localhost:52345 --timeout 2m
                    Arguments = $"rc vfs/refresh dir=\"{dir}\" --rc-addr {rcAddr} --timeout 2m",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                p.Start();
                await p.WaitForExitAsync();
            }
            catch
            {
                // ignore background errors
            }
        });
    }
}
