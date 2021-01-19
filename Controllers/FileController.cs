using System;
using System.IO;
using System.Threading.Tasks;
using IFNMU_API_NORM.Models;
using IFNMU_API_NORM.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace IFNMU_API_NORM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public FileController(DatabaseContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("getFile/{id}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            FileInformation file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);

            if (file == null) return BadRequest("Файл не знайдено");
                
            return File(file.Path, "application/octet-stream");
        } 
        
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] FileViewModel model, bool? isSubDir)
        {
            if(isSubDir==null) return BadRequest("isSubDir is required");

            List<FileInformation> list = new List<FileInformation>();
            string message = "";

            foreach(IFormFile file in model.FormFiles)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", model.DirectoryId.ToString(), file.FileName);
                
                string pathWith = Path.Combine(model.DirectoryId.ToString(), file.FileName);

                if (System.IO.File.Exists(path))
                {
                    message += $" {file.FileName} ";
                    continue;
                }

                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                FileInformation fileInformation = new FileInformation()
                {
                    Name = file.FileName,
                    Path = pathWith
                };
                if((bool)isSubDir) fileInformation.SubDirectoryId = model.DirectoryId;
                else fileInformation.DirectoryId = model.DirectoryId;
                
                var f = _context.Files.Add(fileInformation);

                list.Add(f.Entity);
            }

            if (message != "")
            {
                message = $"Файли: | {message} | вже існують на сервері. Перейменуйте їх";
            }

            await _context.SaveChangesAsync();

            return Ok(new {list, message});
        }

        [HttpGet("getAllFiles")]
        public async Task<IActionResult> GetAllFiles()
        {
            return Ok(await _context.Files.ToListAsync());
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery]List<Guid> id)
        {
            if (id == null) return BadRequest("id == null");
            
            List<Guid> delId = new List<Guid>();

            foreach (Guid i in id)
            {
                FileInformation f = await _context.Files.FirstOrDefaultAsync(l=> l.Id==i);

                if (f != null)
                {
                    delId.Add(f.Id);
                    _context.Files.Remove(f);
                    
                    string path = f.Path;
                    FileInfo fileInf = new FileInfo(path);
                    if (fileInf.Exists)
                    {
                        fileInf.Delete();
                    }
                }

            }

            await _context.SaveChangesAsync();
            
            return Ok(delId);
        }
    }
}