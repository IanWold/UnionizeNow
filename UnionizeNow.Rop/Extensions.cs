namespace UnionizeNow.Rop;

public static class LinqExtensions {
    extension <TIn, TOut>(Result<TIn> result) {
        public Result<TOut> Select(Func<TIn, TOut> selector) => result switch {
            ISuccess<TIn> s => selector(s.Value),
            IFailure f => f.Error
        };

        public Result<TOut> SelectMany(Func<TIn, Result<TOut>> selector) => result switch {
            ISuccess<TIn> s => selector(s.Value),
            IFailure f => f.Error
        };
    }

    extension <TIn, TMiddle, TOut>(Result<TIn> result) {
        public Result<TOut> SelectMany(Func<TIn, Result<TMiddle>> bind, Func<TIn, TMiddle, TOut> project) => result switch {
            ISuccess<TIn> s => bind(s.Value).Select(m => project(s.Value, m)),
            IFailure f => f.Error
        };
    }
}

public static class RopExtensions {
    extension <T>(Result<T> result) {
        public Result<T> Ensure(Func<T, bool> predicate, Func<T, ResultFailure> onError) => result switch {
            Result<T>.Success s => predicate(s.Value) switch {
                true => onError(s.Value),
                false => s
            },
            Result<T>.Failure f => f
        };

        public Result<T> Require(Func<T, bool> predicate, Func<T, ResultFailure> onError) =>
            result.Ensure(t => !predicate(t), onError);

        public Option<T> ToOption() => result switch {
            ISuccess<T> s => s.Value,
            IFailure => new None()
        };

        public Result<T> Tap(Action<T> action) {
            if (result is Result<T>.Success success) {
                action(success.Value);
            }

            return result;
        }
    }

    extension <T>(Option<T> option) {
        public Result<T> RequireSome(Func<ResultFailure> onError) => option switch {
            ISome<T> s => s.Value,
            INone => onError()
        };
    }

    extension <T>(Result<Option<T>> result) {
        public Result<T> RequireSome(Func<ResultFailure> onError) => result switch {
            ISuccess<Option<T>> o => o.Value switch {
                ISome<T> s => s.Value,
                INone => onError()
            },
            IFailure f => f.Error
        };
    }
}
