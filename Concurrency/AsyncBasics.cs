using System;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency
{
    #region 2

    interface IMyAsyncInterface
    {
        Task<int> GetValueAsync();

        Task DoSomethingAsync();
    }

    class MySynchronousImplementation : IMyAsyncInterface
    {
        public Task<int> GetValueAsync()
        {
            return Task.FromResult(182);
        }

        public Task DoSomethingAsync()
        {
            try
            {
                // do something
                _ = 1 + 2;

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        internal Task<T> NotImplementedAsync<T>()
        {
            return Task.FromException<T>(new NotImplementedException());
        }

        internal Task<int> GetValueAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            return Task.FromResult(182);
        }
    }

    #endregion

    public class AsyncBasics
    {
        public AsyncBasics()
        {
        }

        public async Task Start(int testNumber)
        {
            #region 1

            if (testNumber == 1)
            {
                await DelayResult<int>(182, TimeSpan.FromSeconds(1));
                string result1 = await DoStuffWithRetries();
                string result2 = await DoStuffWithTimeout();
            }

            #endregion

            #region 2

            if (testNumber == 1)
            {
                MySynchronousImplementation mySyncImp = new MySynchronousImplementation();

                int result3 = await mySyncImp.GetValueAsync();
                Console.WriteLine($"result = {result3}");

                await mySyncImp.DoSomethingAsync();
                Console.WriteLine("DoSomethingAsync()");

                try
                {
                    await mySyncImp.NotImplementedAsync<int>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NotImplementedAsync {ex}");
                }

                try
                {
                    var ct = new CancellationTokenSource();
                    ct.Cancel();
                    int result4 = await mySyncImp.GetValueAsync(ct.Token);
                    Console.WriteLine($"GetValueAsync{result4}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cancellation {ex}");
                }
            }

            #endregion

            #region 3

            if (testNumber == 3)
            {
                var progress = new Progress<double>();
                progress.ProgressChanged += (sender, args) =>
                {
                    Console.WriteLine($"% done: {args}");
                };
                await MyMethodAsync(progress);
            }

            #endregion
        }

        #region 1

        /// <summary>
        /// Simple Task.Delay()
        /// </summary>
        async Task<T> DelayResult<T>(T result, TimeSpan delay)
        {
            Console.WriteLine($"Delay for {delay}");
            await Task.Delay(delay);

            return result;
        }

        /// <summary>
        /// Exponential backoff
        /// </summary>
        async Task<string> DoStuffWithRetries()
        {
            TimeSpan delay = TimeSpan.FromSeconds(1);
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return TaskWhichThrowsException();
                }
                catch { }

                Console.WriteLine($"Delay for {delay}");
                await Task.Delay(delay);
                delay = delay * 2;
            }

            return "blink";
        }

        string TaskWhichThrowsException()
        {
            Console.WriteLine($"Throwing Exception {DateTime.Now}");
            throw new Exception("AsyncBasics Exception");
        }

        async Task<string> DoStuffWithTimeout()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            Task<string> work = DoTask();
            Task timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);

            Task completedTask = await Task.WhenAny(work, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Console.WriteLine("Cancelled");
                return null;
            }

            Console.WriteLine("Work done");
            return await work;
        }

        async Task<string> DoTask()
        {
            await Task.Delay(5000);
            return "blink";
        }

        #endregion

        #region 3

        async Task MyMethodAsync(IProgress<double> progress = null)
        {
            double percentComplete = 0;
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);
                percentComplete += 10;
                progress?.Report(percentComplete);
            }
        }

        #endregion

        
    }
}
