using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : BaseController
    {
        public ImagesController(CTDbContext context, ILogger<ImagesController> logger) : base(context, logger) {}

        // GET: api/Images/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Image>> GetImage(string id)
        {
            var image = await _context.Images.FindAsync(id);

            if (image == null)
            {
                return NotFound();
            }

            return image;
        }

        // GET: api/Images/BySource/{sourceType}/{sourceId}
        [HttpGet("BySource/{sourceType}/{sourceId}")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
        public async Task<ActionResult<IEnumerable<Image>>> GetImagesBySource(string sourceType, string sourceId)
        {
            try
            {
                ResourceType type = (ResourceType)Enum.Parse(typeof(ResourceType), sourceType);

                var images = await _context.Images.Where(i => i.SourceType == type && i.SourceId == sourceId).ToListAsync();

                if (!images.Any())
                {
                    return NotFound();
                }

                return images;
            }
            catch (ArgumentException)
            {
                return BadRequest($"{sourceType} is not a valid resource type");
            }
        }

        // POST: api/Images
        [DisableRequestSizeLimit]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Image>> PostImage(IFormFile imageFile, [FromForm] string sourceType, [FromForm] string sourceId)
        {
            if (imageFile == null || imageFile.Length <= 0)
            {
                return BadRequest("Image file is compulsory");
            }

            if (sourceType == null || sourceId == null)
            {
                return BadRequest("Must include valid sourceType and sourceId");
            }

            try
            {
                ResourceType type = (ResourceType)Enum.Parse(typeof(ResourceType), sourceType);

                Image image = new Image
                {
                    SourceType = type,
                    SourceId = sourceId
                };

                // full path to file in temp location
                string extension = Path.GetExtension(imageFile.FileName).ToLower(System.Globalization.CultureInfo.CurrentCulture);
                var allowedExtensions = new string[] { ".png", ".jpg" };

                if (! allowedExtensions.Contains(extension))
                {
                    return BadRequest($"File format not permitted, only {string.Join(',', allowedExtensions)} files are accepted");
                }

                var filePath = CommonUtils.GetTmpFile();
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                var sourceEntity = await GetSourceEntity(type, sourceId);
                var subdir = CommonUtils.ToCourseOfferingSubDirectory(sourceEntity);
                image.ImageFile = await FileRecord.GetNewFileRecordAsync(filePath, extension, subdir);

                _context.Images.Add(image);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetImage", new { id = image.Id }, image);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, $"PostImage {imageFile.FileName}");
                return BadRequest($"{sourceType} is not a valid resource type");
            }
        }

        // DELETE: api/Images/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
        public async Task<ActionResult<Image>> DeleteImage(string id)
        {
            var image = await _context.Images.FindAsync(id);

            if (image == null)
            {
                return NotFound();
            }

            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

            return image;
        }

        private async Task<Entity> GetSourceEntity(ResourceType sourceType, string sourceId)
        {
            switch (sourceType)
            {
                case ResourceType.Course:
                    return await _context.Courses.FindAsync(sourceId);

                case ResourceType.Media:
                    return await _context.Medias.FindAsync(sourceId);

                case ResourceType.Offering:
                    return await _context.Offerings.FindAsync(sourceId);

                case ResourceType.Playlist:
                    return await _context.Playlists.FindAsync(sourceId);

                case ResourceType.EPub:
                    var ePub = await _context.EPubs.FindAsync(sourceId);
                    return ePub != null ? await GetSourceEntity(ePub.SourceType, ePub.SourceId) : null;

                default:
                    return null;
            }
        }
    }
}