using Beckett;
using Beckett.Dashboard;
using Beckett.Database;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("HelloWorld")!;

await BeckettDatabase.Migrate(connectionString);

builder.Services.AddNpgsqlDataSource(connectionString, options => options.AddBeckett());

builder.Services.AddBeckettDashboard();

var beckett = builder.Services.AddBeckett(
    options => { options.WithSubscriptionGroup("HelloWorld"); }
);

beckett.Map<MessageReceived>("message_received");

beckett.AddSubscription("log-message")
    .Message<MessageReceived>()
    .Handler(
        (MessageReceived message, ILogger<MessageReceived> logger) =>
            logger.LogInformation("Received message: {Message}", message.Message)
    );

var app = builder.Build();

app.MapGet(
    "/send/{message}",
    async (string message, IMessageStore messageStore, CancellationToken cancellationToken) =>
    {
        await messageStore.AppendToStream(
            "HelloWorld",
            ExpectedVersion.Any,
            new MessageReceived(message),
            cancellationToken
        );

        return Results.Content("""
            <h3>Message sent successfully!</h3>
            <a href="http://localhost:5001/beckett" target="_blank">View Beckett Dashboard</a>
        """, "text/html");
    }
);

app.MapBeckettDashboard("/beckett");

app.Run();

public record MessageReceived(string Message);
