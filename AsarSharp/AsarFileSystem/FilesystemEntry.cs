using System.Collections.Generic;
using AsarSharp.Integrity;
using Newtonsoft.Json;

namespace AsarSharp.AsarFileSystem
{
    public class FilesystemEntry
    {
        [JsonProperty("files")]
        public Dictionary<string, FilesystemEntry> Files { get; set; }
        
        
        [JsonProperty("executable")]
        public bool? Executable { get; set; }
        
        [JsonProperty("size")]
        public long? Size { get; set; }
        
        [JsonProperty("offset")]
        public string Offset { get; set; }
        
        [JsonProperty("unpacked")]
        public bool? Unpacked { get; set; }
        
        [JsonProperty("integrity")]
        public IntegrityHelper.FileIntegrity Integrity { get; set; }
        
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonIgnore]
        public bool IsDirectory => Files != null;
        [JsonIgnore]
        public bool IsFile => Size.HasValue;
        
        [JsonIgnore]
        public bool IsLink => Link != null;

        public override string ToString()
        {
            return $"Offset: {Offset}, Size: {Size} Unpacked: {Unpacked}";
        }
        
        public bool ShouldSerializeUnpacked()
        {
            return Unpacked == true;
        }
    }
}