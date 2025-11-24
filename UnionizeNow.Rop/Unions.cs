namespace UnionizeNow.Rop;

public abstract record ResultFailure(string? Message);

public sealed record Error(string? Message) : ResultFailure(Message);
public sealed record AggregateError(IEnumerable<ResultFailure> Aggregate) : ResultFailure("One or more failures returned. Expand the aggregate to see all errors.");
public sealed record FromException(Exception Exception) : ResultFailure(Exception.Message);

public sealed record Unit;

public abstract partial record Result : IUnionizeNow {
    public partial record Success;

    public partial record Failure(ResultFailure Error);

    public static implicit operator Result(ResultFailure failureValue) => new Failure(failureValue);
}

public abstract partial record Result<T> : IUnionizeNow {
    public partial record Success(T Value);
    public partial record Failure(ResultFailure Error);

    public static implicit operator Result<T>(T successValue) => new Success<T>(successValue);
    public static implicit operator Result<T>(ResultFailure failureValue) => new Failure(failureValue);
}

public abstract partial record Option<T> : IUnionizeNow {
    public partial record Some(T Value);
    public partial record None;

    public static implicit operator Option<T>(T successValue) => new Some(successValue);
}
