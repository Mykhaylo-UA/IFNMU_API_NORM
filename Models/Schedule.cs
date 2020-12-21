using System.Collections.Generic;
using IFNMU_API_NORM.Abstract;

namespace IFNMU_API_NORM.Models
{
    public class Schedule : BaseModel
    {
        public ScheduleType ScheduleType { get; set; }
        public string Group { get; set; }
        public byte Course { get; set; }
        public Faculty Faculty { get; set; }
        
        public List<Week> Weeks { get; set; }

        public Schedule()
        {
            Weeks = new List<Week>();
        }
    }
}