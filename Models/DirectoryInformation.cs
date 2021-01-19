using System.Collections.Generic;
using IFNMU_API_NORM.Abstract;
using System.Text.Json.Serialization;

namespace IFNMU_API_NORM.Models
{
    public class DirectoryInformation : BaseModel
    {
        public string Name { get; set; }
        
        public byte Course { get; set; }
        
        public string NameLesson { get; set; }
        
        public List<FileInformation> Files { get; set; }
        
        public List<SubDirectory> SubDirectories { get; set; }
        
        public List<Link> Links { get; set; }

        public Faculty Faculty { get; set; }

        public DirectoryInformation()
        {
            Files = new List<FileInformation>();
            SubDirectories = new List<SubDirectory>();
            Links = new List<Link>();
        }
    }
}