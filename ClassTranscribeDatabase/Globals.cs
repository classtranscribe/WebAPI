namespace ClassTranscribeDatabase
{
    public class AppSettings
    {
        public string JWT_EXPIRE_DAYS { get; set; }
        public string JWT_ISSUER { get; set; }
        public string JWT_KEY { get; set; }
        public string ALLOWED_HOSTS { get; set; }
        public string POSTGRES { get; set; }
        public string RabbitMQServer { get; set; }
        public string NODE_RPC_SERVER { get; set; }
        public string AZURE_SUBSCRIPTION_KEYS { get; set; }
        public string DATA_DIRECTORY { get; set; }
        public string AUTH0_DOMAIN { get; set; }
        public string AUTH0_CLIENT_ID { get; set; }
    }

    public static class Globals
    {
        public static AppSettings appSettings;
        public const string ROLE_INSTRUCTOR = "Instructor";
        public const string ROLE_STUDENT = "Student"; // Modifiable
        public const string ROLE_ADMIN = "Admin"; // Unmodifiable
        public const string ROLE_UNIVERSITY_ADMIN = "UniversityAdmin";
        public const string POLICY_UPDATE_OFFERING = "UpdateOffering";
        public const string POLICY_READ_OFFERING = "ReadOffering";
    }
}
