﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;
        private readonly IAuthorizationService _authorizationService;
        private readonly UserUtils _userUtils;

        public MediaController(IAuthorizationService authorizationService, 
            WakeDownloader wakeDownloader, 
            CTDbContext context,
            UserUtils userUtils,
            ILogger<MediaController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _wakeDownloader = wakeDownloader;
            _userUtils = userUtils;
        }

        // GET: api/Media/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MediaDTO>> GetMedia(string id)
        {
            var media = await _context.Medias.
                Include(m => m.Video).ThenInclude(v => v.Transcriptions).
                Where(m => m.Id == id).FirstOrDefaultAsync();

            if (media == null)
            {
                return NotFound();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(User, media.Playlist.Offering, Globals.POLICY_READ_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            var playlist = await _context.Playlists.FindAsync(media.PlaylistId);
            //unused var v = await _context.Videos.FindAsync(media.VideoId);
            var user = await _userUtils.GetUser(User);
            var restrict = (bool?) playlist.GetOptionsAsJson()[ "restrictRoomStream"] ?? false;
            var mediaDTO = new MediaDTO
            {
                Id = media.Id,
                Name = media.Name,
                PlaylistId = media.PlaylistId,
                CreatedAt = media.CreatedAt,
                JsonMetadata = media.JsonMetadata,
                SourceType = media.SourceType,
                Duration = media.Video.Duration,
                PublishStatus = media.PublishStatus,
                Options = media.GetOptionsAsJson(),
                Transcriptions = media.Video.Transcriptions
                .Select(t => new TranscriptionDTO
                {
                    Id = t.Id,
                    Path = t.File != null ? t.File.Path : null,
                    Language = t.Language,
                    TranscriptionType = (int) t.TranscriptionType,
                    Label = String.IsNullOrWhiteSpace( t.Label ) ? "" : t.Label
                }).ToList(),
                Video = new VideoDTO
                {
                    Id = media.Video.Id, 
                    Video1Path = media.Video.ProcessedVideo1?.Path != null ? media.Video.ProcessedVideo1.Path : media.Video.Video1?.Path,
                    Video2Path = restrict ? null: media.Video.ProcessedVideo2?.Path != null ? media.Video.ProcessedVideo2.Path : media.Video.Video2?.Path,
                    ASLPath = media.Video.ASLVideo?.Path,
                    TaskLog = media.Video.TaskLog
                },
                WatchHistory = user != null ? media.WatchHistories.Where(w => w.ApplicationUserId == user.Id).FirstOrDefault() : null
            };

            return mediaDTO;
        }

        [HttpPut("PutMediaName")]
        public async Task<IActionResult> PutMediaName(string mediaId, string name)
        {
            if (!MediaExists(mediaId))
            {
                return NotFound();
            }

            Media media = await _context.Medias.FindAsync(mediaId);
            media.Name = name;
            _context.Entry(media).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MediaExists(mediaId))
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

        // PUT: api/Media/5
        [HttpPut("PutJsonMetaData/{id}")]
        [Authorize]
        public async Task<IActionResult> PutJsonMetaData(JObject jsonMetaData, string id)
        {
            if (!MediaExists(id))
            {
                return NotFound();
            }

            Media media = await _context.Medias.FindAsync(id);
            media.JsonMetadata = jsonMetaData ?? new JObject();
            _context.Entry(media).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MediaExists(id))
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

// PUT: api/Media/5
        [HttpPut("Option/{id}/{option}/{type}/{value}")]
        [Authorize]
        public async Task<IActionResult> PutMediaOption(string id, string option, string type,string value)
        {
            if(id == null) return BadRequest("id not specified");
            if(type == null || ! "|int|bool|string|".Contains($"|{type}|")) {
                return BadRequest("type must be int|bool|string");
            }
            if(value == null) return BadRequest("value not specified");

            Media media = await _context.Medias.FindAsync(id);
            JObject theOptions = media.GetOptionsAsJson();
            if (media == null)
            {
                return NotFound();
            }
            if(type == "int") {
                try {
                    theOptions[option] = Int32.Parse(value);
                } catch (FormatException) {
                    return BadRequest($"Unable to parse '{value}' as int");
                }
            } else if(type == "bool") {
                try {
                    theOptions[option] = Boolean.Parse(value);
                } catch (FormatException) {
                    return BadRequest($"Unable to parse '{value}' as bool");
                }
            } else if(type == "string") {
                    theOptions[option] = value;
            } else {
                return BadRequest("Invalid type");// should never happen
            }
            media.SetOptionsAsJson(theOptions);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/ASLVideo
        [DisableRequestSizeLimit]
        [HttpPost("ASLVideo")]
        [Authorize]
        
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Media>> PostASLVideo(IFormFile aSLVideo, [FromForm] string mediaId)
        {
            
            

            if (aSLVideo == null || aSLVideo.Length == 0)
            {
                return BadRequest("ASL Video is compulsory");
            }

            if (Path.GetExtension(aSLVideo.FileName) != ".mp4")
            {
                return BadRequest("File format not permitted");
            }

            var media = await _context.Medias.FindAsync(mediaId);
            if (media == null)
            {
                return NotFound("No such media");
            }
            var video = await _context.Videos.FindAsync(media.VideoId);
            if (video == null)
            {
                return NotFound("No video");
            }

            var filePath = CommonUtils.GetTmpFile();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await aSLVideo.CopyToAsync(stream);
            }

            var subdir = CommonUtils.ToCourseOfferingSubDirectory(_context, media);

            var filerecord = await FileRecord.GetNewFileRecordAsync(filePath, Path.GetExtension(filePath), subdir);
            await _context.FileRecords.AddAsync(filerecord);
            await _context.SaveChangesAsync();
            video.ASLVideoId = filerecord.Id;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Media/ASLVideo/{mediaId}

        [HttpDelete("ASLVideo/{mediaId}")]
       
        [Authorize]
        public async Task<ActionResult<Media>> DeleteASLVideo(string mediaId)
        {

            var media = await _context.Medias.FindAsync(mediaId);
            if (media == null)
            {
                return NotFound("No such media");
            }
            var video = await _context.Videos.FindAsync(media.VideoId);
            if (video == null)
            {
                return NotFound("No video");
            }

            video.ASLVideoId = null; ;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: api/Media
        [DisableRequestSizeLimit]
        [HttpPost("Media")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Media>> PostMedia(IFormFile video1, IFormFile video2, [FromForm] string playlistId)
        {
            if (video1 == null || video1.Length == 0)
            {
                return BadRequest("video1 is compulsory");
            }

            if (Path.GetExtension(video1.FileName) != ".mp4")
            {
                return BadRequest("File format not permitted");
            }

            Video video = new Video();
            Media media = new Media
            {
                PlaylistId = playlistId,
                SourceType = SourceType.Local
            };

            var filePath = CommonUtils.GetTmpFile();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await video1.CopyToAsync(stream);
            }

            media.JsonMetadata.Add("video1", JsonConvert.SerializeObject(video1));
            media.JsonMetadata.Add("video1Path", filePath);

            var playlist = await _context.Playlists.FindAsync(playlistId);
            var subdir = CommonUtils.ToCourseOfferingSubDirectory(_context, playlist);
            
            int index = 0;
            try {
                    index = 1 + await _context.Medias.Where(m=> m.PlaylistId == playlist.Id).Select(m => m.Index).MaxAsync();
            } catch(Exception) {}
            
            media.Index = index;

            video.Video1 = await FileRecord.GetNewFileRecordAsync(filePath, Path.GetExtension(filePath), subdir);

            // Only do this for the first (primary) video
            media.UniqueMediaIdentifier = video.Video1.Hash;

            // Copy second File
            if (video2 != null && video2.Length > 0)
            {
                if (Path.GetExtension(video2.FileName) != ".mp4")
                {
                    return BadRequest("File format not permitted");
                }

                var filePath2 = CommonUtils.GetTmpFile();
                using (var stream = new FileStream(filePath2, FileMode.Create))
                {
                    await video2.CopyToAsync(stream);
                }

                media.JsonMetadata.Add("video2", JsonConvert.SerializeObject(video2));
                media.JsonMetadata.Add("video2Path", filePath2);
                video.Video2 = await FileRecord.GetNewFileRecordAsync(filePath2, Path.GetExtension(filePath2), subdir);
            }

            // The following is essentially a replication of the logic in DownloadMediaTask.OnConsume, but there is enough
            // of a difference between the environments such that it doesn't make sense to refactor this code into a shared project
            // (such as logging, instantiating scene detection, handling exceptions vs returning a BadRequest, etc).

            if (video.Video1 == null || !video.Video1.IsValidFile()
                || (video.Video2 != null && !video.Video2.IsValidFile()))
            {
                return BadRequest("Invalid video files");
            }

            media.Name = CommonUtils.GetMediaName(media);

            // Check if video file record already exists to prevent duplicates
            var existingFile = await _context.FileRecords.Where(f => f.Hash == video.Video1.Hash).FirstOrDefaultAsync();

            if (existingFile == null)
            {
                await AddNewVideoToMedia(video, media);
            }
            else
            {
                var existingVideo = await _context.Videos.Where(v => v.Video1Id == existingFile.Id).FirstOrDefaultAsync();

                // If the file record exists but the video doesn't, delete the old file record and create new video record
                if (existingVideo == null)
                {
                    await existingFile.DeleteFileRecordAsync(_context);
                    await AddNewVideoToMedia(video, media);
                }
                // If file and video records both already exist, delete the new duplicate video (no scene detection needed)
                else
                {
                    media.VideoId = existingVideo.Id;
                    await video.DeleteVideoAsync(_context);
                }
            }

            _context.Medias.Add(media);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMedia", new { id = media.Id }, media);
        }

        // DELETE: api/Media/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<Media>> DeleteMedia(string id)
        {
            var media = await _context.Medias.FindAsync(id);
            if (media == null)
            {
                return NotFound();
            }
            _context.Medias.Remove(media);
            await _context.SaveChangesAsync();

            return media;
        }

        // POST /api/Media/Reorder/{playlistId}
        [HttpPost("Reorder/{playlistId}")]
        public async Task<ActionResult> Reorder(string playlistId, List<string> mediaIds)
        {
            var playlist = await _context.Playlists.FindAsync(playlistId);
            if (mediaIds == null || !mediaIds.Any() || playlist == null || playlist.Medias.Count != mediaIds.Count || playlist.OfferingId == null)
            {
                return BadRequest();
            }
            var offering = await _context.Offerings.FindAsync(playlist.OfferingId);
            if (offering == null)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            var medias = new List<Media>();
            for (int i = 0; i < mediaIds.Count; i++)
            {
                var media = await _context.Medias.FindAsync(mediaIds[i]);
                if (media == null || media.PlaylistId != playlistId)
                {
                    return BadRequest("Invalid mediaIds");
                }
                media.Index = i;
                medias.Add(media);
            }
            _context.Medias.UpdateRange(medias);
            await _context.SaveChangesAsync();
            return RedirectToAction("GetPlaylist", "Playlists", new { id = playlistId });
        }

        private bool MediaExists(string id)
        {
            return _context.Medias.Any(e => e.Id == id);
        }

        private async Task AddNewVideoToMedia(Video video, Media media)
        {
            await _context.Videos.AddAsync(video);
            await _context.SaveChangesAsync();
            media.VideoId = video.Id;
            await _context.SaveChangesAsync();

            _wakeDownloader.SceneDetection(video.Id, false);
            //TODO - re-add this code, but will need to use WakeDownloader instead of directly publishing it
            //_processVideoTask.Publish(video.Id);
        }
    }
}
