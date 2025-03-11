using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class EPubsController : BaseController
  {
    private readonly WakeDownloader _wakeDownloader;
    private readonly CaptionQueries _captionQueries;

    public EPubsController(WakeDownloader wakeDownloader,
        CTDbContext context,
        CaptionQueries captionQueries,
        ILogger<EPubsController> logger) : base(context, logger)
    {
      _captionQueries = captionQueries;
      _wakeDownloader = wakeDownloader;
    }

    public class EPubSceneData
    {
      public string Image { get; set; }
      public string Text { get; set; }
      public TimeSpan Start { get; set; }
      public TimeSpan End { get; set; }
      // public string OCRPhrases { get; set; }
      // public string OCRText { get; set; }
      public List<string> OCRElements { get; set; }
      public string Title { get; set; }
    }

    [NonAction]
    public List<EPubSceneData> GetSceneData(JArray scenes, List<Caption> captions, List<Caption> descriptions)
    {
      var sorted_descriptions = descriptions.OrderBy(c => c.Begin);
      // _logger.LogInformation($"GetSceneData(), {string.Join(", ", sorted_descriptions.Select(d => d.Text))}");
      var chapters = new List<EPubSceneData>();
      var nextStart = new TimeSpan(0);

      if (scenes == null)
      {
        return chapters;
      }
      foreach (Caption description in sorted_descriptions)
      {
        var endTime = description.End;
        var subset = captions.Where(c => c.Begin < endTime && c.Begin >= nextStart).ToList();
        if (description == sorted_descriptions.Last())
        {
          subset = captions.Where(c => c.Begin >= nextStart).ToList();
        }
        TimeSpan GetSceneTimestamp(JToken scene)
        {
          return TimeSpan.Parse(scene["start"].ToString());
        }
        JObject closestScene = (JObject)scenes.Where(s => GetSceneTimestamp(s) < description.End &&
                                             GetSceneTimestamp(s) > description.Begin).
                                             FirstOrDefault();

        if (closestScene == null)
        {
          closestScene = (JObject)scenes.OrderBy(s => Math.Abs((GetSceneTimestamp(s) - description.Begin).Ticks)).FirstOrDefault();
        }

        StringBuilder sb = new StringBuilder();
        subset.ForEach(c => sb.Append(c.Text + " "));
        var img_descriptions = closestScene["phrases"]?.Select(p => p.ToString()).ToList();
        img_descriptions.Insert(0, description.Text);
        chapters.Add(new EPubSceneData
        {
          Image = closestScene["img_file"].ToString(),
          Start = nextStart,
          End = endTime,
          Text = sb.ToString(),
          // OCRText = description.Text,
          // OCRPhrases = closestScene["phrases"]?.ToString(),
          // OCRPhrases = description.Text,
          OCRElements = img_descriptions,
          Title = closestScene["title"]?.ToString()
        });

        nextStart = endTime;
      }
      return chapters;
    }

    /// <summary>
    /// Gets captions and images for a given video
    /// </summary>
    /// 
    [HttpGet("GetEpubData")]
    [Authorize]
    public async Task<ActionResult<List<EPubSceneData>>> GetEpubData(string mediaId, string language)
    {
      _logger.LogInformation($"GetEpubData({mediaId},{language}) starting");
      var media = _context.Medias.Find(mediaId);
      Video video = await _context.Videos.FindAsync(media.VideoId);
      // _logger.LogInformation($"GetEpubData({mediaId},{language}) video found. SceneData:{video.SceneObjectDataId}.");

      if (!video.HasSceneObjectData())
      {
        _logger.LogInformation($"GetEpubData({mediaId}) - Early return - no SceneObjectData");
        return NotFound();
      }
      TextData data = await _context.TextData.FindAsync(video.SceneObjectDataId);
      // _logger.LogInformation($"GetEpubData({mediaId},{language}) getting scenedata as JArray");
      JArray sceneArray = data.GetAsJSON()["Scenes"] as JArray;

      EPub epub = new EPub
      {
        Language = language,
        SourceType = ResourceType.Media,
        SourceId = mediaId
      };
      const string SOURCEINTERNALREF = "ClassTranscribe/Local"; // This is a key inside the database to
                                                                // indicate the source of the captions
      var captions = await _captionQueries.GetCaptionsAsync(media.VideoId, SOURCEINTERNALREF, epub.Language);
      if (captions.Count == 0)
      {
        const string LEGACYSOURCEINTERNALREF = "ClassTranscribe/Azure"; // We should only ask for captions from this
                                                                        // source if the other has no entries
        captions = await _captionQueries.GetCaptionsAsync(media.VideoId, SOURCEINTERNALREF, epub.Language);
      }
      var descriptions = await _captionQueries.GetDescriptionsAsync(media.VideoId, epub.Language);
      // _logger.LogInformation($"GetEpubData({mediaId}) - returning combined SceneData");

      var sd = GetSceneData(sceneArray, captions, descriptions);
      // _logger.LogInformation($"GetEpubData returned scenes: ({string.Join("|", sd.Select(s => s.Text))})");
      return sd;

    }

    /// <summary>
    /// Gets glossary for a given video
    /// </summary>
    /// 
    [HttpGet("GetGlossaryData")]
    [Authorize]
    public async Task<ActionResult<Object>> GetGlossaryData(string mediaId)
    {
      var media = _context.Medias.Find(mediaId);
      Video video = await _context.Videos.FindAsync(media.VideoId);
      if (video.HasGlossaryData())
      {
        TextData data = await _context.TextData.FindAsync(video.GlossaryDataId);
        return data.GetAsJSON();
      }
      return video.Glossary;

    }

    [HttpGet("RequestEpubCreation")]
    [Authorize]
    public ActionResult RequestEpubCreation(string mediaId)
    {
      _wakeDownloader.GenerateScenes(mediaId);
      return Ok();
    }

    // GET: api/EPubs/5
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<EPub>> GetEPub(string id)
    {
      var ePub = await _context.EPubs.FindAsync(id);

      if (ePub == null)
      {
        return NotFound();
      }
      _logger.LogInformation($"!GetEpub, epub string {string.Join("||", ePub.Chapters)}");

      return ePub;
    }

    // GET: api/EPubs/ByOwner/{userid}
    [HttpGet("ByOwner/{UserId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<EPub>>> GetEPubs(string userId = "")
    {
      try
      {

        var ePubs = await _context.EPubs.ToListAsync();

        if (!ePubs.Any())
        {
          return NotFound();
        }

        ePubs.ForEach(ePub =>
        {
          ePub.Chapters = null;
        });

        return ePubs;
      }
      catch (ArgumentException)
      {
        return BadRequest($"Invalid request to /api/EPubs/ByOwner/{userId}");
      }
    }

    // GET: api/EPubs/BySource/{sourceType}/{sourceId}
    [HttpGet("BySource/{sourceType}/{sourceId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<EPub>>> GetEPubsBySource(string sourceType, string sourceId)
    {
      try
      {
        ResourceType type = (ResourceType)Enum.Parse(typeof(ResourceType), sourceType);

        var ePubs = await _context.EPubs.Where(i => i.SourceType == type && i.SourceId == sourceId).ToListAsync();

        if (!ePubs.Any())
        {
          return NotFound();
        }

        ePubs.ForEach(ePub =>
        {
          ePub.Chapters = null;
        });

        return ePubs;
      }
      catch (ArgumentException)
      {
        return BadRequest($"{sourceType} is not a valid resource type");
      }
    }

    // PUT: api/EPubs/5
    [HttpPut("{id}")]
    [DisableRequestSizeLimit]
    [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
    public async Task<IActionResult> PutEPub(string id, EPub ePub)
    {
      if (ePub == null || id != ePub.Id)
      {
        return BadRequest();
      }

      if (string.IsNullOrEmpty(ePub.Title) ||
          string.IsNullOrEmpty(ePub.Filename) ||
          string.IsNullOrEmpty(ePub.Language) ||
          string.IsNullOrEmpty(ePub.Author) ||
          string.IsNullOrEmpty(ePub.Publisher) ||
          string.IsNullOrEmpty(ePub.SourceId))
      {
        return BadRequest("The following fields may not be empty: title, filename, language, author, publisher, sourceId");
      }
      _logger.LogInformation($"!PutEpub, epub string {string.Join("||", ePub.Chapters)}");

      _context.Entry(ePub).State = EntityState.Modified;

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!_context.EPubs.Any(e => e.Id == id))
        {
          return NotFound();
        }
        else
        {
          throw;
        }
      }

      return NoContent();
    }

    // POST: api/EPubs
    [HttpPost]
    [DisableRequestSizeLimit]
    [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
    public async Task<ActionResult<EPub>> PostEPub(EPub ePub)
    {
      if (ePub == null)
      {
        return BadRequest();
      }

      if (string.IsNullOrEmpty(ePub.Title) ||
          string.IsNullOrEmpty(ePub.Filename) ||
          string.IsNullOrEmpty(ePub.Language) ||
          string.IsNullOrEmpty(ePub.Author) ||
          string.IsNullOrEmpty(ePub.Publisher) ||
          string.IsNullOrEmpty(ePub.SourceId))
      {
        return BadRequest("The following fields may not be empty: title, filename, language, author, publisher, sourceId");
      }

      _logger.LogInformation($"!PostEpub, epub string {string.Join("||", ePub.Chapters)}");

      _context.EPubs.Add(ePub);
      await _context.SaveChangesAsync();

      return CreatedAtAction("GetEPub", new { id = ePub.Id }, ePub);
    }

    // DELETE: api/EPubs/5
    [HttpDelete("{id}")]
    [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
    public async Task<ActionResult<EPub>> DeleteEPub(string id)
    {
      var ePub = await _context.EPubs.FindAsync(id);

      if (ePub == null)
      {
        return NotFound();
      }

      _context.EPubs.Remove(ePub);
      await _context.SaveChangesAsync();

      return ePub;
    }
  }
}