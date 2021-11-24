using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.IO;
using System.Linq;

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
    }
}
