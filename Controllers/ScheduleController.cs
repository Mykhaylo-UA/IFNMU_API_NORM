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
     [Route("api/[controller]")]
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
                 .Where(s => s.Course == (byte)course && s.Faculty == (Faculty)faculty  && s.ScheduleType== ScheduleType.Short
                             && s.Weeks.Where(w=>w.WeekNumber==(byte)numberWeek).Count() > 0)
                 .ToListAsync();
             
             if(schedules == null || schedules.Count == 0) return NotFound();

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

             List<SubGroupObject> subGroupList = new List<SubGroupObject>();
             
             Regex regex = new Regex(@"\W\w\s\d*\W");
             Regex regexSubGroup = new Regex(@"\W\w+\s\d*\W+");
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

                 MatchCollection matchSubGroup = regexSubGroup.Matches(lessons[b].Name);
                 if (matchSubGroup.Count > 0)
                 {
                     foreach (Match match in matchSubGroup)
                     {
                         string s = "";
                         if (match.Value.Contains("+"))
                         {
                             s = "+";
                         }
                         else if (match.Value.Contains("-"))
                         {
                             s = "-";
                         }
                         else if (match.Value.Contains("*"))
                         {
                             s = "*";
                         }
                         
                         lessons[b].Name = lessons[b].Name.Replace(match.Value, "").Trim();
                         
                         subGroupList.Add(new SubGroupObject() { IdSubGroup = lessons[b].Id, String = s});
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
                     List<LessonViewModel> lvm = new List<LessonViewModel>()
                         {
                             new LessonViewModel(){Number = 1, String = ""},
                             new LessonViewModel(){Number = 2, String = ""},
                             new LessonViewModel(){Number = 3, String = ""},
                             new LessonViewModel(){Number = 4, String = ""}
                         };

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
                                             brea = true;
                                         }
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

                             if (les.Day.Week.Schedule.Group == "333")
                             {
                                 name += "";
                             }
                             else
                             {
                                 SubGroupObject obj = subGroupList.FirstOrDefault(a=> a.IdSubGroup == les.Id);
                                 
                                 name += les.Day.Week.Schedule.Group+ (obj != null ? obj.String : "") + " ";
                             }
                         }

                         lvm.First(l=>l.Number == k.Key).String = name;
                     }

                     model.Days.First(d => d.DayOfWeek == o.Key).Lessons.Add(lvm);
                 }
             }

             foreach (var a in model.Days)
             {
                 for(int k = 0; k< a.Lessons.Count; k++)
                 { 
                     if (a.Lessons[k].Count < 4)
                     {
                         if(a.Lessons[k].FirstOrDefault(l=> l.Number == 1) == null) a.Lessons[k].Add(new LessonViewModel(){Number=1, String = ""});
                         if(a.Lessons[k].FirstOrDefault(l=> l.Number == 2) == null) a.Lessons[k].Add(new LessonViewModel(){Number=2, String = ""});
                         if(a.Lessons[k].FirstOrDefault(l=> l.Number == 3) == null) a.Lessons[k].Add(new LessonViewModel(){Number=3, String = ""});
                         if(a.Lessons[k].FirstOrDefault(l=> l.Number == 4) == null) a.Lessons[k].Add(new LessonViewModel(){Number=4, String = ""});
                     }

                     
                     a.Lessons[k] = a.Lessons[k].OrderBy(l => l.Number).ToList();
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
                             bool empty = false;
                             
                             if (group.Trim() == String.Empty) empty=true;
                             
                             
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
                                             Faculty = GetFaculty(gr),
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
                                 string subgroup = string.Empty;

                                 if (group.Contains("+"))
                                 {
                                     subgroup = " (підгрупа +)";
                                 }
                                 else if(group.Contains("-"))
                                 {
                                     subgroup = " (підгрупа -)";
                                 }
                                 else if(group.Contains("*"))
                                 {
                                     subgroup = " (підгрупа *)";
                                 }

                                 string newGroup = group.Replace("+", "").Replace("-", "").Replace("*", "");

                                 
                                 Schedule schedule = schedules.FirstOrDefault(s => s.Group == (empty ? "333":newGroup));
                                 if (schedule == null)
                                 {
                                     schedule = new Schedule()
                                     {
                                         ScheduleType = ScheduleType.Short,
                                         Group = (empty ? "333": GetFaculty(newGroup) != faculty ? "333" : newGroup),
                                         Course = (byte)course,
                                         Faculty = (empty ? (Faculty)faculty : GetFaculty(newGroup)),
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

                                Console.WriteLine(model.NameLessons[a] + "    | namelesson after");

                                 Lesson lesson = new Lesson()
                                 {
                                     Name = model.NameLessons[a] + (subgroup != String.Empty ? subgroup : ""),
                                     Number = day.Lessons[a][i].Number,
                                     LessonType = LessonType.Practice,
                                 };
                                    Console.WriteLine(lesson.Name + "    | namelesson before");
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

             List<Schedule> schedules = await _context.Schedules.Where(s=> s.Course == (byte)course && s.Faculty == (Faculty)faculty && s.Weeks.Where(w=>w.WeekNumber==(byte)numberWeek).Count() > 0)
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
                             bool empty = false;
                             
                             if (group.Trim() == String.Empty) empty=true;
                             
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
                                             Id= Guid.NewGuid(),
                                             ScheduleType = ScheduleType.Short,
                                             Group = gr,
                                             Course = (byte)course,
                                             Faculty = GetFaculty(gr),
                                             LectionInfo = lectionInfo
                                         };
                                         schedule = _context.Schedules.Add(schedule).Entity;
                                         
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
                                             },
                                             ScheduleId = schedule.Id
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
                                 string subgroup = string.Empty;

                                 if (group.Contains("+"))
                                 {
                                     subgroup = " (підгрупа +)";
                                 }
                                 else if(group.Contains("-"))
                                 {
                                     subgroup = " (підгрупа -)";
                                 }
                                 else if(group.Contains("*"))
                                 {
                                     subgroup = " (підгрупа *)";
                                 }

                                 string newGroup = group.Replace("+", "").Replace("-", "").Replace("*", "");
                                 
                                 Schedule schedule = schedules.FirstOrDefault(s => s.Group == (empty ? "333":newGroup));
                                 if (schedule == null)
                                 {
                                     schedule = new Schedule()
                                     {
                                         Id= Guid.NewGuid(),
                                         ScheduleType = ScheduleType.Short,
                                         Group = (empty ? "333": GetFaculty(newGroup) != faculty ? "333" : newGroup),
                                         Course = (byte)course,
                                         Faculty = (empty ? (Faculty)faculty : GetFaculty(newGroup)),
                                         LectionInfo = lectionInfo
                                     };
                                     schedule = _context.Schedules.Add(schedule).Entity;
                                     
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
                                         },
                                         ScheduleId = schedule.Id
                                     });
                                 }
                                 
                                 Lesson lesson = new Lesson()
                                 {
                                     Name = model.NameLessons[a] + (subgroup != String.Empty ? subgroup : ""),
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

         

         [HttpGet]
         [Route("getFullSchedule")]
         public async Task<IActionResult> GetFullShedule([FromQuery]byte? course, [FromQuery]Faculty? faculty, [FromQuery]DateTime? startDate)
         {
             if (course == null) return BadRequest("course is null");
             if (faculty == null) return BadRequest("faculty is null");
             if (startDate == null) return BadRequest("startDate is null");

             ScheduleViewModel model = new ScheduleViewModel()
             {
                 Days = new List<DayViewModel>()
                 {
                     new DayViewModel(){DateTime = startDate.GetValueOrDefault()},
                     new DayViewModel(){DateTime = startDate.GetValueOrDefault().AddDays(1)},
                     new DayViewModel(){DateTime = startDate.GetValueOrDefault().AddDays(2)},
                     new DayViewModel(){DateTime = startDate.GetValueOrDefault().AddDays(3)},
                     new DayViewModel(){DateTime = startDate.GetValueOrDefault().AddDays(4)}
                 }
             };

             List<Schedule> schedules = await _context.Schedules
                 .Include(s => s.Weeks.Where(w=>w.StartDate == (DateTime)startDate))
                 .ThenInclude(w=>w.Days)
                 .ThenInclude(d=>d.Lessons)
                 .Where(s => s.Course == (byte)course && s.Faculty == (Faculty)faculty && s.ScheduleType== ScheduleType.Full 
                             && s.Weeks.Where(w=>w.StartDate==startDate).Count() > 0)
                 .ToListAsync();
             
             if(schedules == null || schedules.Count == 0) return NotFound();

             Console.WriteLine(schedules.Count);
             Console.WriteLine(schedules[0]);
             
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

             List<Guid> guidsChangeNumber = new List<Guid>();
             
             List<SubGroupObject> subGroupList = new List<SubGroupObject>();             
             
             Regex regex = new Regex(@"\W\w\s\d*\W");
             Regex regexSubGroup = new Regex(@"\W\w+\s\d*\W+");
             for(int b=0; b<lessons.Count; b++)
             {
                 if (lessons[b].Number >= 4)
                 {
                     lessons[b].Number = 3;
                     guidsChangeNumber.Add(lessons[b].Id);
                 }
                 else if (lessons[b].Number == 2)
                 {
                     lessons[b].Number = 1;
                     guidsChangeNumber.Add(lessons[b].Id);
                 }
                 
                 MatchCollection matches = regex.Matches(lessons[b].Name);
                 if (matches.Count > 0)
                 {
                     foreach (Match match in matches)
                     {
                         lessons[b].Name = lessons[b].Name.Replace(match.Value, "").Trim();
                     }
                 }
                 
                 MatchCollection matchSubGroup = regexSubGroup.Matches(lessons[b].Name);
                 if (matchSubGroup.Count > 0)
                 {
                     foreach (Match match in matchSubGroup)
                     {
                         string s = "";
                         if (match.Value.Contains("+"))
                         {
                             s = "+";
                         }
                         else if (match.Value.Contains("-"))
                         {
                             s = "-";
                         }
                         else if (match.Value.Contains("*"))
                         {
                             s = "*";
                         }
                         
                         lessons[b].Name = lessons[b].Name.Replace(match.Value, "").Trim();
                         
                         subGroupList.Add(new SubGroupObject() { IdSubGroup = lessons[b].Id, String = s});
                     }
                 }
             }
             
             

             var groupLessonName = lessons.GroupBy(l => l.Name);

             foreach (var ln in groupLessonName)
             {
                 model.NameLessons.Add(ln.Key);

                 var groupOfDay = ln.GroupBy(l => l.Day.DateTime);

                 foreach (var o in groupOfDay)
                 {
                     List<LessonViewModel> lvm = new List<LessonViewModel>()
                     {
                         new LessonViewModel(){Number = 1, String = ""},
                         new LessonViewModel(){Number = 3, String = ""}
                     };
                     
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
                                             bool a = guidsChangeNumber.Contains(les.Id);
                                             
                                             byte number = a
                                                 ? (byte)(les.Number + 1)
                                                 : les.Number;
                                             
                                             lec = $"{s.Letter},{number},{les.NumberAuditor}";
                                             brea = true;
                                         }
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
                             
                             if (les.Day.Week.Schedule.Group == "333")
                             {
                                 name += "";
                             }
                             else
                             {
                                 SubGroupObject obj = subGroupList.FirstOrDefault(a=> a.IdSubGroup == les.Id);
                                 
                                 name += les.Day.Week.Schedule.Group + (obj != null ? obj.String : "") + " ";
                             }
                         }
                         lvm.First(l=>l.Number == k.Key).String = name;
                     }

                     model.Days.First(d => d.DateTime == o.Key).Lessons.Add(lvm);
                 }
             }
             foreach (var a in model.Days)
             {
                 for(int k = 0; k< a.Lessons.Count; k++)
                 { 
                     if (a.Lessons[k].Count < 2)
                     {
                         if(a.Lessons[k].FirstOrDefault(l=> l.Number == 1) == null) a.Lessons[k].Add(new LessonViewModel(){Number=1, String = ""});
                         if(a.Lessons[k].FirstOrDefault(l=> l.Number == 3) == null) a.Lessons[k].Add(new LessonViewModel(){Number=3, String = ""});
                     }

                     
                     a.Lessons[k] = a.Lessons[k].OrderBy(l => l.Number).ToList();
                 }
             }
             
             return Ok(model);
         }
         
         [HttpPost]
         [Route("addFullSchedule")]
         public async Task<IActionResult> AddFullSchedule([FromBody] ScheduleViewModel model, [FromQuery] byte? course,
             [FromQuery] Faculty? faculty, [FromQuery] string lectionInfo)
         {
             if(model == null) return BadRequest("Model is null");
             if(model.Days == null) return BadRequest("Model.Days.Count is null");
             if(faculty == null) return BadRequest("faculty is null");
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
                             bool empty = false;
                             
                             if (group.Trim() == String.Empty) empty=true;
                             
                             if (group.Contains(","))
                             {
                                 string[] info = group.Split(",");
                                 string name = $"{model.NameLessons[a]} (Л {info[2]})";
                                 byte number = Convert.ToByte(info[1]);

                                 LectionInfo inf = lectionInfos.FirstOrDefault(g => g.Letter.ToUpper() == info[0].Trim().ToUpper());

                                 if (inf == null) continue;
                                 foreach (var gr in inf.Groups)
                                 {
                                     Schedule schedule = schedules.FirstOrDefault(s => s.Group == gr);
                                     if (schedule == null)
                                     {
                                         schedule = new Schedule()
                                         {
                                             ScheduleType = ScheduleType.Full,
                                             Group = gr,
                                             Course = (byte)course,
                                             Faculty = GetFaculty(gr),
                                             LectionInfo = lectionInfo
                                         };
                                         
                                         
                                         schedule.Weeks.Add(new Week()
                                         {
                                             WeekType = WeekType.WithDate,
                                             StartDate = model.Days[0].DateTime,
                                             FinishDate = model.Days[0].DateTime.GetValueOrDefault().AddDays(4),
                                             Days = new List<Day>()
                                             {
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault()},
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(1)},
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(2)},
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(3)},
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(4)}
                                             }
                                         });
                                         schedules.Add(schedule);
                                     }

                                     Lesson lesson = new Lesson()
                                     {
                                         Name = name,
                                         Number = number,
                                         LessonType = LessonType.Lection,
                                         NumberAuditor = info[2]
                                     };
                                     
                                     schedule.Weeks.First(w=> w.StartDate == model.Days[0].DateTime.GetValueOrDefault()).Days.First(d=>d.DateTime==day.DateTime.GetValueOrDefault()).Lessons.Add(lesson);    
                                     
                                 }
                                 
                             }
                             else
                             {
                                 string subgroup = string.Empty;

                                 if (group.Contains("+"))
                                 {
                                     subgroup = " (клінічка +)";
                                 }
                                 else if(group.Contains("-"))
                                 {
                                     subgroup = " (клінічка -)";
                                 }
                                 else if(group.Contains("*"))
                                 {
                                     subgroup = " (клінічка *)";
                                 }

                                 string newGroup = group.Replace("+", "").Replace("-", "").Replace("*", "");
                                 
                                 Schedule schedule = schedules.FirstOrDefault(s => s.Group == (empty ? "333":newGroup));
                                 if (schedule == null)
                                 {
                                     schedule = new Schedule()
                                     {
                                         ScheduleType = ScheduleType.Full,
                                         Group = (empty ? "333": GetFaculty(newGroup) != faculty ? "333" : newGroup),
                                         Course = (byte)course,
                                         Faculty = (empty ? (Faculty)faculty : GetFaculty(newGroup)),
                                         LectionInfo = lectionInfo
                                     };
                                     schedule.Weeks.Add(new Week()
                                     {
                                         WeekType = WeekType.WithDate,
                                         StartDate = model.Days[0].DateTime,
                                         FinishDate = model.Days[0].DateTime.GetValueOrDefault().AddDays(4),
                                         Days = new List<Day>()
                                         {
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault()},
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(1)},
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(2)},
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(3)},
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(4)}
                                         }
                                     });
                                     schedules.Add(schedule);
                                 }

                                 Lesson lesson = new Lesson()
                                 {
                                     Name = model.NameLessons[a] + (subgroup != String.Empty ? subgroup : ""),
                                     Number = day.Lessons[a][i].Number,
                                     LessonType = LessonType.Practice,
                                 };
                             
                                 schedule.Weeks.First(w=> w.StartDate == model.Days[0].DateTime.GetValueOrDefault()).Days.First(d=>d.DateTime==day.DateTime.GetValueOrDefault()).Lessons.Add(lesson);    
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
         [Route("editFullSchedule")]
         public async Task<IActionResult> EditFullSchedule([FromBody] ScheduleViewModel model, [FromQuery] byte? course,
             [FromQuery] Faculty? faculty, [FromQuery] string lectionInfo, [FromQuery]DateTime? startDate)
         {
             if(model == null) return BadRequest("Model is null");
             if(model.Days == null) return BadRequest("Model.Days.Count is null");
             if(faculty == null) return BadRequest("faculty is null");
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

             List<Schedule> schedules = await _context.Schedules.Where(s=> s.Course == (byte)course && s.Faculty == (Faculty)faculty && s.Weeks.Where(w=>w.StartDate==startDate).Count() > 0)
                 .Include(s=> s.Weeks.Where(w=>w.StartDate==(DateTime)startDate)).ToListAsync();

             foreach (var schedule in schedules)
             {
                 _context.Weeks.RemoveRange(schedule.Weeks);
             }

             await _context.SaveChangesAsync();
             Console.WriteLine("delete weeks");
             
             foreach (DayViewModel day in model.Days)
             {
                 for(int a =0; a< day.Lessons.Count; a++)
                 {
                     for(int i=0; i< day.Lessons[a].Count; i++)
                     {
                         string[] splitStrings = day.Lessons[a][i].String.Split(" ");

                         foreach (string group in splitStrings)
                         {
                             bool empty = false;
                             
                             if (group.Trim() == String.Empty) empty=true;
                             
                             if (group.Contains(","))
                             {
                                 string[] info = group.Split(",");
                                 string name = $"{model.NameLessons[a]} (Л {info[2]})";
                                 byte number = Convert.ToByte(info[1]);

                                 LectionInfo inf = lectionInfos.FirstOrDefault(g => g.Letter.ToUpper() == info[0].Trim().ToUpper());

                                 if (inf == null) continue;
                                 foreach (var gr in inf.Groups)
                                 {
                                     Schedule schedule = schedules.FirstOrDefault(s => s.Group == gr);
                                     if (schedule == null)
                                     {
                                         schedule = new Schedule()
                                         {
                                             Id= Guid.NewGuid(),
                                             ScheduleType = ScheduleType.Full,
                                             Group = gr,
                                             Course = (byte)course,
                                             Faculty = GetFaculty(gr),
                                             LectionInfo = lectionInfo,
                                         };
                                         schedule = _context.Schedules.Add(schedule).Entity;
                                         
                                         schedules.Add(schedule);
                                     }

                                     if (schedule.Weeks.FirstOrDefault(w => w.StartDate == startDate) == null)
                                     {
                                         schedule.Weeks.Add(new Week()
                                         {
                                             Id = Guid.NewGuid(),
                                             WeekType = WeekType.WithDate,
                                             StartDate = model.Days[0].DateTime,
                                             FinishDate = model.Days[0].DateTime.GetValueOrDefault().AddDays(4),
                                             Days = new List<Day>()
                                             {
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault()},
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(1)},
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(2)},
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(3)},
                                                 new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(4)}
                                             },
                                             ScheduleId = schedule.Id
                                         });
                                     }
                                     
                                     Lesson lesson = new Lesson()
                                     {
                                         Name = name,
                                         Number = number,
                                         LessonType = LessonType.Lection,
                                         NumberAuditor = info[2]
                                     };
                                     
                                     schedule.Weeks.First(w=> w.StartDate == model.Days[0].DateTime.GetValueOrDefault()).Days.First(d=>d.DateTime==day.DateTime.GetValueOrDefault()).Lessons.Add(lesson);    
                                     
                                 }
                                 
                             }
                             else
                             {
                                 string subgroup = string.Empty;

                                 if (group.Contains("+"))
                                 {
                                     subgroup = " (клінічка +)";
                                 }
                                 else if(group.Contains("-"))
                                 {
                                     subgroup = " (клінічка -)";
                                 }
                                 else if(group.Contains("*"))
                                 {
                                     subgroup = " (клінічка *)";
                                 }

                                 string newGroup = group.Replace("+", "").Replace("-", "").Replace("*", "");
                                 
                                 Schedule schedule = schedules.FirstOrDefault(s => s.Group == (empty ? "333":newGroup));
                                 if (schedule == null)
                                 {
                                     schedule = new Schedule()
                                     {
                                         Id= Guid.NewGuid(),
                                         ScheduleType = ScheduleType.Full,
                                         Group = (empty ? "333": GetFaculty(newGroup) != faculty ? "333" : newGroup),
                                         Course = (byte)course,
                                         Faculty = (empty ? (Faculty)faculty : GetFaculty(newGroup)),
                                         LectionInfo = lectionInfo
                                     };
                                     schedule = _context.Schedules.Add(schedule).Entity;

                                     Console.WriteLine("schedule add id:"+schedule.Id);
                                     
                                     schedules.Add(schedule);
                                 }
                                 Console.WriteLine("if null");
                                 if (schedule.Weeks.FirstOrDefault(w => w.StartDate == startDate) == null)
                                 {
                                     schedule.Weeks.Add(new Week()
                                     {
                                         Id = Guid.NewGuid(),
                                         WeekType = WeekType.WithDate,
                                         StartDate = model.Days[0].DateTime,
                                         FinishDate = model.Days[0].DateTime.GetValueOrDefault().AddDays(4),
                                         Days = new List<Day>()
                                         {
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault()},
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(1)},
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(2)},
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(3)},
                                             new Day(){DateTime = day.DateTime.GetValueOrDefault().AddDays(4)}
                                         },
                                         ScheduleId = schedule.Id
                                     });
                                     
                                     Console.WriteLine("after if");
                                 }

                                 Console.WriteLine("lesson");
                                 Lesson lesson = new Lesson()
                                 {
                                     Name = model.NameLessons[a] + (subgroup != String.Empty ? subgroup : ""),
                                     Number = day.Lessons[a][i].Number,
                                     LessonType = LessonType.Practice,
                                 };
                             
                                 schedule.Weeks.First(w=> w.StartDate == model.Days[0].DateTime.GetValueOrDefault()).Days.First(d=>d.DateTime==day.DateTime.GetValueOrDefault()).Lessons.Add(lesson);    
                             }
                         }
                     }
                 }
             }


             foreach (var s in schedules)
             {
                 _context.Weeks.Add(s.Weeks.First(w => w.StartDate == startDate.GetValueOrDefault()));
             }
             await _context.SaveChangesAsync();
             
             return Ok(schedules);
         }
         
         
         [HttpDelete]
         public async Task<IActionResult> Delete([FromQuery]List<Guid> id)
         {
             if (id == null) return BadRequest("id == null");
            
             List<Guid> delId = new List<Guid>();

             foreach (Guid i in id)
             {
                 Schedule f = await _context.Schedules.FirstOrDefaultAsync(l=> l.Id==i);

                 if (f != null)
                 {
                     delId.Add(f.Id);
                     _context.Schedules.Remove(f);
                 }

             }

             await _context.SaveChangesAsync();
            
             return Ok(delId);
         }
         
         [HttpGet]
         [Route("getAllSchedules")]
         public async Task<IActionResult> GetAllWeeks()
         {
             List<Schedule> schedules = await _context.Schedules.ToListAsync();

             if(schedules == null) return NotFound();

             return Ok(schedules);
         }
         
         [NonAction]
         public static Faculty GetFaculty(string group)
         {
             switch (group)
             {
                 case "1":
                 case "2":
                 case "3":
                 case "4":
                 case "5":
                 case "6":
                 case "7":
                 case "8":
                 case "9":
                 case "10":
                 case "10а":
                 case "10a":
                 case "10б": return Faculty.Medicine;
                 
                 case "81":
                 case "82":
                 case "83":
                 case "84": return Faculty.ShortMedicine;
                 
                 case "31":
                 case "32":
                 case "33" : return Faculty.Pediatric;
                 
                 case "11":
                 case "12":
                 case "13":
                 case "14" : return Faculty.Stomatology;
                 
                 case "18":
                 case "19": return Faculty.ShortStomatology;
                 
                 case "41":
                 case "42": return Faculty.Farmacy;
                 
                 case "49": return Faculty.ShortFarmacy;
                 
                 case "91":
                 case "92":
                 case "93":
                 case "94": return Faculty.Reabilitology;
                 
                 case "30":
                 case "30a":
                 case "30а": return Faculty.Paramedic;
                 
                 case "23":
                 case "24":
                 case "25":
                 case "26": return Faculty.Nurse;
                 
                 case "47":
                 case "47a":
                 case "47а": return Faculty.Farmacy9;
                 
                 case "21":
                 case "21а":
                 case "21a": return Faculty.Ortopedic;
                 
                 case "22": return Faculty.Ortopedic11;


                 default: return Faculty.Foreigners;
             }
         }

         private class SubGroupObject
         {
             public Guid IdSubGroup { get; set; }
             public string String { get; set; }
         }
     }
 }
