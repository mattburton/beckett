using Beckett;
using TodoList;
using TodoList.Infrastructure.Database;

var builder = Host.CreateApplicationBuilder(args);

await builder.AddTodoListDatabase();

builder.AddBeckett().UseTodoListModule();

var host = builder.Build();

host.Run();
