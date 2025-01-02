using System;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Testing.AzureServiceBus;
using Testing.AzureServiceBus.Builder;
using Testing.AzureServiceBus.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace TestBenchApi.Tests;

public class ApiTests : IClassFixture<CustomWebApplicationFactory<IApiAssemblyMarker>>, IClassFixture<ServiceBusFixture>
{
    private readonly ServiceBusFixture _serviceBusFixture;
    private readonly CustomWebApplicationFactory<IApiAssemblyMarker> _webApplicationFactory;

    public ApiTests(
        CustomWebApplicationFactory<IApiAssemblyMarker> webApplicationFactory,
        ServiceBusFixture serviceBusFixture,
        ITestOutputHelper testOutputHelper)
    {
        ArgumentNullException.ThrowIfNull(serviceBusFixture);

        serviceBusFixture.Initialize(
            ServiceBusConfig(),
            testOutputHelper);

        _webApplicationFactory = webApplicationFactory;
        _serviceBusFixture = serviceBusFixture;
    }

    [Fact]
    public async Task TestQueueMessage()
    {
        // Arrange
        HttpClient httpClient = _webApplicationFactory.CreateClient();

        // Act
        HttpResponseMessage responseMessage = await httpClient.PostAsync(
            "api/send/queue",
            null);

        // Assert
        responseMessage.EnsureSuccessStatusCode();

        CloudEvent message = await _serviceBusFixture.ConsumeMessageAsync<CloudEvent>("testqueue");
        Assert.NotNull(message);
        Assert.NotNull(message.Data);

        MessageArgs messageArgs = message.Data.ToObjectFromJson<MessageArgs>();
        Assert.NotNull(messageArgs);

        Assert.Equal(
            "Hello World!",
            messageArgs.Message);
    }

    [Fact]
    public async Task TestTopicMessage()
    {
        // Arrange
        HttpClient httpClient = _webApplicationFactory.CreateClient();

        // Act
        HttpResponseMessage responseMessage = await httpClient.PostAsync(
            "api/send/topic",
            null);

        // Assert
        responseMessage.EnsureSuccessStatusCode();

        CloudEvent message = await _serviceBusFixture.ConsumeMessageAsync<CloudEvent>(
            "testtopic",
            "testsubscription");
        Assert.NotNull(message);
        Assert.NotNull(message.Data);

        MessageArgs messageArgs = message.Data.ToObjectFromJson<MessageArgs>();
        Assert.NotNull(messageArgs);

        Assert.Equal(
            "Hello World!",
            messageArgs.Message);
    }

    [Fact]
    public async Task TestTopicWithRuleMessage()
    {
        // Arrange
        HttpClient httpClient = _webApplicationFactory.CreateClient();

        // Act
        HttpResponseMessage responseMessage = await httpClient.PostAsync(
            "api/send/topic",
            null);

        // Assert
        responseMessage.EnsureSuccessStatusCode();

        CloudEvent message = await _serviceBusFixture.ConsumeMessageAsync<CloudEvent>(
            "testtopic",
            "testsubscriptionwithrule");
        Assert.NotNull(message);
        Assert.NotNull(message.Data);

        MessageArgs messageArgs = message.Data.ToObjectFromJson<MessageArgs>();
        Assert.NotNull(messageArgs);

        Assert.Equal(
            "Hello World!",
            messageArgs.Message);
    }

    [Fact]
    public async Task TestProcessQueueMessage()
    {
        // Arrange
        await _serviceBusFixture.PublishMessageAsync(
            "testqueue",
            new ServiceBusMessage("Hello World!"));
        HttpClient httpClient = _webApplicationFactory.CreateClient();

        // Act
        HttpResponseMessage responseMessage = await httpClient.PostAsync(
            "api/process/queue",
            null);

        // Assert
        responseMessage.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TestProcessTopicMessage()
    {
        // Arrange
        await _serviceBusFixture.PublishMessageAsync(
            "testqueue",
            new ServiceBusMessage("Hello World!"));
        HttpClient httpClient = _webApplicationFactory.CreateClient();

        // Act
        HttpResponseMessage responseMessage = await httpClient.PostAsync(
            "api/process/queue",
            null);

        // Assert
        responseMessage.EnsureSuccessStatusCode();
    }

    private static ServiceBusConfiguration ServiceBusConfig()
        => ServiceBusConfigurationBuilder
            .Create()
            .AddDefaultNamespace(
                namespaceConfigBuilder => namespaceConfigBuilder
                    .AddQueue("testqueue")
                    .AddTopic(
                        "testtopic",
                        topicBuilder => topicBuilder
                            .AddSubscription(
                                "testsubscription")
                            .AddSubscription(
                                "testsubscriptionwithrule",
                                subscriptionBuilder => subscriptionBuilder
                                    .AddRule(
                                        "testrule",
                                        ruleBuilder => ruleBuilder
                                            .WithFilterType(RuleFilterType.Correlation)
                                            .WithCorrelationFilter(
                                                correlationFilterBuilder => correlationFilterBuilder
                                                    .WithCloudEventsContentType()
                                                    .WithProperties(
                                                        "MyProperty",
                                                        "MyValue"))))))
            .Build();
}