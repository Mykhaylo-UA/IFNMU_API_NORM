using System;
using System.Text.Json.Serialization;
using IFNMU_API_NORM.Abstract;

namespace IFNMU_API_NORM.Models
{
    public class Lesson : BaseModel
    {
        public string Name { get; set; }
        public byte Number { get; set; }
        public LessonType LessonType { get; set; }
        public string NumberAuditor { get; set; }
        
        [JsonIgnore]
        public Guid DayId { get; set; }
        [JsonIgnore]
        public Day Day { get; set; }
    }
}