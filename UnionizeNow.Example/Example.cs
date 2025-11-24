namespace UnionizeNow.Example;

public abstract partial record Side : IUnionizeNow {
    public partial record Unionist(string Name);
    public partial record Scab;
}

public abstract partial record Option<T> : IUnionizeNow {
    public partial record Some(T Result);
    public partial record None;

    // Need to manually add this now; may generate in future
    public static implicit operator Option<T>(T result) => new Some(result);
}

public static class Class1 {
    public static Side ChooseSide(string? name = null) =>
        string.IsNullOrWhiteSpace(name)
        ? new Scab()
        : new Side.Unionist(name);

    // This will trigger compiler warning as the switch is incomplete:
    public static bool IsUnionistIncomplete() => ChooseSide() switch {
        IScab => true
    };

    // This triggers no warning! It's complete!
    // Further, this demonstrates you can use both the generated interface and the union.member notation:
    public static bool IsUnionist() => ChooseSide("Eugene") switch {
        IUnionist => true,
        Side.Scab => false
    };

    // The _ case demonstrates using the manually-added implicit conversion from T
    public static Option<int> Divide(int first, int second) => second switch {
        0 => new None(),
        _ => first / second
    };

    public static bool CanDivide(int first, int second) => Divide(first, second) switch {
        ISome<int> => true,
        Option<int>.None => false
    };
}
