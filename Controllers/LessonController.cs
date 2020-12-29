using System.Threading.Tasks;
using IFNMU_API_NORM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFNMU_API_NORM.Controllers
{
    public class LessonController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public LessonController(DatabaseContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("getLessons")]
        public async Task<IActionResult> Get(byte course, string group)
        {
            Schedule schedule = await _context.Schedules.Include(s=>s.Weeks)
                .ThenInclude(w=>w.Days)
                .ThenInclude(d=>d.Lessons)
                .FirstOrDefaultAsync(s => s.Course == course && s.Group == group);

            return Ok(schedule);
        }
    }
}