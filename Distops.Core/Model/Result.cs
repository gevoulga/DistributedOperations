// <copyright file="Result.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>


using System.Diagnostics;
using System.Reflection;

namespace Distops.Core.Model
{
    public abstract class Result<TSuccess, TError>
    {
        public static implicit operator Result<TSuccess, TError>(TError error) => new Error(error);

        public static implicit operator Result<TSuccess, TError>(TSuccess success) => new Success(success);

        private static Exception Capture(Exception error, int skipFrames = 1)
        {
            if (string.IsNullOrEmpty(error.StackTrace))
            {
                // this is a "cold" exception that was not thrown before, it carries no stack trace;
                // capture current stack trace (from app entry point to where the exception was caught and converted to Result).
                ExceptionUtilities.SetStackTrace(error, new StackTrace(skipFrames + 1, true));
                return error;
            }

            // this is a "hot" exception, it carries stack trace from where it has been thrown to where it's been caught;
            // additionally, capture current stack trace (from app entry point to where the exception was caught and converted to Result).
            var capture = new Exception("Captured error.", error);
            ExceptionUtilities.SetStackTrace(capture, new StackTrace(skipFrames + 1, true));
            return capture;
        }

        public TResult Switch<TResult>(Func<TSuccess, TResult> onSuccess, Func<TError, TResult> onError)
        {
            _ = onSuccess ?? throw new ArgumentNullException(nameof(onSuccess));
            switch (this)
            {
                case Error error:
                    return onError(error.Value);
                case Success success:
                    return onSuccess(success.Value);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // [ContractAnnotation("=> false, onsuccess:notnull, onerror:null")]
        // [ContractAnnotation("=> true, onsuccess:null, onerror:notnull")]
        public bool ExtractError(out TSuccess? onsuccess, out TError? onerror)
        {
            switch (this)
            {
                case Error error:
                    onsuccess = default;
                    onerror = error.Value;
                    return true;
                case Success success:
                    onsuccess = success.Value;
                    onerror = default;
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public sealed class Success : Result<TSuccess, TError>, IEquatable<Success>
        {
            public Success(TSuccess value)
            {
                Value = value;
            }

            public TSuccess Value { get; }

            #region Equality

            public bool Equals(Success? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return EqualityComparer<TSuccess>.Default.Equals(Value, other.Value);
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Success other && Equals(other);
            }

            public override int GetHashCode()
            {
                return EqualityComparer<TSuccess>.Default.GetHashCode(Value);
            }

            public static bool operator ==(Success? left, Success? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Success? left, Success? right)
            {
                return !Equals(left, right);
            }

            #endregion
        }

        public sealed class Error : Result<TSuccess, TError>, IEquatable<Error>
        {
            private readonly Exception? captured;

            public Error(TError error)
            {
                if (error == null)
                {
                    throw new ArgumentNullException(nameof(error));
                }

                if (error is Exception exception)
                {
                    captured = Capture(exception);
                }

                Value = error;
            }

            public TError Value { get; }

            public void Throw()
            {
                if (captured != null)
                {
                    // re-throw the captured error, producing stack trace from this point to where it's going to be caught.
                    throw new Exception("Rethrowing captured error.", captured);
                }

                throw new InvalidOperationException($"There is no captured Exception object to rethrow from this instance of Result. {ToString()}");
            }

            #region Equality

            public bool Equals(Error? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return EqualityComparer<TError>.Default.Equals(Value, other.Value);
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is Error other && Equals(other);
            }

            public override int GetHashCode()
            {
                return EqualityComparer<TError>.Default.GetHashCode(Value);
            }

            public static bool operator ==(Error? left, Error? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Error? left, Error? right)
            {
                return !Equals(left, right);
            }

            #endregion
        }

        internal static class ExceptionUtilities
        {
            private static readonly FieldInfo StackTraceStringFi = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly Type TraceFormatTi = Type.GetType("System.Diagnostics.StackTrace").GetNestedType("TraceFormat", BindingFlags.NonPublic);
            private static readonly MethodInfo TraceToStringMi = typeof(StackTrace).GetMethod("ToString", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { TraceFormatTi }, null);

            internal static void SetStackTrace(Exception target, StackTrace stack)
            {
                var getStackTraceString = TraceToStringMi.Invoke(stack, new[] { Enum.GetValues(TraceFormatTi).GetValue(0) });
                StackTraceStringFi.SetValue(target, getStackTraceString);
            }
        }
    }
}