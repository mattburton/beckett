using Beckett;
using TodoList;
using TodoList.Infrastructure.Database;

var builder = Host.CreateApplicationBuilder(args);

await builder.AddTodoListDatabase();

builder.AddBeckett().UseTodoListComponent();

var host = builder.Build();

host.Run();
