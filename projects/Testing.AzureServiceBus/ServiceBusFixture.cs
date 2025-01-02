﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Testcontainers.ServiceBus;
using Testing.AzureServiceBus.Logging;
using Xunit.Abstractions;
using ServiceBusConfiguration = Testing.AzureServiceBus.Configuration.ServiceBusConfiguration;

namespace Testing.AzureServiceBus;

public class ServiceBusFixture : IAsyncDisposable
{
    private ServiceBusContainer? _serviceBusContainer;
    private ILogger? _logger;
    private bool _isInitialized;

    private const int ServiceBusDefaultPort = 5672;

    public void Initialize(
        ServiceBusConfiguration serviceBusConfiguration,
        ITestOutputHelper testOutputHelper)
    {
        if (_isInitialized)
        {
            return;
        }

        _logger = new XunitLogger<ServiceBusFixture>(testOutputHelper);
        FileUtils.SaveUserConfig(serviceBusConfiguration);

        _serviceBusContainer =
            new ServiceBusBuilder()
                .WithAcceptLicenseAgreement(true)
                .WithResourceMapping(
                    "Config.json",
                    "/ServiceBus_Emulator/ConfigFiles/")
                .WithPortBinding(
                    ServiceBusDefaultPort,
                    ServiceBusDefaultPort)
                .Build();
        _serviceBusContainer
            .StartAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
        _isInitialized = true;
    }

    public async Task PublishMessageAsync(
        string queueOrTopicName,
        ServiceBusMessage serviceBusMessage)
    {
        await using ServiceBusClient client = new(ServiceBusConstants.ConnectionString);
        await using ServiceBusSender? sender = client.CreateSender(queueOrTopicName);

        await sender.SendMessageAsync(serviceBusMessage);

        _logger?.LogInformation(
            "Message published to {QueueOrTopicName}.",
            queueOrTopicName);
    }

    public async Task<TMessage?> ConsumeMessageAsync<TMessage>(
        string queueName,
        int maxWaitTimeInSeconds = 5)
    {
        return await ConsumeMessageInternalAsync<TMessage>(
            queueName,
            null,
            maxWaitTimeInSeconds);
    }

    public async Task<TMessage?> ConsumeMessageAsync<TMessage>(
        string topicName,
        string subscriptionName,
        int maxWaitTimeInSeconds = 5)
    {
        return await ConsumeMessageInternalAsync<TMessage>(
            topicName,
            subscriptionName,
            maxWaitTimeInSeconds);
    }

    private async Task<TMessage?> ConsumeMessageInternalAsync<TMessage>(
        string queueOrTopicName,
        string? subscriptionName,
        int maxWaitTimeInSeconds = 5)
    {
        await using ServiceBusClient client = new(ServiceBusConstants.ConnectionString);

        ServiceBusReceiverOptions opt = new()
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
        };

        await using ServiceBusReceiver? receiver = !string.IsNullOrWhiteSpace(subscriptionName)
            ? client.CreateReceiver(
                queueOrTopicName,
                subscriptionName,
                opt)
            : client.CreateReceiver(
                queueOrTopicName,
                opt);

        ServiceBusReceivedMessage message =
            await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(maxWaitTimeInSeconds));
        if (message != null)
        {
            _logger?.LogInformation(
                "Message received from {QueueOrTopicName}.",
                queueOrTopicName);

            await receiver.CompleteMessageAsync(message);
            return message.Body.ToObjectFromJson<TMessage>();
        }

        _logger?.LogWarning("No message found to consume.");
        return default;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async Task DisposeAsync(
        bool disposing)
    {
        if (disposing)
        {
            if (_serviceBusContainer != null)
            {
                await _serviceBusContainer.DisposeAsync();
            }
        }
    }
}