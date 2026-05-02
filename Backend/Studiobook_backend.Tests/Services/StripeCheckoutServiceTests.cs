using Microsoft.Extensions.Options;
using Studiobook_backend.Services;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Tests.Services;

public class StripeCheckoutServiceTests
{
    [Fact]
    public void PublishableKey_ReturnsConfiguredPublishableKey()
    {
        var service = new StripeCheckoutService(
            Options.Create(new StripeSettings
            {
                PublishableKey = "pk_test_dummy",
                SecretKey = "sk_test_dummy",
                SuccessUrl = "https://frontend.example.com/reservations/complete",
                CancelUrl = "https://frontend.example.com/rooms/{ROOM_ID}"
            }));

        Assert.Equal("pk_test_dummy", service.PublishableKey);
    }
}