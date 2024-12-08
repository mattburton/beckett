using System.Text.Json.Serialization;
using TodoList.GetList;

namespace API.TodoList;

public static class Routes
{
    public static RouteGroupBuilder TodoListRoutes(this RouteGroupBuilder builder) =>
        builder
            .AddItemRoute()
            .CompleteItemRoute()
            .CreateListRoute()
            .GetListRoute();
}

[JsonSerializable(typeof(AddItemRequest))]
[JsonSerializable(typeof(AddItemResponse))]
[JsonSerializable(typeof(CompleteItemResponse))]
[JsonSerializable(typeof(CreateListRequest))]
[JsonSerializable(typeof(CreateListResponse))]
[JsonSerializable(typeof(TodoListView))]
internal partial class TodoListApiJsonSerializerContext : JsonSerializerContext;
