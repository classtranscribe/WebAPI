using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests.Utils
{
    public class Common
    {
        public static bool IsValidFilePath(Entity entity)
        {
            switch (entity)
            {
                case Course c:
                    return c.FilePath.Length == 9
                        && c.FilePath.Substring(0, 5) == $"{c.CreatedAt:yyMM}-"
                        && Directory.Exists(Path.Combine(Globals.appSettings.DATA_DIRECTORY, c.FilePath))
                        && c.FilePath[5..].ToCharArray().All(char.IsLetterOrDigit);

                case CourseOffering co:
                    return co.FilePath.Length == 19
                        && co.FilePath.Substring(0, 9) == co.Course.FilePath
                        && co.FilePath.ToCharArray()[9] == Path.DirectorySeparatorChar
                        && co.FilePath.Substring(10, 5) == $"{co.CreatedAt:yyMM}-"
                        && Directory.Exists(Path.Combine(Globals.appSettings.DATA_DIRECTORY, co.FilePath))
                        && co.FilePath[15..].ToCharArray().All(char.IsLetterOrDigit);

                default:
                    return false;
            }
        }

        public static async Task<CourseOffering> GetCourseOfferingForFileRecord(CTDbContext context)
        {
            var c = new Course { Id = "c_filerecord" };
            var o = new Offering { Id = "o_filerecord" };
            var co = new CourseOffering { CourseId = c.Id, OfferingId = o.Id };
            var p = new Playlist { OfferingId = o.Id };

            context.Courses.Add(c);
            context.CourseOfferings.Add(co);
            context.Offerings.Add(o);
            context.Playlists.Add(p);

            await FileRecord.SetFilePath(context, c);
            await FileRecord.SetFilePath(context, co);

            context.SaveChanges();

            return co;
        }
    }
}
