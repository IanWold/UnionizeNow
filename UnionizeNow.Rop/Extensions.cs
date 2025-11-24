namespace UnionizeNow.Rop;

public static class LinqExtensions {
    extension (Result result) {
        public Result Select(Func<object?, object?> _) =>
            result;

        public Result SelectMany(Func<object?, Result> bind) => result switch {
            ISuccess => bind(null),
            Result.Failure f => f
        };

        public Result SelectMany(Func<object?, Result> bind, Func<object?, object?, object?> _) => result switch {
            ISuccess => bind(null),
            Result.Failure f => f
        };
    }

    extension <TOut>(Result result) {
        public Result<TOut> Select(Func<TOut> selector) => result switch {
            ISuccess => selector(),
            IFailure f => f.Error
        };

        public Result<TOut> Select(Func<object?, TOut> selector) => result switch {
            ISuccess => selector(null),
            IFailure f => f.Error
        };

        public Result<TOut> SelectMany(Func<object?, Result<TOut>> bind) => result switch {
            ISuccess => bind(null),
            IFailure f => f.Error
        };
    }

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

    extension <TMiddle, TOut>(Result result) {
        public Result<TOut> SelectMany(Func<object?, Result<TMiddle>> bind, Func<object?, TMiddle, TOut> project) => result switch {
            ISuccess => bind(null).Select(m => project(null, m)),
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
    extension (Result result) {
        public Result Require(bool predicate, Func<ResultFailure> onError) => result switch {
            Result.Success s => predicate ? s : onError(),
            Result.Failure f => f
        };

        public Result OnSuccess(Action action) {
            if (result is Result.Success) {
                action();
            }

            return result;
        }

        public Result OnFailure(Action<ResultFailure> action) {
            if (result is Result.Failure failure) {
                action(failure.Error);
            }

            return result;
        }
    }

    extension <T>(Result<T> result) {
        public Result<T> Require(Func<T, bool> predicate, Func<T, ResultFailure> onError) => result switch {
            Result<T>.Success s => predicate(s.Value) ? s : onError(s.Value),
            Result<T>.Failure f => f
        };

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
