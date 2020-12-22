using System.Collections.Generic;
using IFNMU_API_NORM.Models;

namespace IFNMU_API_NORM.ViewModels
{
    public class ScheduleViewModel
    {
        public List<DayViewModel> Days { get; set; }
        
        public List<string> NameLessons { get; set; }

        public ScheduleViewModel()
        {
            NameLessons = new List<string>();
            Days = new List<DayViewModel>();
        }
    }
}