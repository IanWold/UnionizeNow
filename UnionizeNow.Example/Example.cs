namespace UnionizeNow.Example;

public abstract partial record Side : IUnionizeNow {
    public partial record Unionist(string Name);
    public partial record Scab;
}

public abstract partial record Option<T> : IUnionizeNow {
    public partial record Some(T Result);
    public partial record None;
}

public static class Class1 {
    public static Side ChooseSide(string? name = null) =>
        string.IsNullOrWhiteSpace(name)
        ? new Scab()
        : new Side.Unionist(name);

    // This will trigger compiler warning:
    public static bool IsUnionistIncomplete() => ChooseSide() switch {
        IScab => true
    };

    public static bool IsUnionist() => ChooseSide("Eugene") switch {
        IUnionist => true,
        Side.Scab => false
    };

    public static Option<int> Divide(int first, int second) => second switch {
        0 => new None(),
        _ => new Option<int>.Some(first / second)
    };

    public static bool CanDivide(int first, int second) => Divide(first, second) switch {
        ISome<int> => true,
        Option<int>.None => false
    };
}
