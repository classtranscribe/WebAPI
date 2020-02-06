﻿
using Microsoft.Extensions.Logging;

namespace ClassTranscribeDatabase
{
    public class AppSettings
    {
        public string JWT_EXPIRE_DAYS { get; set; }
        public string JWT_ISSUER { get; set; }
        public string JWT_KEY { get; set; }
        public string ALLOWED_HOSTS { get; set; }
        public string POSTGRES_SERVER_NAME { get; set; }
        public string POSTGRES_DB { get; set; }
        public string POSTGRES_USER { get; set; }
        public string POSTGRES_PASSWORD { get; set; }
        public string RabbitMQServer { get; set; }
        public string NODE_RPC_SERVER { get; set; }
        public string PYTHON_RPC_SERVER { get; set; }
        public string AZURE_SUBSCRIPTION_KEYS { get; set; }
        public string DATA_DIRECTORY { get; set; }
        public string AUTH0_DOMAIN { get; set; }
        public string AUTH0_CLIENT_ID { get; set; }
        public string GOD_MODE_PASSWORD { get; set; }
        public string BOX_CLIENT_ID { get; set; }
        public string BOX_CLIENT_SECRET { get; set; }
        public string APPLICATION_INSIGHTS_KEY { get; set; }
    }

    public static class Globals
    {
        public static AppSettings appSettings;
        public static ILogger logger;
        public const string ROLE_INSTRUCTOR = "Instructor";
        public const string ROLE_STUDENT = "Student"; // Modifiable
        public const string ROLE_ADMIN = "Admin"; // Unmodifiable
        public const string ROLE_TEACHING_ASSISTANT = "TeachingAssistant";
        public const string ROLE_UNIVERSITY_ADMIN = "UniversityAdmin";
        public const string ROLE_ADVISORS = "Advisors";
        public const string POLICY_UPDATE_OFFERING = "UpdateOffering";
        public const string POLICY_READ_OFFERING = "ReadOffering";
    }
}
