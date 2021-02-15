
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ClassTranscribeDatabase
{
    /// <summary>
    /// All the configuration setttins that are supplied either by vs_appsettings.json or by environment variables.
    /// </summary>
    public class AppSettings
    {
        public AppSettings() {
            Globals.appSettings = this;
        }
        public string JWT_EXPIRE_DAYS { get; set; } = "30"; // Days
        public string HOST_NAME { get; set; }="localhost";
        public string JWT_KEY { get; set; }= "localtestingonly"; // Production needs a secret string
        public string ALLOWED_HOSTS { get; set; }="*";
        // See DatabaseConnectionString below
        // Explicitly set the connection string-
          public string POSTGRES_CONNECTION_STRING { get; set; } = "";
          // Or set the parameters-
        public string POSTGRES_SERVER_NAME { get; set; } = "";
        public string POSTGRES_SERVER_PORT { get; set; } = "5432";
        public string POSTGRES_MAX_POOL_SIZE{ get; set; } = "10";
      
        public string POSTGRES_USER { get;set;} = "postgres";
        public string POSTGRES_PASS {get; set;} = "ctpass";
  
        public string POSTGRES_DB { get; set; }="ct";
        public string ADMIN_USER_ID {  get; set; } = "ctdev";
        public string ADMIN_PASSWORD { get; set; }= "ctpass";
        

        public string RabbitMQServer { get; set; } = ""; // deprecated - use RABBITMQ_SERVER_NAME instead

        public string RABBITMQ_SERVER_NAME { get; set; } = ""; // consistent with other keys including POSTGRES
        public string RabbitMQServerName { get {
            string candidate = RABBITMQ_SERVER_NAME.Length >0 ? RABBITMQ_SERVER_NAME : RabbitMQServer;
           
            if(candidate.Length > 0) {
                return candidate;
            }
            return IsRunningInDocker() ? "rabbitmq" : "localhost";
        }}
        
        public string RABBITMQ_REFCOUNT_CHANNELS { get; set; } = ""; // consistent with other keys including POSTGRES

        public string RABBITMQ_PORT { get; set; } = "5672";

        // RABBITMQ_PREFETCH_COUNT has been replaced with these CONCURRENT LIMITS-
        //no longer used g RABBITMQ_PREFETCH_COUNT { get; set; } // No longer used; can be deleted in next cleanup
        public string MAX_CONCURRENT_TRANSCRIPTIONS { get; set; } = "1";
        // Tasks that require significant processing e.g. scene detect, recoding
        public string MAX_CONCURRENT_VIDEO_TASKS { get; set; } = "1";

        // LIMITS ARE PER QUEUE e.g. 5 Download playlists tasks and 5 download video tasks
        public string MAX_CONCURRENT_SYNC_TASKS { get; set; } = "1";

        public string PYTHON_RPC_SERVER { get; set; } = "pythonrpcserver:50051";
        public string AZURE_SUBSCRIPTION_KEYS { get; set; } = "";
        public string DATA_DIRECTORY  { get; set; } = "./docker_data/data";
        public string AUTH0_DOMAIN { get; set; } = "";
         public string AUTH0_CLIENT_ID { get; set; } = "";
        public string BOX_CLIENT_ID { get; set; } = "";
        public string BOX_CLIENT_SECRET { get; set; } = "";
        public string APPLICATION_INSIGHTS_KEY { get; set; } = "";
        public string SLACK_WEBHOOK_URL { get; set; } = "";
        public string TEST_SIGN_IN { get; set; }
        public bool TestSignIn {
            get {
                return TEST_SIGN_IN.ToUpperInvariant() == "TRUE"; 
                }
        }

        public string CILOGON_CLIENT_ID { get; set; } = "";
        public string CILOGON_CLIENT_SECRET { get; set; } = "";
        public string CILOGON_DOMAIN { get; set; } = "";
        public string GITSHA1 { get; set; } = "";
        public string BUILDNUMBER { get; set; } = "";
        public string ES_CONNECTION_ADDR { get; set; } = "";
        public string ESConnectionAddress { 
            get {
                if( ES_CONNECTION_ADDR.Length > 0 ) {
                    return ES_CONNECTION_ADDR.StartsWith("http") ? ES_CONNECTION_ADDR : ""; // Invalid strings will disable elastic search 
                }
                return IsRunningInDocker() ?  "http://elasticsearch:9200" :  "http://localhost:9200" ;
            }
        }

        public string ES_INDEX_TIME_TO_LIVE {get; set;} = "2880";

        // We let the message expire - which is likely if the server is overloaded- the periodic check will rediscover things to do later
        // The suggested value is PERIODIC_CHECK_EVERY_MINUTES -5
        // i.e we assume that the periodic check takes no more than 5 minutes to enqueue a task
        // If it expires - that is okay - we will rediscover it in a future periodic check
        public string RABBITMQ_TASK_TTL_MINUTES { get; set; } = "55";

        // Goes to upstream providers to get latest list of videos identifiers for each active playlist
        // Identifies missing items provided the object creating time is older than PERIODIC_CHECK_OLDER_THAN_MINUTES
        public string PERIODIC_CHECK_EVERY_MINUTES { get; set; } = "120";

        // PERIODIC_CHECK_OLDER_THAN_MINUTES Should be at least PERIODIC_CHECK_EVERY_MINUTES, probably more
        // We only want to identify missing tasks that clearly should have completed in the previous cycle (or older)
        public string PERIODIC_CHECK_OLDER_THAN_MINUTES { get; set; } = "240";

        // Transcripion and Translation options

        // e.g. en-US
        // See MSTranscriptionService for known supported recognition languages
        public string SPEECH_RECOGNITION_DIALECT { get; set; } = "en-US";

        // e.g. Mostly two letter codes e.g. fr
        // See MSTranscriptionService.cs for known supported translation languages
        public string LANGUAGE_TRANSLATIONS { get; set; } = "zh-Hans,ko,es,fr";

        // See MSTranscriptionService
        public string MOCK_RECOGNITION { get; set; } = "";

        public string DIGEST_CALCULATION_METHOD { get; set; } = "";

        public string DEV_ENV {get;set;} = "";

        public bool IsRunningInDocker() {
                return DEV_ENV == "DOCKER";
        }

        public string DatabaseConnectionString {
            get {
                if(POSTGRES_CONNECTION_STRING.Length > 0) {
                    return POSTGRES_CONNECTION_STRING;
                }
                string server = POSTGRES_SERVER_NAME;
                if(server.Length == 0) {
                    server = IsRunningInDocker() ? "db" : "localhost";
                }
                return $"Server={server};Port={POSTGRES_SERVER_PORT};Database={POSTGRES_DB};User Id={POSTGRES_USER};Password={POSTGRES_PASS};MaxPoolSize={POSTGRES_MAX_POOL_SIZE}";
            }
        }
    }

    /// <summary>
    /// Global Constants used across the project.
    /// </summary>
    public static class Globals
    {
        public static AppSettings appSettings;
        public const string ROLE_INSTRUCTOR = "Instructor";
        public const string ROLE_STUDENT = "Student"; // Modifiable
        public const string ROLE_ADMIN = "Admin"; // Unmodifiable
        public const string ROLE_TEACHING_ASSISTANT = "TeachingAssistant";
        public const string ROLE_UNIVERSITY_ADMIN = "UniversityAdmin";
        public const string ROLE_ADVISORS = "Advisors";
        public const string POLICY_UPDATE_OFFERING = "UpdateOffering";
        public const string POLICY_READ_OFFERING = "ReadOffering";
        public const string CLAIM_USER_ID = "classtranscribe/UserId";

        public const int CAPTION_LENGTH = 40;
    }
}
