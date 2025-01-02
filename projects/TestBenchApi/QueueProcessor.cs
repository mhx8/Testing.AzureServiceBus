﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;

namespace TestBenchApi;

public class QueueProcessor
{
    private readonly ServiceBusProcessor _serviceBusProcessor;
    private readonly ILogger _logger;

    public QueueProcessor(
        IAzureClientFactory<ServiceBusProcessor> serviceBusFactory,
        ILogger<QueueProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceBusFactory);

        _serviceBusProcessor = serviceBusFactory.CreateClient("QueueProcessor");
        _serviceBusProcessor.ProcessMessageAsync += MessageHandler;
        _serviceBusProcessor.ProcessErrorAsync += ErrorHandler;

        _logger = logger;
    }

    public async Task ProcessMessageAsync()
    {
        await _serviceBusProcessor.StartProcessingAsync();

        await Task.Delay(TimeSpan.FromSeconds(5));

        await _serviceBusProcessor.StopProcessingAsync();
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        _logger.LogInformation($"Received message: SequenceNumber:{args.Message.SequenceNumber} Body:{body}");
        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError($"Message handler encountered an exception {args.Exception}.");
        return Task.CompletedTask;
    }
}