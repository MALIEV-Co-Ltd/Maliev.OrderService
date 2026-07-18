namespace Maliev.OrderService.Tests.Testing
{
    /// <summary>
    /// Shared helpers for deterministic eventual-consistency assertions in tests.
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Waits until the supplied condition returns a successful result.
        /// </summary>
        /// <typeparam name="T">The result type returned by the condition.</typeparam>
        /// <param name="condition">The asynchronous condition to evaluate.</param>
        /// <param name="isSuccess">Predicate that determines whether the result is acceptable.</param>
        /// <param name="timeout">Maximum time to wait.</param>
        /// <param name="pollInterval">Delay between attempts.</param>
        /// <param name="failureMessage">Message used when the condition does not succeed before timeout.</param>
        /// <param name="cancellationToken">A token used to cancel the wait.</param>
        /// <returns>The first successful result.</returns>
        /// <exception cref="TimeoutException">Thrown when the timeout expires before the condition succeeds.</exception>
        public static async Task<T> WaitForAsync<T>(
            Func<CancellationToken, Task<T>> condition,
            Func<T, bool> isSuccess,
            TimeSpan timeout,
            TimeSpan pollInterval,
            string failureMessage,
            CancellationToken cancellationToken = default)
        {
            DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
            T? lastResult = default;

            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lastResult = await condition(cancellationToken);
                if (isSuccess(lastResult))
                {
                    return lastResult;
                }

                TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                await Task.Delay(
                    remaining < pollInterval ? remaining : pollInterval,
                    cancellationToken);
            }

            throw new TimeoutException($"{failureMessage} Last result: {lastResult}");
        }

        /// <summary>
        /// Waits until the supplied condition returns <see langword="true"/>.
        /// </summary>
        /// <param name="condition">The asynchronous boolean condition to evaluate.</param>
        /// <param name="timeout">Maximum time to wait.</param>
        /// <param name="pollInterval">Delay between attempts.</param>
        /// <param name="failureMessage">Message used when the condition does not succeed before timeout.</param>
        /// <param name="cancellationToken">A token used to cancel the wait.</param>
        /// <returns>A task that completes when the condition succeeds.</returns>
        public static async Task WaitForAsync(
            Func<CancellationToken, Task<bool>> condition,
            TimeSpan timeout,
            TimeSpan pollInterval,
            string failureMessage,
            CancellationToken cancellationToken = default)
        {
            _ = await WaitForAsync(
                condition,
                static result => result,
                timeout,
                pollInterval,
                failureMessage,
                cancellationToken);
        }
    }
}
