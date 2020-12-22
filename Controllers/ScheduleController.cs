using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFNMU_API_NORM.Models;
using IFNMU_API_NORM.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace IFNMU_API_NORM.Controllers
 {
     [ApiController]
     [Route("[controller]")]
     public class ScheduleController : ControllerBase
     {
         private readonly DatabaseContext _context;
         
         public ScheduleController(DatabaseContext context)
         {
             _context = context;
         }

         [HttpGet]
         [Route("getShortSchedule")]
         public async Task<IActionResult> GetShortShedule([FromQuery]byte? course, [FromQuery]Faculty? faculty, [FromQuery]byte? numberWeek)
         {
             if (course == null) return BadRequest("course is null");
             if (faculty == null) return BadRequest("faculty is null");
             if (numberWeek == null) return BadRequest("numberWeek is null");

             ScheduleViewModel model = new ScheduleViewModel()
             {
                 Days = new List<DayViewModel>()
                 {
                     new DayViewModel(){DayOfWeek = DayOfWeek.Monday},
                     new DayViewModel(){DayOfWeek = DayOfWeek.Tuesday},
                     new DayViewModel(){DayOfWeek = DayOfWeek.Wednesday},
                     new DayViewModel(){DayOfWeek = DayOfWeek.Thursday},
                     new DayViewModel(){DayOfWeek = DayOfWeek.Friday}
                 }
             };

             List<Schedule> schedules = await _context.Schedules
                 .Include(s => s.Weeks.Where(w=>w.WeekNumber == (byte)numberWeek))
                 .ThenInclude(w=>w.Days)
                 .ThenInclude(d=>d.Lessons)
                 .Where(s => s.Course == (byte)course && s.Faculty == (Faculty)faculty)
                 .ToListAsync();
             
             Console.WriteLine(schedules.Count);
             

             List<Lesson> lessons = new List<Lesson>();
             List<Day> days = new List<Day>();
             
             foreach (var schedule in schedules)
             {
                 foreach (var week in schedule.Weeks)
                 {
                     days.AddRange(week.Days);
                     foreach (var day in week.Days)
                     {
                         lessons.AddRange(day.Lessons);
                     }
                 }
             }

             var groupLessonName = lessons.GroupBy(l => l.Name);

             foreach (var ln in groupLessonName)
             {
                 model.NameLessons.Add(ln.Key);
                 

                 var groupOfDay = ln.GroupBy(l => l.Day.DayOfWeek);

                 foreach (var o in groupOfDay)
                 {
                     List<LessonViewModel> lvm = new List<LessonViewModel>();
                     
                     var groupOfNumber = o.GroupBy(d => d.Number);

                     foreach (var k in groupOfNumber)
                     {
                         string name = "";
                         
                         foreach (var les in k)
                         {
                             name += les.Day.Week.Schedule.Group + " ";
                         }
                         
                         lvm.Add(new LessonViewModel(){Number = k.Key, String = name});
                     }
                     
                     model.Days.First(d=> d.DayOfWeek == o.Key).Lessons.Add(lvm);
                     
                 }
             }
             
             return Ok(model);
         }
         
         [HttpPost]
         [Route("addShortSchedule")]
         public async Task<IActionResult> AddShortSchedule([FromBody]ScheduleViewModel model, [FromQuery]byte? course, [FromQuery]Faculty? faculty, [FromQuery]byte? numberWeek)
         {
             if(model == null) return BadRequest("Model is null");
             if(model.Days == null) return BadRequest("Model.Days.Count is null");
             if(faculty == null) return BadRequest("faculty is null");
             if(numberWeek == null) return BadRequest("numberWeek is null");
             if(course == null) return BadRequest("course is null");

             List<Schedule> schedules = new List<Schedule>();
             
             foreach (DayViewModel day in model.Days)
             {
                 for(int a =0; a< day.Lessons.Count; a++)
                 {
                     for(int i=0; i< day.Lessons[a].Count; i++)
                     {
                         string[] splitStrings = day.Lessons[a][i].String.Split(" ");

                         foreach (string group in splitStrings)
                         {
                             Schedule schedule = schedules.FirstOrDefault(s => s.Group == group);
                             if (schedule == null)
                             {
                                 schedule = new Schedule()
                                 {
                                     ScheduleType = ScheduleType.Short,
                                     Group = group,
                                     Course = (byte)course,
                                     Faculty = (Faculty)faculty
                                 };
                                 schedule.Weeks.Add(new Week()
                                 {
                                     WeekType = WeekType.WithNumber,
                                     WeekNumber = numberWeek,
                                     Days = new List<Day>()
                                     {
                                         new Day(){DayOfWeek = DayOfWeek.Monday},
                                         new Day(){DayOfWeek = DayOfWeek.Tuesday},
                                         new Day(){DayOfWeek = DayOfWeek.Wednesday},
                                         new Day(){DayOfWeek = DayOfWeek.Thursday},
                                         new Day(){DayOfWeek = DayOfWeek.Friday}
                                     }
                                 });
                                 schedules.Add(schedule);
                             }

                             Lesson lesson = new Lesson()
                             {
                                Name = model.NameLessons[a],
                                Number = day.Lessons[a][i].Number,
                                LessonType = LessonType.Practice
                             };
                             
                             schedule.Weeks.First(w=> w.WeekNumber==numberWeek).Days.First(d=>d.DayOfWeek==day.DayOfWeek).Lessons.Add(lesson);
                         }
                     }
                 }
             }

             _context.Schedules.AddRange(schedules);
             await _context.SaveChangesAsync();
             
             return Ok(schedules);
         }
     }
 }