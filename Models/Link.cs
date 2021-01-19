using System;
using System.Text.Json.Serialization;
using IFNMU_API_NORM.Abstract;

namespace IFNMU_API_NORM.Models
{
    public class Link : BaseModel
    {
        public string Path { get; set; }
        public string Name { get; set; }
        
        public Guid DirectoryInformationId { get; set; }
        
        [JsonIgnore]
        public DirectoryInformation DirectoryInformation { get; set; }
    }
}