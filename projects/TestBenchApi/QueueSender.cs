using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;

namespace TestBenchApi;

public class QueueSender
{
    private readonly ServiceBusSender _messageSender;
    private readonly ILogger _logger;

    public QueueSender(
        IAzureClientFactory<ServiceBusSender> serviceBusFactory,
        ILogger<QueueSender> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceBusFactory);

        _messageSender = serviceBusFactory.CreateClient("QueueSender");
        _logger = logger;
    }

    public async Task SendMessageAsync()
    {
        CloudEvent cloudEvent = new(
            source: "https://testapi.com/events",
            type: "sl.testapi.queue.message",
            jsonSerializableData: new MessageArgs("Hello World!"));

        ServiceBusMessage message = new(new BinaryData(cloudEvent))
        {
            ContentType = "application/cloudevents+json"
        };

        await _messageSender.SendMessageAsync(message);
        _logger.LogInformation("Message sent to Azure Service Bus.");
    }
}