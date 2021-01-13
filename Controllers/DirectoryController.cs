using System;
using System.Threading.Tasks;
using IFNMU_API_NORM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IFNMU_API_NORM.ViewModels;

namespace IFNMU_API_NORM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DirectoryController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public DirectoryController(DatabaseContext context)
        {
            _context = context;
        }

        [HttpGet("directory/{id}")]
        public async Task<IActionResult> GetById(Guid? id)
        {
            if (id == null) return BadRequest("id == null");
        
            DirectoryInformation d = await _context.Directory.Include(l=> l.Files).FirstOrDefaultAsync(l => l.Id == id);

            if (d == null) return NotFound();

            return Ok(d);
        }
        
        [HttpGet("directory/{course}/{name}/{faculty}")]
        public async Task<IActionResult> GetByCourseAndName(byte? course, string name, Faculty? faculty)
        {
            if (course == null) return BadRequest("course == null");
            if (name == null || name==String.Empty) return BadRequest("name == null");
            if (faculty == null) return BadRequest("faculty == null");
            
            Regex regex = new Regex(@"\W\w\s\d*\W");
            MatchCollection matches = regex.Matches(name);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    name = name.Replace(match.Value, "").Trim();
                }
            }
            
            DirectoryInformation d = await _context.Directory.Include(l=> l.Files)
                .FirstOrDefaultAsync(l => l.Course == course && l.NameLesson.Contains(name) && l.Faculty == faculty);

            if (d == null) return NotFound();

            d.Files = d.Files.OrderBy(f => f.Name).ToList();

            return Ok(d);
        }

        [HttpGet("directories")]
        public async Task<IActionResult> GetById()
        {
            return Ok(await _context.Directory.Include(d=> d.Files).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]DirectoryViewModel model)
        {
            DirectoryInformation k = await _context.Directory
                .FirstOrDefaultAsync(l => l.Course == model.Course && l.NameLesson == model.NameLesson);

            if (k != null) return BadRequest("Папка з такою назвою, курсом і факультетом вже існує");
            
            DirectoryInformation directory = new DirectoryInformation()
            {
                Name = model.NameLesson,
                Course = model.Course,
                NameLesson = model.NameLesson,
                Faculty =  model.Faculty
            };

            var d = _context.Directory.Add(directory);
            await _context.SaveChangesAsync();
                
            return Ok(d.Entity);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery]List<Guid> id)
        {
            if (id == null) return BadRequest("id == null");
            
            List<Guid> delId = new List<Guid>();

            foreach (Guid i in id)
            {
                DirectoryInformation f = await _context.Directory.FirstOrDefaultAsync(l=> l.Id==i);

                if (f != null)
                {
                    delId.Add(f.Id);
                    _context.Directory.Remove(f);
                    
                    var pathDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", f.Id.ToString());
                
                    if(Directory.Exists(pathDir))
                    {
                        Directory.Delete(pathDir, true);
                    }
                }

            }

            await _context.SaveChangesAsync();
            
            return Ok(delId);
        }
    }
}