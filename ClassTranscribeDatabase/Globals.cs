namespace ClassTranscribeDatabase
{
    public class AppSettings
    {
        public string AZURE_B2C_SIGNIN_POLICY { get; set; }
        public string AZURE_B2C_DOMAIN { get; set; }
        public string AZURE_B2C_DIRECTORY { get; set; }
        public string AZURE_B2C_CLIENTID { get; set; }
        public string JWT_EXPIRE_DAYS { get; set; }
        public string JWT_ISSUER { get; set; }
        public string JWT_KEY { get; set; }
        public string ALLOWED_HOSTS { get; set; }
        public string POSTGRES { get; set; }
        public string AzureAdB2CInstance { get; set; }
        public string RabbitMQServer { get; set; }
        public string NODE_RPC_SERVER { get; set; }
        public string AZURE_REGION { get; set; }
        public string AZURE_SUBSCRIPTION_KEY { get; set; }
        public string DATA_DIRECTORY { get; set; }
    }

    public static class Globals
    {
        public static AppSettings appSettings;
        public const string ROLE_INSTRUCTOR = "Instructor";
        public const string ROLE_STUDENT = "Student"; // Modifiable
        public const string ROLE_ADMIN = "Admin"; // Unmodifiable
        public const string ROLE_UNIVERSITY_ADMIN = "UniversityAdmin";
    }
}
