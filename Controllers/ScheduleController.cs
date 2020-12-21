using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFNMU_API_NORM.Models;
using IFNMU_API_NORM.ViewModels;
using Microsoft.AspNetCore.Mvc;


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
         public async Task<IActionResult> GetShortShedule()
         {
             
             return Ok();
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