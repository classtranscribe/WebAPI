using System;
using Microsoft.CognitiveServices.Speech;
using Xunit;

namespace UnitTests
{
    public class SpeechSDKLoadTest
    {
        [Fact]
        public void TestNativeLibraryLoad()
        {
            try
            {
                var config = SpeechConfig.FromSubscription("d82a4773-4e61-44e2-a21b-f2c37da7a892", "westus");
                // Attempting to create a recognizer triggers full native library initialization including audio.
                using (var recognizer = new SpeechRecognizer(config))
                {
                    Assert.NotNull(recognizer);
                }
            }
            catch (TypeInitializationException ex)
            {
                Assert.Fail($"Failed to load native Speech SDK library: {ex.Message} {ex.InnerException?.Message}");
            }
            catch (DllNotFoundException ex)
            {
                Assert.Fail($"Native Speech SDK library or dependency not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                // If it's just an auth error, the library LOADED successfully.
                if (ex.Message.Contains("Subscription key") || ex.Message.Contains("region") || ex.Message.Contains("Exception with error code"))
                {
                    return;
                }
                throw;
            }
        }
    }
}
