using System;
using System.Collections.Generic;
using System.Text;
using ClassTranscribeDatabase.Models;
using System.IO;
using System.Security.Cryptography;
using ClassTranscribeDatabase;
using System.Linq;

namespace TaskEngine
{
    public class FileHasher
    {
        public static void ComputeSha256HashForDirectory(string dir_path)
        {
            
            if (!Directory.Exists(dir_path))
            {
                Console.WriteLine("The directory specified could not be found.");
                return;
            }
            List<FileRecord> result = new List<FileRecord>();
            // Create a DirectoryInfo object representing the specified directory.
            var dir = new DirectoryInfo(dir_path);
            // Get the FileInfo objects for every file in the directory.
            FileInfo[] files = dir.GetFiles();
            // Initialize a SHA256 hash object.
            using (var _context = CTDbContext.CreateDbContext())
            {
                using (SHA256 mySHA256 = SHA256.Create())
                {
                    // Compute the hash values for each file in directory.
                    foreach (FileInfo fInfo in files)
                    {
                        try
                        {
                            if (!_context.FileRecords.Any(f => f.Path == fInfo.FullName))
                            {
                                result.Add(new FileRecord(fInfo.FullName));
                            }
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine($"I/O Exception: {e.Message}");
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            Console.WriteLine($"Access Exception: {e.Message}");
                        }
                    }
                }
                _context.FileRecords.AddRange(result);
                _context.SaveChanges();
            }
        }
    }
}
