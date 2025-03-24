using System.Collections.Generic;

namespace WeModPatcher.Models
{

    public enum EPatchType
    {
        ActivatePro = 1,
        DisableUpdates = 2,
        DisableTelemetry = 4
    }

    public enum EPatchProcessMethod
    {
        None = 0,
        Runtime = 1,
        Static = 2
    }
    
    public sealed class PatchConfig
    {
        public HashSet<EPatchType> PatchTypes { get; set; }
        public EPatchProcessMethod PatchMethod { get; set; }
        public string Path { get; set; }
    }
}