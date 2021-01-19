using System;
using IFNMU_API_NORM.Abstract;
using System.Text.Json.Serialization;

namespace IFNMU_API_NORM.Models
{
    public class FileInformation : BaseModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        
        [JsonIgnore]
        public Guid? DirectoryId { get; set; }
        [JsonIgnore]
        public DirectoryInformation Directory { get; set; }

        public Guid? SubDirectoryId {get;set;}
        [JsonIgnore]
        public SubDirectory SubDirectory {get;set;}
    }
}