using System.Collections.Generic;
using System.ComponentModel;

namespace Xname.DeveloperTools;

internal sealed class Config
{
    [Description("Error handling options")]
    public bool LoggingToConsoleEnabled { get; set; } = true;

    public bool LoggingToFileEnabled { get; set; } = false;

    public List<string> LogBlacklist { get; set; } = new();
}
