using TestBenchApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<QueueSender>();
builder.Services.AddSingleton<TopicSender>();
builder.Services.AddSingleton<TopicProcessor>();
builder.Services.AddSingleton<QueueProcessor>();

WebApplication app = builder.Build();
app.MapPost("/api/send/queue", async (QueueSender sender) =>
{
    await sender.SendMessageAsync();
    return Results.Ok();
});

app.MapPost("/api/send/topic", async (TopicSender sender) =>
{
    await sender.SendMessageAsync();
    return Results.Ok();
});

app.MapPost("/api/process/topic", async (TopicProcessor processor) =>
{
    await processor.ProcessMessageAsync();
    return Results.Ok();
});

app.MapPost("/api/process/queue", async (QueueProcessor processor) =>
{
    await processor.ProcessMessageAsync();
    return Results.Ok();
});

app.Run();
