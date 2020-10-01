using System;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Tests
{
    public class Tests
    {
        // Inputs: token, api, method, inputs, expected outputs
        public class TestData : IEnumerable<object[]>
        {
            string[] inputs1 = new string[] { };
            string exp1 = @"[
  {
    'offering': {
      'sectionName': 'AB',
      'termId': '0001',
      'accessType': 0,
      'logEventsFlag': false,
      'courseName': 'Test Course',
      'description': null,
      'jsonMetadata': null,
      'visibility': 0,
      'id': '4003'
    },
    'courses': [
      {
        'courseId': 'test_course',
        'courseName': null,
        'courseNumber': '000',
        'description': null,
        'departmentId': '2001',
        'departmentAcronym': 'CS'
      }
    ],
    'instructorIds': [
      {
        'firstName': 'Test',
        'lastName': 'User',
        'universityId': null,
        'status': 0,
        'metadata': null,
        'id': '99',
        'userName': null,
        'normalizedUserName': null,
        'email': 'testuser999@classtranscribe.com',
        'normalizedEmail': null,
        'emailConfirmed': false,
        'passwordHash': null,
        'securityStamp': '59f8704e-94a2-494b-a85f-2594d2e1d8e8',
        'concurrencyStamp': '75a7fbf7-d470-411f-96e2-3263874c2e4a',
        'phoneNumber': null,
        'phoneNumberConfirmed': false,
        'twoFactorEnabled': false,
        'lockoutEnd': null,
        'lockoutEnabled': false,
        'accessFailedCount': 0
      }
    ],
    'term': {
      'name': 'Test Term',
      'startDate': '2020-04-10T20:25:13.733004',
      'endDate': '2020-07-10T20:25:13.733429',
      'universityId': '1001',
      'id': '0001'
    }
  },
  {
    'offering': {
      'sectionName': 'AB',
      'termId': '0001',
      'accessType': 0,
      'logEventsFlag': false,
      'courseName': 'Test Course',
      'description': null,
      'jsonMetadata': null,
      'visibility': 0,
      'id': '4002'
    },
    'courses': [
      {
        'courseId': 'test_course',
        'courseName': null,
        'courseNumber': '000',
        'description': null,
        'departmentId': '2001',
        'departmentAcronym': 'CS'
      }
    ],
    'instructorIds': [
      {
        'firstName': 'Test2',
        'lastName': 'User2',
        'universityId': null,
        'status': 0,
        'metadata': null,
        'id': '002',
        'userName': null,
        'normalizedUserName': null,
        'email': 'testuser002@classtranscribe.com',
        'normalizedEmail': null,
        'emailConfirmed': false,
        'passwordHash': null,
        'securityStamp': '24889ab2-401b-49df-9767-3973706340b0',
        'concurrencyStamp': '663e3b25-2897-421f-b823-7fa3841d4469',
        'phoneNumber': null,
        'phoneNumberConfirmed': false,
        'twoFactorEnabled': false,
        'lockoutEnd': null,
        'lockoutEnabled': false,
        'accessFailedCount': 0
      },
      {
        'firstName': 'est',
        'lastName': 'User',
        'universityId': null,
        'status': 0,
        'metadata': null,
        'id': '99',
        'userName': null,
        'normalizedUserName': null,
        'email': 'testuser999@classtranscribe.com',
        'normalizedEmail': null,
        'emailConfirmed': false,
        'passwordHash': null,
        'securityStamp': '273a5d5b-bd1a-46f4-bed8-64048cfef1a3',
        'concurrencyStamp': '4649d124-af7a-4a51-934e-5045b1453b08',
        'phoneNumber': null,
        'phoneNumberConfirmed': false,
        'twoFactorEnabled': false,
        'lockoutEnd': null,
        'lockoutEnabled': false,
        'accessFailedCount': 0
      }
    ],
    'term': {
      'name': 'Test Term',
      'startDate': '2020-04-10T20:25:13.733004',
      'endDate': '2020-07-10T20:25:13.733429',
      'universityId': '1001',
      'id': '0001'
    }
  }
]";
            string exp2 = @"[
  {
    'offering': {
      'sectionName': 'AB',
      'termId': '0001',
      'accessType': 0,
      'logEventsFlag': false,
      'courseName': 'Test Course',
      'description': null,
      'jsonMetadata': null,
      'visibility': 0,
      'id': '4003'
    },
    'courses': [
      {
        'courseId': 'test_course',
        'courseName': null,
        'courseNumber': '000',
        'description': null,
        'departmentId': '2001',
        'departmentAcronym': 'CS'
      }
    ],
    'instructorIds': [
      {
        'firstName': 'Test',
        'lastName': 'User',
        'universityId': null,
        'status': 0,
        'metadata': null,
        'id': '99',
        'userName': null,
        'normalizedUserName': null,
        'email': 'testuser999@classtranscribe.com',
        'normalizedEmail': null,
        'emailConfirmed': false,
        'passwordHash': null,
        'securityStamp': '59f8704e-94a2-494b-a85f-2594d2e1d8e8',
        'concurrencyStamp': '75a7fbf7-d470-411f-96e2-3263874c2e4a',
        'phoneNumber': null,
        'phoneNumberConfirmed': false,
        'twoFactorEnabled': false,
        'lockoutEnd': null,
        'lockoutEnabled': false,
        'accessFailedCount': 0
      }
    ],
    'term': {
      'name': 'Test Term',
      'startDate': '2020-04-10T20:25:13.733004',
      'endDate': '2020-07-10T20:25:13.733429',
      'universityId': '1001',
      'id': '0001'
    }
  },
  {
    'offering': {
      'sectionName': 'AB',
      'termId': '0001',
      'accessType': 0,
      'logEventsFlag': false,
      'courseName': 'Test Course',
      'description': null,
      'jsonMetadata': null,
      'visibility': 0,
      'id': '4002'
    },
    'courses': [
      {
        'courseId': 'test_course',
        'courseName': null,
        'courseNumber': '000',
        'description': null,
        'departmentId': '2001',
        'departmentAcronym': 'CS'
      }
    ],
    'instructorIds': [
      {
        'firstName': 'Test2',
        'lastName': 'User2',
        'universityId': null,
        'status': 0,
        'metadata': null,
        'id': '002',
        'userName': null,
        'normalizedUserName': null,
        'email': 'testuser002@classtranscribe.com',
        'normalizedEmail': null,
        'emailConfirmed': false,
        'passwordHash': null,
        'securityStamp': '24889ab2-401b-49df-9767-3973706340b0',
        'concurrencyStamp': '663e3b25-2897-421f-b823-7fa3841d4469',
        'phoneNumber': null,
        'phoneNumberConfirmed': false,
        'twoFactorEnabled': false,
        'lockoutEnd': null,
        'lockoutEnabled': false,
        'accessFailedCount': 0
      },
      {
        'firstName': 'Test',
        'lastName': 'User',
        'universityId': null,
        'status': 0,
        'metadata': null,
        'id': '99',
        'userName': null,
        'normalizedUserName': null,
        'email': 'testuser999@classtranscribe.com',
        'normalizedEmail': null,
        'emailConfirmed': false,
        'passwordHash': null,
        'securityStamp': '273a5d5b-bd1a-46f4-bed8-64048cfef1a3',
        'concurrencyStamp': '4649d124-af7a-4a51-934e-5045b1453b08',
        'phoneNumber': null,
        'phoneNumberConfirmed': false,
        'twoFactorEnabled': false,
        'lockoutEnd': null,
        'lockoutEnabled': false,
        'accessFailedCount': 0
      }
    ],
    'term': {
      'name': 'Test Term',
      'startDate': '2020-04-10T20:25:13.733004',
      'endDate': '2020-07-10T20:25:13.733429',
      'universityId': '1001',
      'id': '0001'
    }
  }
]";

            string _token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0dXNlcjk5OUBjbGFzc3RyYW5zY3JpYmUuY29tIiwianRpIjoiMjMzOTM5YjAtMjI4Ni00NjM3LTk2NmUtYTU3NmY4NWUwYjc5IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZ2l2ZW5uYW1lIjoiVGVzdCIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL3N1cm5hbWUiOiJVc2VyIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZW1haWxhZGRyZXNzIjoidGVzdHVzZXI5OTlAY2xhc3N0cmFuc2NyaWJlLmNvbSIsImNsYXNzdHJhbnNjcmliZS9Vc2VySWQiOiI5OSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFkbWluIiwiZXhwIjoxNjAwMjM5MTkzLCJpc3MiOiJodHRwczovL2xvY2FsaG9zdCIsImF1ZCI6Imh0dHBzOi8vbG9jYWxob3N0In0.k67HR7RtGf9ZUATxrpp_dvWOaXwH1H2DxE6VK941FDI";
            string _api = "api/offerings/bystudent";
            string _method = "Get"; // Get / Put / Delete / Post 


            public IEnumerator<object[]> GetEnumerator()
            { 
                yield return new object[] { _token, _api, _method, inputs1, exp1  };
                yield return new object[] { _token, _api, _method, inputs1, exp2 };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }


        [Theory]
        [ClassData(typeof(TestData))]
        public void AssertResultIsCorrect( string _token, string _api, string _method, string[] _input, string _expectation)
        {

            //Setting.GenerateSetting();
            Setting.baseUrl = "https://localhost:5001/";
            Setting.ignoreProperties = new List<string>
            {
                "concurrencyStamp",
                "securityStamp"
            };

            TestCase t = new TestCase(_token, _api, _method, _input, _expectation);

            AssertInfo log = t.Run();

            Assert.Equal("succeed", log.assert);
        }


    }
}