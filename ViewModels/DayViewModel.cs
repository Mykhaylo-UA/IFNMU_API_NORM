using System;
using System.Collections.Generic;

namespace IFNMU_API_NORM.ViewModels
{
    public class DayViewModel
    {
        public DateTime? DateTime { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        
        public List<List<LessonViewModel>> Lessons { get; set; }
    }
}