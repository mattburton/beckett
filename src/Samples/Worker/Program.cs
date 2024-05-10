using Beckett;
using MinimalApi.Infrastructure.Database;
using MinimalApi.TodoList;

var builder = Host.CreateApplicationBuilder(args);

await builder.AddTodoListDatabase();

builder.AddBeckett().UseTodoListModule();

var host = builder.Build();

host.Run();
