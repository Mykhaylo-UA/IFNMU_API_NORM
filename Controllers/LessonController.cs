using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFNMU_API_NORM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFNMU_API_NORM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public LessonController(DatabaseContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("getLessons")]
        public async Task<IActionResult> Get(byte course, string group, byte? weekNumber, DateTime? startDate)
        {
            Schedule schedule = await _context.Schedules.Include(s=>s.Weeks)
                .ThenInclude(w=>w.Days)
                .ThenInclude(d=>d.Lessons)
                .FirstOrDefaultAsync(s => s.Course == course && s.Group == group);

            if (schedule == null) return NotFound();

            if (weekNumber != null)
            {
                Week week = schedule.Weeks.FirstOrDefault(w => w.WeekNumber == weekNumber);
                
                schedule.Weeks = new List<Week>()
                {
                    week
                };
                
                schedule.Weeks[0].Days = schedule.Weeks[0].Days.OrderBy(d => d.DayOfWeek).ToList();

                for (byte i =0; i < schedule.Weeks[0].Days.Count; i++)
                {
                    schedule.Weeks[0].Days[i].Lessons = schedule.Weeks[0].Days[i].Lessons.OrderBy(l => l.Number).ToList();
                }
            }
            else if (startDate != null)
            {
                Week week = schedule.Weeks.FirstOrDefault(w => w.StartDate == startDate);
                
                schedule.Weeks = new List<Week>()
                {
                    week
                };

                schedule.Weeks[0].Days = schedule.Weeks[0].Days.OrderBy(d => d.DateTime).ToList();

                for (byte i =0; i < schedule.Weeks[0].Days.Count; i++)
                {
                    schedule.Weeks[0].Days[i].Lessons = schedule.Weeks[0].Days[i].Lessons.OrderBy(l => l.Number).ToList();
                }
            }
            else
            {
                return BadRequest("weekNumber or startDate == null");
            }
            
            return Ok(schedule);
        }

        [HttpGet]
        [Route("getWeeks")]
        public async Task<IActionResult> GetWeeks(byte course, string group)
        {
            Schedule schedule = await _context.Schedules.Include(s=>s.Weeks)
                .FirstOrDefaultAsync(s => s.Course == course && s.Group == group);

            if(schedule == null) return NotFound();

            List<string> weeksType = new List<string>();

            foreach (var week in schedule.Weeks)
            {
                if (schedule.ScheduleType == ScheduleType.Full)
                {
                    string s = week.StartDate.GetValueOrDefault().ToString("yyyy-MM-dd");
                    weeksType.Add(s);
                }
                else
                {
                    string s = week.WeekNumber.ToString();
                    weeksType.Add(s);
                }
            }

            return Ok(weeksType);
        }
        
        [HttpGet]
        [Route("getAllWeeks")]
        public async Task<IActionResult> GetAllWeeks()
        {
            List<Week> weeks = await _context.Weeks.ToListAsync();

            if(weeks == null) return NotFound();

            return Ok(weeks);
        }
        
        [HttpDelete]
        [Route("deleteWeeks")]
        public async Task<IActionResult> Delete([FromQuery]List<Guid> id)
        {
            if (id == null) return BadRequest("id == null");
            
            List<Guid> delId = new List<Guid>();

            foreach (Guid i in id)
            {
                Week f = await _context.Weeks.FirstOrDefaultAsync(l=> l.Id==i);

                if (f != null)
                {
                    delId.Add(f.Id);
                    _context.Weeks.Remove(f);
                }

            }

            await _context.SaveChangesAsync();
            
            return Ok(delId);
        }
    }
}