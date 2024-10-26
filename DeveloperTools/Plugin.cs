using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;

namespace Xname.DeveloperTools;

internal sealed class Plugin
{
    public static PluginHandler Handler { get; private set; }

    [PluginConfig]
    public static Config Config;

    [PluginPriority(LoadPriority.Low)]
    [PluginEntryPoint("Developer Tools", "1.0.0", "Plugin that helps with code debugging", "Xname")]
    public void Load()
    {
        Handler = PluginHandler.Get(this);
        EventManager.RegisterAllEvents(this);
    }

    [PluginUnload]
    public void Unload()
    {
        EventManager.UnregisterAllEvents(this);
    }
}
