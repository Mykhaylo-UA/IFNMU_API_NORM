using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using IFNMU_API_NORM.Abstract;

namespace IFNMU_API_NORM.Models
{
    public class Week : BaseModel
    {
        public WeekType WeekType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        public byte? WeekNumber { get; set; } 
        
        [JsonIgnore]
        public Guid ScheduleId { get; set; }
        [JsonIgnore]
        public Schedule Schedule { get; set; }
        
        public List<Day> Days { get; set; }

        public Week()
        {
            Days = new List<Day>();
        }
    }
}