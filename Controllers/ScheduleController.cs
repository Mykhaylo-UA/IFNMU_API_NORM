using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
             
             List<LectionInfo> lectionInfos = new List<LectionInfo>();
             
             if (schedules[0].LectionInfo != null)
             {
                 
                 string[] lectionSlesh = schedules[0].LectionInfo.Trim().Split("/");
                 foreach (string ls in lectionSlesh)
                 {
                     string[] lectionDefis = ls.Trim().Split("-");
                     LectionInfo info = new LectionInfo() {Letter = lectionDefis[0].Trim()};

                     string[] groups = lectionDefis[1].Trim().Split(",");

                     foreach (string s in groups)
                     {
                         info.Groups.Add(s.Trim());
                     }

                     lectionInfos.Add(info);
                 }
             }
             

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
             
             Regex regex = new Regex(@"\W\w\s\d*\W");
             for(int b=0; b<lessons.Count; b++)
             {
                 MatchCollection matches = regex.Matches(lessons[b].Name);
                 if (matches.Count > 0)
                 {
                     foreach (Match match in matches)
                     {
                         lessons[b].Name = lessons[b].Name.Replace(match.Value, "").Trim();
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
                             
                             if (les.LessonType == LessonType.Lection)
                             {
                                 string lec = "";
                                 bool brea = false;
                                 foreach (var s in lectionInfos)
                                 {
                                     foreach (var x in s.Groups)
                                     {
                                         if (les.Day.Week.Schedule.Group == x)
                                         {
                                             lec = $"{s.Letter},{les.Number}";
                                         }
                                         brea = true;
                                     }

                                     if (brea)
                                     {
                                         break;
                                     }
                                 }

                                 if (name.Contains(lec))
                                 {
                                     continue;
                                 }
                                 else
                                 {
                                     name += lec + " ";
                                     continue;
                                 }
                             }
                             
                             name += les.Day.Week.Schedule.Group + " ";
                         }
                         
                         lvm.Add(new LessonViewModel(){Number = k.Key, String = name});
                     }

                     model.Days.First(d => d.DayOfWeek == o.Key).Lessons.Add(lvm);
                 }
             }
             
             return Ok(model);
         }
         
         [HttpPost]
         [Route("addShortSchedule")]
         public async Task<IActionResult> AddShortSchedule([FromBody]ScheduleViewModel model, [FromQuery]byte? course, [FromQuery]Faculty? faculty, [FromQuery]byte? numberWeek, [FromQuery]string lectionInfo)
         {
             if(model == null) return BadRequest("Model is null");
             if(model.Days == null) return BadRequest("Model.Days.Count is null");
             if(faculty == null) return BadRequest("faculty is null");
             if(numberWeek == null) return BadRequest("numberWeek is null");
             if(course == null) return BadRequest("course is null");

             List<LectionInfo> lectionInfos = new List<LectionInfo>();
             
             if (lectionInfo != null)
             {
                 
                 string[] lectionSlesh = lectionInfo.Trim().Split("/");
                 foreach (string ls in lectionSlesh)
                 {
                     string[] lectionDefis = ls.Trim().Split("-");
                     LectionInfo info = new LectionInfo() {Letter = lectionDefis[0].Trim()};

                     string[] groups = lectionDefis[1].Trim().Split(",");

                     foreach (string s in groups)
                     {
                         info.Groups.Add(s.Trim());
                     }

                     lectionInfos.Add(info);
                 }
             }

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
                             if (group.Contains(","))
                             {
                                 string[] info = group.Split(",");
                                 string name = $"{model.NameLessons[a]} (Л {info[1]})";

                                 LectionInfo inf = lectionInfos.FirstOrDefault(g => g.Letter.ToUpper() == info[0].Trim().ToUpper());

                                 if (inf == null) continue;
                                 foreach (var gr in inf.Groups)
                                 {
                                     Schedule schedule = schedules.FirstOrDefault(s => s.Group == gr);
                                     if (schedule == null)
                                     {
                                         schedule = new Schedule()
                                         {
                                             ScheduleType = ScheduleType.Short,
                                             Group = gr,
                                             Course = (byte)course,
                                             Faculty = (Faculty)faculty,
                                             LectionInfo = lectionInfo
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
                                         Name = name,
                                         Number = day.Lessons[a][i].Number,
                                         LessonType = LessonType.Lection,
                                         NumberAuditor = info[1]
                                     };
                             
                                     schedule.Weeks.First(w=> w.WeekNumber==numberWeek).Days.First(d=>d.DayOfWeek==day.DayOfWeek).Lessons.Add(lesson);
                                 }
                                 
                             }
                             else
                             {
                                 Schedule schedule = schedules.FirstOrDefault(s => s.Group == group);
                                 if (schedule == null)
                                 {
                                     schedule = new Schedule()
                                     {
                                         ScheduleType = ScheduleType.Short,
                                         Group = group,
                                         Course = (byte)course,
                                         Faculty = (Faculty)faculty,
                                         LectionInfo = lectionInfo
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
                                     LessonType = LessonType.Practice,
                                 };
                             
                                 schedule.Weeks.First(w=> w.WeekNumber==numberWeek).Days.First(d=>d.DayOfWeek==day.DayOfWeek).Lessons.Add(lesson);    
                             }
                         }
                     }
                 }
             }

             _context.Schedules.AddRange(schedules);
             await _context.SaveChangesAsync();
             
             return Ok(schedules);
         }

         [HttpPut]
         [Route("editShortSchedule")]
         public async Task<IActionResult> EditShortSchedule([FromBody] ScheduleViewModel model,[FromQuery] byte? course, [FromQuery] Faculty? faculty, [FromQuery] byte? numberWeek, [FromQuery] string lectionInfo)
         {
             if(model == null) return BadRequest("Model is null");
             if(model.Days == null) return BadRequest("Model.Days.Count is null");
             if(faculty == null) return BadRequest("faculty is null");
             if(numberWeek == null) return BadRequest("numberWeek is null");
             if(course == null) return BadRequest("course is null");

             List<LectionInfo> lectionInfos = new List<LectionInfo>();
             
             if (lectionInfo != null)
             {
                 
                 string[] lectionSlesh = lectionInfo.Trim().Split("/");
                 foreach (string ls in lectionSlesh)
                 {
                     string[] lectionDefis = ls.Trim().Split("-");
                     LectionInfo info = new LectionInfo() {Letter = lectionDefis[0].Trim()};

                     string[] groups = lectionDefis[1].Trim().Split(",");

                     foreach (string s in groups)
                     {
                         info.Groups.Add(s.Trim());
                     }

                     lectionInfos.Add(info);
                 }
             }

             List<Schedule> schedules = await _context.Schedules.Where(s=> s.Course == (byte)course && s.Faculty == (Faculty)faculty)
                 .Include(s=> s.Weeks.Where(w=>w.WeekNumber==numberWeek)).ToListAsync();

             foreach (var schedule in schedules)
             {
                 _context.Weeks.RemoveRange(schedule.Weeks);
             }
             await _context.SaveChangesAsync();
             
             foreach (DayViewModel day in model.Days)
             {
                 for(int a =0; a< day.Lessons.Count; a++)
                 {
                     for(int i=0; i< day.Lessons[a].Count; i++)
                     {
                         string[] splitStrings = day.Lessons[a][i].String.Split(" ");

                         foreach (string group in splitStrings)
                         {
                             if (group.Contains(","))
                             {
                                 string[] info = group.Split(",");
                                 string name = $"{model.NameLessons[a]} (Л {info[1]})";

                                 LectionInfo inf = lectionInfos.FirstOrDefault(g => g.Letter.ToUpper() == info[0].Trim().ToUpper());

                                 if (inf == null) continue;
                                 foreach (var gr in inf.Groups)
                                 {
                                     Schedule schedule = schedules.FirstOrDefault(s => s.Group == gr);
                                     if (schedule == null)
                                     {
                                         schedule = new Schedule()
                                         {
                                             ScheduleType = ScheduleType.Short,
                                             Group = gr,
                                             Course = (byte)course,
                                             Faculty = (Faculty)faculty,
                                             LectionInfo = lectionInfo
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

                                     if (schedule.Weeks.FirstOrDefault(w => w.WeekNumber == numberWeek) == null)
                                     {
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
                                     }

                                     Lesson lesson = new Lesson()
                                     {
                                         Name = name,
                                         Number = day.Lessons[a][i].Number,
                                         LessonType = LessonType.Lection,
                                         NumberAuditor = info[1]
                                     };
                             
                                     schedule.Weeks.First(w=> w.WeekNumber==numberWeek).Days.First(d=>d.DayOfWeek==day.DayOfWeek).Lessons.Add(lesson);
                                 }
                                 
                             }
                             else
                             {
                                 Schedule schedule = schedules.FirstOrDefault(s => s.Group == group);
                                 if (schedule == null)
                                 {
                                     schedule = new Schedule()
                                     {
                                         ScheduleType = ScheduleType.Short,
                                         Group = group,
                                         Course = (byte)course,
                                         Faculty = (Faculty)faculty,
                                         LectionInfo = lectionInfo
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
                                 
                                 if (schedule.Weeks.FirstOrDefault(w => w.WeekNumber == numberWeek) == null)
                                 {
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
                                 }
                                 
                                 Lesson lesson = new Lesson()
                                 {
                                     Name = model.NameLessons[a],
                                     Number = day.Lessons[a][i].Number,
                                     LessonType = LessonType.Practice,
                                 };
                             
                                 schedule.Weeks.First(w=> w.WeekNumber==numberWeek).Days.First(d=>d.DayOfWeek==day.DayOfWeek).Lessons.Add(lesson);    
                             }
                         }
                     }
                 }
             }

             foreach (var s in schedules)
             {
                 _context.Weeks.Add(s.Weeks.First(w => w.WeekNumber == numberWeek));
             }
             await _context.SaveChangesAsync();
             
             return Ok(schedules);
         }
     }
 }