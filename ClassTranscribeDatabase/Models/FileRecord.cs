using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase.Models
{
    /// <summary>
    /// This class stores the record of a file in the database.
    /// </summary>
    public class FileRecord : Entity
    {
        private FileRecord(string path)
        {
            // See logic in Path setter below. If /data/ is not present in the path then this little statement throws an ArgumentException
            Path = path;

            FileName = System.IO.Path.GetFileName(path);
        }

        /// <summary>
        /// Generate a new file record for a given file.
        /// </summary>
        /// <param name="filepath">Path of the file</param>
        /// <param name="ext">Extension of the file</param>
        public static async Task<FileRecord> GetNewFileRecordAsync(string filepath, string ext, string subdir)
        {
            // string courseOfferingSubDir = "";
            // if ( courseOffering == null || string.IsNullOrEmpty(courseOffering.FilePath)) {
            //     courseOfferingSubDir="/data/"; //legacy, pre 2022

            // } else if ( courseOffering.IsDeletedStatus == Status.Deleted  )
            // {
            //     throw new InvalidOperationException("Invalid CourseOffering entity- Course Offering was deleted");
            // } else {
            //     courseOfferingSubDir = courseOffering.FilePath; // e.g. "/data/2203-abcd"
             if(subdir.Contains("..") )
            {
               throw new InvalidOperationException("Sanity check: Course Offering FilePaths cannot contain parent directory traversal paths, '..'");
            }

            // }

            // Move file to the CourseOffering's FilePath
            var tmpFile = new FileRecord(filepath);

            var newDirectory = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, subdir );
            
            Directory.CreateDirectory(newDirectory); // No-op if dir already exists and is a directory
            var uuid = Guid.NewGuid().ToString();
            var newFilePath = System.IO.Path.Combine(newDirectory, uuid + ext);
        
            File.Move(tmpFile.Path, newFilePath);

            var fileRecord = new FileRecord(newFilePath);
            // TODO: Add .ConfigureAwait(false) here? or not?
            // See https://devblogs.microsoft.com/dotnet/configureawait-faq/
            fileRecord.Hash = await ComputeSha256HashForFileAsync(fileRecord.Path);
            fileRecord.Id = uuid;
            return fileRecord;
        }

        public static async Task SetFilePath(CTDbContext context, Entity entity)
        {
            // If the entity's CreatedAt field is set to the default DateTime (Jan 1, 0001) this means that
            // it is a new Entity that hasn't yet been added to the DB so we can just use the current date
            var createdAt = entity.CreatedAt != default ? entity.CreatedAt : DateTime.Now;
            var filePath = $"{createdAt:yyMM}-{CommonUtils.RandomString(4)}";

            switch (entity)
            {
                case Course course:
                    if (!string.IsNullOrEmpty(course.FilePath))
                    {
                        throw new InvalidOperationException("FilePath already exists.");
                    }

                    var newDirectory = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, filePath);

                    while (Directory.Exists(newDirectory))
                    {
                        filePath = $"{createdAt:yyMM}-{CommonUtils.RandomString(4)}";
                        newDirectory = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, filePath);
                    }

                    course.FilePath = filePath;
                    Directory.CreateDirectory(newDirectory);
                    break;

                case CourseOffering courseOffering:
                    if (!string.IsNullOrEmpty(courseOffering.FilePath))
                    {
                        throw new InvalidOperationException("FilePath already exists.");
                    }

                    var linkedCourse = await context.Courses.FindAsync(courseOffering.CourseId);
                    if (string.IsNullOrEmpty(linkedCourse?.FilePath))
                    {
                        throw new InvalidOperationException("The CourseOffering must be linked to a valid course that has a FilePath.");
                    }

                    newDirectory = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, linkedCourse.FilePath, filePath);

                    while (Directory.Exists(newDirectory))
                    {
                        filePath = $"{createdAt:yyMM}-{CommonUtils.RandomString(4)}";
                        newDirectory = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, linkedCourse.FilePath, filePath);
                    }

                    courseOffering.FilePath = System.IO.Path.Combine(linkedCourse.FilePath, filePath);
                    Directory.CreateDirectory(newDirectory);
                    break;

                default:
                    throw new InvalidOperationException("Invalid entity passed: " + entity.GetType());
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Check if a file at "filePath" exists and is of at least "minLen" bytes.
        /// </summary>
        public static bool IsValidFile(string filepath, int minLen = 100)
        {
            var temp = new FileRecord(filepath);
            // Checks if the filepath is valid and the file at least has "minLen" bytes.
            return temp.Path.Length > 0 && File.Exists(temp.Path) && new FileInfo(temp.Path).Length > minLen;
        }

        /// <summary>
        /// Check if a file at "Path" exists and is of at least "minLen" bytes.
        /// </summary>
        public bool IsValidFile(int minLen = 100)
        {
            return IsValidFile(Path, minLen);
        }

        // Callers should mark this object modified and save it to the DB
        public void ReplaceWith(FileRecord newFile)
        {

            if (newFile.Path == this.Path)
            {
                return;
            }
            File.Delete(this.Path);
            File.Move(newFile.Path, this.Path);
            if (File.Exists(newFile.Path))
            {
                File.Delete(newFile.Path); // perhaps we are moving across volumes
            }
            this.Hash = newFile.Hash;
            // don't set this.PrivatePath; we've just moved the new contents to use the same original path of this object

        }

        public FileRecord() { }
        public string FileName { get; set; }

        /// <summary>
        /// Difference between PrivatePath, Path and VMPath
        /// 1. PrivatePath is the actual path stored on the database. This path is relative to the 
        /// 2. Path
        /// </summary>
        [SwaggerIgnore]
        [IgnoreDataMember]
        public string PrivatePath { get; set; }
        [NotMapped]
        public string Path
        {
            get
            {
                string p = PrivatePath;
                // Windows
                if (System.IO.Path.DirectorySeparatorChar == '\\')
                {
                    p = PrivatePath.Replace('\\', '/');
                }
                p = p.Substring(p.LastIndexOf("/data/") + 6);
                return System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, p);
            }
            set
            {
                // Windows
                if (value.Contains('\\'))
                {
                    value = value.Replace('\\', '/');
                }
                PrivatePath = value.Substring(value.LastIndexOf("/data/"));
            }
        }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public string VMPath
        {
            get
            {
                return PrivatePath;
            }
        }
        [SwaggerIgnore]
        [IgnoreDataMember]
        public string Hash { get; set; }

        // TODO: Need a logger in this class

        /// <summary>
        /// Compute the SHA256 Hashsum of a file.
        /// </summary>
        public static async Task<string> ComputeSha256HashForFileAsync(string filePath)
        {
            string method = Globals.appSettings.DIGEST_CALCULATION_METHOD;
            if (method.Trim() == "ComputeSha256Synchronous")
            {
                // original synchrnous way - will be removed if the new TaskThread approach
                // works well in production
                // The problem with the synchronous was is that it blocks all other
                // asynchronous code form running while the sha256 digest is calculated
                return ComputeSha256Synchronous(filePath);
            }
            return await ComputeSha256TaskThread(filePath);

        }

        private async static Task<string> ComputeSha256TaskThread(string filePath)
        {
            string result = null;
            return await Task.Run(() =>
           {

               // Creating another thread is not absolutely necessary
               // We could call ComputeSha256Synchronous directly inside Task.Run()

               // However this way we can explicitly set this as a background low priority thread
               // Which *may* help Transcription tasks from timing out
               Thread t = new Thread(() => { result = ComputeSha256Synchronous(filePath); });

               t.Priority = ThreadPriority.BelowNormal; // TODO/TOREVIEW Is supported under POSIX implementation?
               t.Start();
               t.Join();
               return result;
           });
        }
        private static string ComputeSha256Synchronous(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                // Create a SHA256
                SHA256 Sha256 = SHA256.Create();

                // ComputeHash - returns byte array  
                byte[] bytes = Sha256.ComputeHash(stream);
                // Convert byte array to a string   

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        // The following is impossible because rpcClient is in the CTCommons project
        // which depends on this project, not the other way around

        //bool useRPCForFileDigest = false;
        //if(useRPCForFileDigest)
        //{
        //    var request = new CTGrpc.FileHashRequest
        //    {
        //        File = filePath,
        //        Algorithms = "sha256"
        //    };
        //    CTGrpc.FileHashResponse rpcresponse = await _rpcClient.PythonServerClient(request);
        //    return rpcresponse.Result;
        // }
        // ComputeHashAsync is not yet available (only in 5.0 RC1)
        //
        // The concern is that ComputeHash is hogging the thread for too long
        // So async tasks (e.g. SpeechToText might timeout)



        /// <summary>
        /// Delete a file record, and its corresponding file.
        /// </summary>
        public async Task DeleteFileRecordAsync(CTDbContext context)
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
            var dbFileRecord = await context.FileRecords.FindAsync(Id);
            if (dbFileRecord != null)
            {
                context.FileRecords.Remove(dbFileRecord);
                await context.SaveChangesAsync();
            }
        }
    }
}
