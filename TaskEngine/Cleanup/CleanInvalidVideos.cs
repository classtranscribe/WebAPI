using ClassTranscribeDatabase;
using System.IO;
using System.Linq;

namespace TaskEngine.Cleanup
{
    class CleanInvalidVideos
    {
        // Deletes all Videos which don't have a file or have an invalid file (size under 1000 bytes)

        private readonly CTDbContext _context;
        public CleanInvalidVideos(CTDbContext context)
        {
            _context = context;
        }

        public void Clean()
        {
            var incompleteVideos = _context.Videos.Select(v => new
            {
                Length = File.Exists(v.Video1.Path) ? new System.IO.FileInfo(v.Video1.Path).Length : -1,
                Path = v.Video1.Path,
                Video = v
            }).ToList().Where(v => v.Length < 1000).OrderBy(v => v.Length).Select(v => new { v.Video, v.Length }).ToList();

            foreach (var item in incompleteVideos)
            {
                if (File.Exists(item.Video.Video1.Path))
                {
                    File.Delete(item.Video.Video1.Path);
                }
                _context.FileRecords.Remove(item.Video.Video1);
                if (item.Video.Video2 != null)
                {
                    if (File.Exists(item.Video.Video2.Path))
                    {
                        File.Delete(item.Video.Video2.Path);
                    }
                    _context.FileRecords.Remove(item.Video.Video2);
                }
                if (item.Video.Audio != null)
                {
                    if (File.Exists(item.Video.Audio.Path))
                    {
                        File.Delete(item.Video.Audio.Path);
                    }
                    _context.FileRecords.Remove(item.Video.Audio);
                }
                _context.Videos.Remove(item.Video);
            }

            _context.SaveChanges();
        }
    }
}
