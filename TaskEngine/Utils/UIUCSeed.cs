using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TaskEngine.Utils
{
    public class UIUCSeed
    {
        public class CSVCourse
        {
            public string TERM_DESC { get; set; }
            public string CRN { get; set; }

            public string SUBJ { get; set; }
            public string NBR { get; set; }

            public string SEC { get; set; }
            public string CRS_TITLE { get; set; }
            public string SCHED_TYPE { get; set; }
        }

        public static void SeedCourses()
        {
            string file = Path.Combine(Globals.appSettings.DATA_DIRECTORY, "seed", "Fall2019CourseList.csv");
            TextReader reader = new StreamReader(file);
            var csvReader = new CsvReader(reader);
            var records = csvReader.GetRecords<CSVCourse>();
            List<CSVCourse> csvCourses = new List<CSVCourse>(records);
            using (var _context = CTDbContext.CreateDbContext())
            {
                Department eceDept = _context.Departments.Where(d => d.Acronym == "ECE" && d.UniversityId == "1001").FirstOrDefault();
                List<Course> courses = csvCourses.Where(c => c.SUBJ == "ECE").GroupBy(c => c.CRS_TITLE).Select(c => new Course
                {
                    CourseName = c.First().CRS_TITLE,
                    Description = c.First().CRS_TITLE,
                    CourseNumber = c.First().NBR,
                    Department = eceDept                    
                }).ToList();
                _context.Courses.AddRange(courses);
                _context.SaveChanges();
            }


            Console.WriteLine("Test");
        }
    }
}
