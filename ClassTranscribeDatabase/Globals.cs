namespace ClassTranscribeDatabase
{
    /// <summary>
    /// All the configuration setttins that are supplied either by vs_appsettings.json or by environment variables.
    /// </summary>
    public class AppSettings
    {
        public string LTI_SHARED_SECRET { get; set; }
        public string MEDIA_WORKER_SHARED_SECRET { get; set; }

        public string JWT_EXPIRE_DAYS { get; set; }
        public string HOST_NAME { get; set; }
        public string JWT_KEY { get; set; }
        public string ALLOWED_HOSTS { get; set; }
        public string POSTGRES_SERVER_NAME { get; set; }
        public string POSTGRES_SERVER_PORT { get; set; } = "5432";
        public string POSTGRES_DB { get; set; }

        public string ADMIN_USER_ID { get; set; }
        public string ADMIN_PASSWORD { get; set; }
        public string RabbitMQServer { get; set; } = ""; // deprecated - use RABBITMQ_SERVER_NAME instead

        public string RABBITMQ_SERVER_NAME { get; set; } = ""; // consistent with other keys including POSTGRES

        public string RABBITMQ_REFCOUNT_CHANNELS { get; set; } = ""; // consistent with other keys including POSTGRES

        public string RABBITMQ_PORT { get; set; } = "5672";

        // RABBITMQ_PREFETCH_COUNT has been replaced with these CONCURRENT LIMITS-
        //no longer used g RABBITMQ_PREFETCH_COUNT { get; set; } // No longer used; can be deleted in next cleanup
        public string MAX_CONCURRENT_TRANSCRIPTIONS { get; set; }
        // Tasks that require significant processing e.g. scene detect, recoding
        public string MAX_CONCURRENT_VIDEO_TASKS { get; set; }

        // LIMITS ARE PER QUEUE e.g. 5 Download playlists tasks and 5 download video tasks
        public string MAX_CONCURRENT_SYNC_TASKS { get; set; }


        public string PYTHON_RPC_SERVER { get; set; }
        public string AZURE_SUBSCRIPTION_KEYS { get; set; }
        public string DATA_DIRECTORY { get; set; }
        public string AUTH0_DOMAIN { get; set; }
        public string AUTH0_CLIENT_ID { get; set; }
        public string BOX_CLIENT_ID { get; set; }
        public string BOX_CLIENT_SECRET { get; set; }
        public string APPLICATION_INSIGHTS_KEY { get; set; }
        public string SLACK_WEBHOOK_URL { get; set; }
        public string TEST_SIGN_IN { get; set; }
        public string CILOGON_CLIENT_ID { get; set; }
        public string CILOGON_CLIENT_SECRET { get; set; }
        public string CILOGON_DOMAIN { get; set; }
        public string GITSHA1 { get; set; }
        public string BUILDNUMBER { get; set; }
        public string ES_CONNECTION_ADDR { get; set; }

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
        public const string ROLE_MEDIA_WORKER = "MediaWorker";

        public const string TEST_USER_ID = "99";
        public const string MEDIA_WORKER_USER_ID = "98";

        public const string POLICY_UPDATE_OFFERING = "UpdateOffering";
        public const string POLICY_READ_OFFERING = "ReadOffering";
        public const string CLAIM_USER_ID = "classtranscribe/UserId";
   
        public const string MEDIA_WORKER_EMAIL = "automated_media_worker@classtranscribe"; // Deliberately invalid domain
        public const string TEST_USER_EMAIL = "testuser999@classtranscribe.com";

        public const int CAPTION_LENGTH = 40;
    }
}
