using System;
using System.Collections.Generic;
using IFNMU_API_NORM.Abstract;
using System.Text.Json.Serialization;

namespace IFNMU_API_NORM.Models
{
    public class SubDirectory : BaseModel
    {
        public string Name { get; set; }
        
        public List<FileInformation> Files {get; set; }
        
        public Guid DirectoryInformationId { get; set; }

        [JsonIgnore]
        public DirectoryInformation DirectoryInformation { get; set; }

        public SubDirectory()
        {
            Files = new List<FileInformation>();
        }
    }
}