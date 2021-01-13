using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace IFNMU_API_NORM.ViewModels
{
    public class FileViewModel
    {
        public Guid DirectoryId { get; set; }
        public List<IFormFile> FormFiles { get; set; }
    }
}