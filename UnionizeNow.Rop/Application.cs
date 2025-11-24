using Microsoft.Extensions.Logging;

namespace UnionizeNow.Rop;

public record InvalidError(string Name, string Message) : ResultFailure(Message) {
    public static Result If(bool predicate, string name, string message) =>
        predicate
        ? new InvalidError(name, message)
        : new Success();
}

public record NotFoundError(string Message) : ResultFailure(Message);

public record Todo(
    int Id,
    string Title,
    IEnumerable<TodoItem> Items
);

public record TodoItem(
    int Id,
    string Name,
    bool IsCompleted
);

public class TodoService(ITodoRepository repository, ILogger<TodoService> logger) {
    public Result<Todo> GetById(int id) =>
        from _ in InvalidError.If(id < 1, nameof(id), "Id must be greater than 0.")
            .OnFailure(_ => logger.LogInformation("Invalid id {Id}", id))
        from todo in repository.GetById(id)
            .ToResult(() => new NotFoundError($"Unable to find Todo {id}."))
        select todo;

    public Result<int> Create(string title) =>
        from _ in InvalidError.If(string.IsNullOrWhiteSpace(title), nameof(title), "Must provide a title.")
            .OnFailure(_ => logger.LogInformation("Invalid title '{Title}'", title))
        from id in repository.Create(title)
        select id;

    public Result UpdateTitleById(int id, string title) =>
        from vId in InvalidError.If(id < 1, nameof(id), "Id must be greater than 0.")
            .OnFailure(_ => logger.LogInformation("Invalid id {Id}", id))
        from vTitle in InvalidError.If(string.IsNullOrWhiteSpace(title), nameof(title), "Must provide a title.")
        from result in repository.UpdateTitleById(id, title)
        select result;

    public Result DeleteById(int id) =>
        from _ in InvalidError.If(id < 1, nameof(id), "Id must be greater than 0.")
            .OnFailure(_ => logger.LogInformation("Invalid id {Id}", id))
        from result in repository.DeleteById(id)
        select result;
}

public class ItemService(IItemRepository repository, ILogger<ItemService> logger) {
    public Result<TodoItem> GetById(int id) =>
        from _ in InvalidError.If(id < 1, nameof(id), "Id must be greater than 0.")
            .OnFailure(_ => logger.LogInformation("Invalid id {Id}", id))
        from item in repository.GetById(id)
            .ToResult(() => new NotFoundError($"Unable to find Todo {id}."))
        select item;

    public Result<IEnumerable<TodoItem>> GetAllByTodoId(int id) =>
        from _ in InvalidError.If(id < 1, nameof(id), "Id must be greater than 0.")
            .OnFailure(f => logger.LogInformation("Invalid id {Id}", id))
        from items in repository.GetAllByTodoId(id)
            .Require(i => i.Any(), _ => new NotFoundError($"Unable to find any items for todo {id}."))
            .OnFailure(_ => logger.LogInformation("No items found for todo {Id}", id))
        select items;

    public Result<int> Create(string name) =>
        from _ in InvalidError.If(string.IsNullOrWhiteSpace(name), nameof(name), "Must provide a name.")
            .OnFailure(_ => logger.LogInformation("Invalid name '{Name}'", name))
        from id in repository.Create(name)
        select id;

    public Result UpdateCompletedById(int id, bool isCompleted) =>
        from vId in InvalidError.If(id < 1, nameof(id), "Id must be greater than 0.")
            .OnFailure(_ => logger.LogInformation("Invalid id {Id}", id))
        from result in repository.UpdateCompletedById(id, isCompleted)
        select result;

    public Result UpdateNameById(int id, string name) =>
        from vId in InvalidError.If(id < 1, nameof(id), "Id must be greater than 0.")
            .OnFailure(_ => logger.LogInformation("Invalid id {Id}", id))
        from vName in InvalidError.If(string.IsNullOrWhiteSpace(name), nameof(name), "Must provide a name.")
            .OnFailure(_ => logger.LogInformation("Invalid name '{Name}'", name))
        from result in repository.UpdateNameById(id, name)
        select result;

    public Result DeleteById(int id) =>
        from _ in InvalidError.If(id < 1, nameof(id), "Id must be greater than 0.")
            .OnFailure(_ => logger.LogInformation("Invalid id {Id}", id))
        from result in repository.DeleteById(id)
        select result;
}

public interface ITodoRepository {
    Result<Option<Todo>> GetById(int id);
    Result<int> Create(string title);
    Result UpdateTitleById(int id, string title);
    Result DeleteById(int id);
}

public interface IItemRepository {
    Result<Option<TodoItem>> GetById(int id);
    Result<IEnumerable<TodoItem>> GetAllByTodoId(int id);
    Result<int> Create(string name);
    Result UpdateCompletedById(int id, bool isCompleted);
    Result UpdateNameById(int id, string name);
    Result DeleteById(int id);
}
