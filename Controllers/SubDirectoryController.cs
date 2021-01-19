using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IFNMU_API_NORM.Models;
using IFNMU_API_NORM.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFNMU_API_NORM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubDirectoryController : Controller
    {
        private readonly DatabaseContext _context;

        public SubDirectoryController(DatabaseContext context)
        {
            _context = context;
        }

        [HttpGet("subdirectory/{id}")]
        public async Task<IActionResult> GetById(Guid? id)
        {
            if (id == null) return BadRequest("id == null");
        
            SubDirectory d = await _context.SubDirectories.Include(l=> l.Files)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (d == null) return NotFound();

            return Ok(d);
        }

        [HttpPost]
        [Route("addSubDirectory")]
        public async Task<IActionResult> PostSubDirectory([FromBody]SubDirectoryViewModel model)
        {
            SubDirectory k = await _context.SubDirectories
                .FirstOrDefaultAsync(l=> l.Name == model.Name && l.DirectoryInformationId == model.DirectoryId);

            if (k != null) return BadRequest("Папка з такою назвою уже існує");
            
            SubDirectory directory = new SubDirectory()
            {
                Name = model.Name,
                DirectoryInformationId = model.DirectoryId
            };

            var d = _context.SubDirectories.Add(directory);
            await _context.SaveChangesAsync();
                
            var pathDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", d.Entity.Id.ToString());
                
            if(!Directory.Exists(pathDir))
            {
                Directory.CreateDirectory(pathDir);
            }
            
            return Ok(d.Entity);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery]List<Guid> id)
        {
            if (id == null) return BadRequest("id == null");
            
            List<Guid> delId = new List<Guid>();

            foreach (Guid i in id)
            {
                SubDirectory f = await _context.SubDirectories.FirstOrDefaultAsync(l=> l.Id==i);

                if (f != null)
                {
                    delId.Add(f.Id);
                    _context.SubDirectories.Remove(f);
                    
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