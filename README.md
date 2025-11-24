<div align="center">
  <img src="https://raw.githubusercontent.com/IanWold/UnionizeNow/refs/heads/main/Icon.png" height="128">
  <h1>UnionizeNow</h1>
</div>


The C# language team has been discussing adding "discriminated unions," their term for [Algebraic Data Types](https://en.wikipedia.org/wiki/Algebraic_data_type) (ADTs), for quite a while now. It appears as though this feature may begin to enter the language as soon as the next version (C# 15; releasing November 2026). However, I am impatient though, and I want to unionize now! (hence the name of this repo). This repository demonstrates one way of using source generators and analyzers to hack discriminated unions into the language. Although union types are mostly favored by the C# community, I understand there's always an amount of office politics that go into new features, and I wouldn't want to wade into this area so I want to present this repo as a completely politically neutral example of _how_ to do this.

You can check out [the full example](https://github.com/IanWold/UnionizeNow/blob/main/UnionizeNow.Example/Example.cs) in this repo but I'll detail below everything I'm doing.

## Analyzer/Suppressor

I have [demonstrated before that result types are easy to implement](https://ian.wold.guru/Posts/roll_your_own_csharp_results.html); these result types get us most of the way where we want to be. I've used in production implementations like I describe in that article, and they have worked to great success! The thing that nags me though about these result types is they require a custom `Map` implementation instead of being able to use `switch`. Something like:

```csharp
var model = result.Map<MyModel>(
    success: o => MyModel.From(o),
    failure: m => // do something with message m
);
```

The contrived case doesn't read so bad but when real code happens the length becomes a bit burdensome. On the other hand, `switch` would be much easier but I'm not going to litter my code with a bunch of literally unreachable catchall cases:

```csharp
var model = result switch {
    Success<MyDao> o => MyModel.From(o),
    Failure { Message: var m } => // do something with message m
    _ => throw new Exception("Never going to reach")
};
```

So I thought "why don't I make an analyzer to suppress the compiler warning about exhaustiveness in switch expressions" and that turned out to be an almost good thought, because it was almost doable. It isn't doable because in my previous implementation, the individual result types do not inherit from the result object, they just have implicit conversions into the result type. Thus, the result object will never be of the individual result types so I can't switch on them.

To use `switch` I _require_ something of the form

```csharp
public abstract record Option<T> {
    public sealed record Some(T Value) : Option<T>;
    public sealed record None : Option<T>;
}
```

So this works then - my analyzer can detect that `Option<T>` has member-children `Some` and `None`, and I can disable the ehaustiveness check:

```csharp
bool IsSome<T>(Option<T> option) => option switch {
    Option<T>.Some => true,
    Option<T>.None => false
}
```

So as to prevent this analyzer from clashing with any cases this might match that aren't intended to be unions, I add a marker interface to act as a sort of "union card," to signify that the record is indeed in solidarity with what we're trying to do:

```csharp
public abstract record Option<T> : IUnionizeNow { ... }
```

## Source Generator

Alright, so the analyzer works! I still have one pet peeve, which is that I need to fully-qualify the name `Option<T>`, or whatever my union name is, each time I want to reference some union option. This makes it a bit clunky:

```csharp
public abstract record DatabaseResult<T> : IUnionizeNow {
    public sealed record Found(T Value) : DatabaseResult<T>;
    public sealed record NotFound : DatabaseResult<T>;
}

public DatabaseResult<ItemDao> GetItem(int id) {
    // Do some work

    if (dbResult is null) {
        return new DatabaseResult<ItemDao>.NotFound();
    }

    return new DatabaseResult<ItemDao>.Found(dao);
}

public bool HasItem(int id) => GetItem(id) switch {
    DatabaseResult<ItemDao>.Found => true,
    DatabaseResult<ItemDao>.NotFound => false
};
```

I get lost with the fully-qualified result name everywhere. What I really want is:

```csharp
public abstract record DatabaseResult<T> : IUnionizeNow {
    public sealed record Found(T Value) : DatabaseResult<T>;
    public sealed record NotFound : DatabaseResult<T>;
}

public DatabaseResult<ItemDao> GetItem(int id) {
    // Do some work

    if (dbResult is null) {
        return new NotFound();
    }

    return dao;
}

public bool HasItem(int id) => GetItem(id) switch {
    Found => true,
    NotFound => false
};
```

This isn't _quite_ going to be achievable, but I'm going to get close to this! Here's how I do.

First, we'll make the union records be partial so we can extend them with a source generator:

```csharp
public abstract partial record DatabaseResult<T> : IUnionizeNow {
    public partial record Found(T Value) : DatabaseResult<T>;
    public partial record NotFound : DatabaseResult<T>;
}
```

The first thing we need our generator to do is to duplicate the `Found` and `NotFound` result types at the namespace level so that I don't need to `DatabaseResult<T>.` into them. This will require that our generator also generates implicit conversions from these top-level types into the member types. Here's what I first got the generator outputting:

```csharp
namespace MyProjectNamespace {
    public abstract partial record DatabaseResult<T> : IUnionizeNow {
        public partial record Found(T Value);
        public partial record NotFound;

        public static implicit operator DatabaseResult<T>(MyProjectNamespace.Found<T> r) => new Found(r.Value);
        public static implicit operator DatabaseResult<T>(MyProjectNamespace.NotFound r) => new NotFound();
    }

    public record Found<T>(T Value);
    public record NotFound;
}
```

This makes it so that I don't need to fully qualify the result type name when I return a result, but now I need to account for the switch. This is where it gets difficult. There is simply no way to make the namespace-scoped result objects appear equivalent to the member-scoped ones. The only way to get close is to introduce a new type that is common to both. Hey, interface names only differ by an "I" right?

Alright, so we're generating interfaces now:

```csharp
namespace MyProjectNamespace {
    public abstract partial record DatabaseResult<T> : IUnionizeNow {
        public partial record Found(T Value) : IFound<T>;
        public partial record NotFound : INotFound;

        public static implicit operator DatabaseResult<T>(MyProjectNamespace.Found<T> r) => new Found(r.Value);
        public static implicit operator DatabaseResult<T>(MyProjectNamespace.NotFound r) => new NotFound();
    }

    public record Found<T>(T Value);
    public interface IFound<T> {
        T Value { get; }
    }

    public record NotFound;
    public interface INotFound {
    }
}
```

Aaaaand that lets me do the following:

```csharp
public abstract partial record DatabaseResult<T> : IUnionizeNow {
    public partial record Found(T Value) : DatabaseResult<T>;
    public partial record NotFound : DatabaseResult<T>;
}

public DatabaseResult<ItemDao> GetItem(int id) {
    // Do some work

    if (dbResult is null) {
        return new NotFound();
    }

    return new Found<ItemDao>(dao);
}

public bool HasItem(int id) => GetItem(id) switch {
    IFound => true,
    INotFound => false
};
```

Yep that's pretty dirty, but I did say we're hacking these in. Hold the line! I added a couple niceties, in particular I made it so that the client code doesn't need to have each member result implement the parent result type, that can be generated:

```csharp
// Client implements:
public abstract partial record DatabaseResult<T> : IUnionizeNow {
    public partial record Found(T Value);
    public partial record NotFound;
}

// This gets generated:
public abstract partial record DatabaseResult<T> : IUnionizeNow {
    public partial record Found(T Value) : DatabaseResult<T>, IFound<T>;
    public partial record NotFound : DatabaseResult<T>, INotFound;

    public static implicit operator DatabaseResult<T>(MyProjectNamespace.Found<T> r) => new Found(r.Value);
    public static implicit operator DatabaseResult<T>(MyProjectNamespace.NotFound r) => new NotFound();
}

public record Found<T>(T Value);
public interface IFound<T> {
    T Value { get; }
}

public record NotFound;
public interface INotFound {
}
```

And you know, that's pretty much what we want out of unions! The official implementation will, ideally, let us one-line all this:

```
public union DatabaseResult<T>(Found(T Value), NotFound);
```

But until then this is not so unweildy.

## Railway-Oriented Programming (ROP)

In [UnionizeNow.Rop](https://github.com/IanWold/UnionizeNow/tree/main/UnionizeNow.Rop) I've included an example of what a [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/) setup might look like using these unions.

Note that LINQ's `select`/`select many` are equivalent to functional bind operators (`>>=`) that are typically used in functional ROP, so implementing the LINQ methods for the `Result` union allows using the LINQ query syntax to chain business logic:

```csharp
public class ItemService(AuthService auth, ItemRepository repository) {
    static Result ValidateId(int id) =>
        id > 0
        ? new Success()
        : new ValidationError(nameof(id), "Id must be greater than 0");

    public Result<Item> GetItemById(int id, ClaimsPrincipal user) =>
        from _ in auth.Authorize(user)
        from _ in ValidateId(id)
        from maybeDao in repository.GetItem(id)
        from dao in maybeDao.RequireSome(() => new NotFoundError($"Unable to find item {id}."))
        from item in Item.From(dao)
        select item;
}
```