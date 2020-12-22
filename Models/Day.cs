using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using IFNMU_API_NORM.Abstract;

namespace IFNMU_API_NORM.Models
{
    public class Day : BaseModel
    {
        public DayOfWeek? DayOfWeek { get; set; }
        public DateTime? DateTime { get; set; }
        
        [JsonIgnore]
        public Guid WeekId { get; set; }
        [JsonIgnore]
        public Week Week { get; set; }
        
        public List<Lesson> Lessons { get; set; }

        public Day()
        {
            Lessons = new List<Lesson>();
        }
    }
}