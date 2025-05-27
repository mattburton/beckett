namespace API.TodoLists;

public static class Routes
{
    public static void MapTodoListRoutes(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/todo-lists")
            .With(AddItem.Route)
            .With(CompleteItem.Route)
            .With(CreateList.Route)
            .With(GetList.Route);
    }
}
