// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPalPikPak;

internal sealed partial class CmdPalPikPakPage : ListPage
{
    public CmdPalPikPakPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "PikPak";
        Name = "Open";
    }

    public override IListItem[] GetItems()
    {
        return [
            new ListItem(new AddOfflineDownloadPage()) { Title = "Add Offline Download" },
            new ListItem(new SettingsPage()) { Title = "Settings" },
        ];
    }
}
