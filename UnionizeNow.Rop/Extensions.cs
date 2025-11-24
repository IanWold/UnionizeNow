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
            Result<T>.Success s => predicate(s.Value) ? onError(s.Value) : s,
            Result<T>.Failure f => f
        };

        public Result<T> Require(Func<T, bool> predicate, Func<T, ResultFailure> onError) =>
            result.Ensure(t => !predicate(t), onError);

        public Option<T> ToOption() => result switch {
            ISuccess<T> s => s.Value,
            IFailure => new None()
        };

        public Result<T> OnSuccess(Action<T> action) {
            if (result is Result<T>.Success success) {
                action(success.Value);
            }

            return result;
        }

        public Result<T> OnFailure(Action<ResultFailure> action) {
            if (result is Result<T>.Failure failure) {
                action(failure.Error);
            }

            return result;
        }
    }

    extension <T>(Option<T> option) {
        public Result<T> ToResult(Func<Result<T>> onNone) => option switch {
            ISome<T> s => s.Value,
            INone => onNone()
        };
    }

    extension <T>(Result<Option<T>> result) {
        public Result<T> ToResult(Func<Result<T>> onNone) => result switch {
            ISuccess<Option<T>> s => s.Value.ToResult(onNone),
            IFailure f => f.Error
        };
    }
}
