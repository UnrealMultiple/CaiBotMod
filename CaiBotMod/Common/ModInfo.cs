using System;

namespace CaiBotMod.Common;

public class ModInfo
{
    public ModInfo(string name, Version version)
    {
        this.Name = name;
        this.Version = version;
    }

    public string Name;
    public string Description = null!;
    public string Author = null!;
    public Version Version;
}