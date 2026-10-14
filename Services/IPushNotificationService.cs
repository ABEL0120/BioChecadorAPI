using System.Text.Json;
using BioChecadorAPI.DTOs;
using WebPush;

namespace BioChecadorAPI.Services
{
    public interface IPushNotificationService
    {
        Task<bool> EnviarNotificacionAsync(string subscriptionJson, string titulo, string mensaje, string url = "/home");
    }

    public class PushNotificationService : IPushNotificationService
    {
        private readonly VapidDetails _vapidDetails;

        public PushNotificationService(IConfiguration config)
        {
            var subject = config["VapidKeys:Subject"];
            var publicKey = config["VapidKeys:PublicKey"];
            var privateKey = config["VapidKeys:PrivateKey"];

            _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        }

        public async Task<bool> EnviarNotificacionAsync(string subscriptionJson, string titulo, string mensaje, string url = "/home")
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var subData = JsonSerializer.Deserialize<PushSubscriptionPayload>(subscriptionJson, options);

                if (subData == null || string.IsNullOrWhiteSpace(subData.Endpoint))
                    return false;

                var subscription = new PushSubscription(
                    subData.Endpoint,
                    subData.Keys.P256dh,
                    subData.Keys.Auth
                );

                var payloadObj = new NotificacionPushModel
                {
                    Title = titulo,
                    Body = mensaje,
                    Data = new { url }
                };

                var payloadJson = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var client = new WebPushClient();
                await client.SendNotificationAsync(subscription, payloadJson, _vapidDetails);
                return true;
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
