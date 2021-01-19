using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IFNMU_API_NORM.Models;
using IFNMU_API_NORM.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFNMU_API_NORM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LinkController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public LinkController(DatabaseContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _context.Links.ToListAsync());
        }
        
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]LinkViewModel model)
        {
            if (model == null) return BadRequest();
            if (model.Name == "") return BadRequest("Name is empty");
            if (model.Path == "") return BadRequest("Path is empty");
            if (model.DirectoryId == Guid.Empty) return BadRequest("DirectoryId is empty");
            
            Link link = new Link()
            {
              Name  = model.Name,
              Path = model.Path,
              DirectoryInformationId = model.DirectoryId
            };
            
            var d = _context.Links.Add(link);
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
                Link f = await _context.Links.FirstOrDefaultAsync(l=> l.Id==i);

                if (f != null)
                {
                    delId.Add(f.Id);
                    _context.Links.Remove(f);
                }

            }

            await _context.SaveChangesAsync();
            
            return Ok(delId);
        }
    }
}