using System.Threading.Tasks;
using Moq.Language;

namespace CodeOnlyTests
{
    public static class MoqExtensions
    {
        public static ISetupSequentialResult<Task<T>> ReturnsAsync<T>(this ISetupSequentialResult<Task<T>> setup, T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);

            return setup.Returns(tcs.Task);
        }
    }
}
