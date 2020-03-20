using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            #region 1 Delay, retries, timeout

            if (testNumber == 1)
            {
                await DelayResult<int>(182, TimeSpan.FromSeconds(1));
                string result1 = await DoStuffWithRetries();
                string result2 = await DoStuffWithTimeout();
            }

            #endregion

            #region 2 Completed tasks

            if (testNumber == 2)
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

            #region 3 Progress updates

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

            #region 4 WhenAll
        
            if (testNumber == 4)
            {
                await ObserveOneExceptionAsync();
                await ObserverAllExceptionsAsync();
            }

            #endregion
        
            #region 5 WhenAny

            if (testNumber == 5)
            {
                int length = await FirstRespondingUrlAsync(new HttpClient(), "https://www.google.com.au", "https://www.google.com.au");
                Console.WriteLine($"FirstRespondingUrlAsync: {length}");
            }

            #endregion
        
            #region 6 Processing Tasks as they complete

            if (testNumber == 6)
            {
                await ProcessTasksAsync1();
                await ProcessTasksAsync2();
            }

            #endregion
        
            #region 7 Context via ConfigureAwait

            if (testNumber == 7)
            {
                await ResumeOnContextAsync();
                await ResumeWithoutContextAsync();
            }

            #endregion

            #region 8 Catching exceptions

            if (testNumber == 8)
            {
                await TestAsync1();
                await TestAsync2();
            }

            #endregion

            #region 9 Async void: avoid - skipped
            #endregion

            #region 10

            if (testNumber == 10)
            {
                await MethodAsync1();
                await MethodAsync2();
            }

            #endregion

            #region 11

            if (testNumber == 11)
            {
                await ConsumingMethodAsync1();
                await ConsumingMethodAsync2();
                await ConsumingMethodAsync3();
                await ConsumingMethodAsync4();
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

        #region 4

        async Task WhenAllTest()
        {
            Task task1 = Task.Delay(TimeSpan.FromSeconds(1));
            Task task2 = Task.Delay(TimeSpan.FromSeconds(2));
            Task task3 = Task.Delay(TimeSpan.FromSeconds(1));

            await Task.WhenAll(task1, task2, task3);

            Task<int> task4 = Task.FromResult(1);
            Task<int> task5 = Task.FromResult(2);
            Task<int> task6 = Task.FromResult(3);

            int[] results = await Task.WhenAll(task4, task5, task6);
        }

        async Task<string> DownloadAllAsync(HttpClient client, IEnumerable<string> urls)
        {
            var downloads = urls.Select(urls => client.GetStringAsync(urls));
            Task<string>[] downloadTasks = downloads.ToArray();

            string[] htmlPages = await Task.WhenAll(downloadTasks);

            return string.Concat(htmlPages);
        }

        async Task ThrowNotImplementedExceptionAsync()
        {
            await Task.Delay(10);
            throw new NotImplementedException();
        }

        async Task ThrowInvalidOperationExceptionAsync()
        {
            await Task.Delay(10);
            throw new InvalidOperationException();
        }

        async Task ObserveOneExceptionAsync()
        {
            var task1 = ThrowNotImplementedExceptionAsync();
            var task2 = ThrowInvalidOperationExceptionAsync();

            try
            {
                await Task.WhenAll(task1, task2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ObserveOneExceptionAsync(): {ex}");
            }
        }

        async Task ObserverAllExceptionsAsync()
        {
            var task1 = ThrowNotImplementedExceptionAsync();
            var task2 = ThrowInvalidOperationExceptionAsync();

            Task allTasks = Task.WhenAll(task1, task2);
            try
            {
                await allTasks;
            }
            catch
            {
                AggregateException allExceptions = allTasks.Exception;
                foreach (var ex in allExceptions.InnerExceptions)
                {
                    Console.WriteLine($"ObserverAllExceptionsAsync(): {ex}");
                }
            }
        }

        #endregion

        #region 5

        async Task<int> FirstRespondingUrlAsync(HttpClient client, string urlA, string urlB)
        {
            Task<byte[]> downloadTaskA = client.GetByteArrayAsync(urlA);
            Task<byte[]> downloadTaskB = client.GetByteArrayAsync(urlB);

            Task<byte[]> completedTask = await Task.WhenAny(downloadTaskA, downloadTaskB);
            byte[] data = await completedTask;
            
            return data.Length;
        }

        #endregion

        #region 6

        async Task<int> DelayAndReturnAsync(int value)
        {
            await Task.Delay(TimeSpan.FromSeconds(value));
            return value;
        }

        async Task AwaitAndProcessAsync(Task<int> task)
        {
            int result = await task;
            Console.WriteLine($"AwaitAndProcessAsync: {result}");
        }

        async Task ProcessTasksAsync1()
        {
            Task<int> taskA = DelayAndReturnAsync(2);
            Task<int> taskB = DelayAndReturnAsync(3);
            Task<int> taskC = DelayAndReturnAsync(1);
            Task<int>[] tasks = new[] { taskA, taskB, taskC };

            IEnumerable<Task> taskQuery =
                from t in tasks
                select AwaitAndProcessAsync(t);

            Task[] processingTasks = taskQuery.ToArray();
            await Task.WhenAll(processingTasks);
        }

        async Task ProcessTasksAsync2()
        {
            Task<int> taskA = DelayAndReturnAsync(2);
            Task<int> taskB = DelayAndReturnAsync(3);
            Task<int> taskC = DelayAndReturnAsync(1);
            Task<int>[] tasks = new[] { taskA, taskB, taskC };

            Task[] processingTasks = tasks.Select(async t =>
            {
                int result = await t;
                Console.WriteLine($"ProcessTasksAsync2: {result}");
            }).ToArray();

            await Task.WhenAll(processingTasks);
        }

        #endregion
    
        #region 7

        async Task ResumeOnContextAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            // resumes on same context
        }

        async Task ResumeWithoutContextAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

            // discards context when it resumes
        }

        #endregion
    
        #region 8

        async Task ThrowExceptionAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            throw new InvalidOperationException("test");
        }

        async Task TestAsync1()
        {
            try
            {
                await ThrowExceptionAsync();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("TestAsync1: Exception caught");
            }
        }

        async Task TestAsync2()
        {
            Task task = ThrowExceptionAsync();
            try
            {
                await task;
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("TestAsync2: Exception caught");
            }
        }

        #endregion

        #region 10

        async ValueTask<int> MethodAsync1()
        {
            await Task.Delay(100);
            return 182;
        }

        async Task<int> SlowMethodAsync2()
        {
            await Task.Delay(100);
            return 182;
        }
        
        ValueTask<int> MethodAsync2()
        {
            bool canBehaveSynchronously = true;
            if (canBehaveSynchronously)
            {
                return new ValueTask<int>(182);
            }

            return new ValueTask<int>(SlowMethodAsync2());
        }

        #endregion

        #region 11

        async Task ConsumingMethodAsync1()
        {
            int value = await MethodAsync2();
            Console.WriteLine($"ConsumingMethodAsync1: {value}");
        }

        async Task ConsumingMethodAsync2()
        {
            ValueTask<int> valueTask = MethodAsync2();
            int value = await valueTask;
            Console.WriteLine($"ConsumingMethodAsync2: {value}");
        }

        async Task ConsumingMethodAsync3()
        {
            Task<int> task = MethodAsync2().AsTask();
            int value = await task;
            int anotherValue = await task;
            Console.WriteLine($"ConsumingMethodAsync3: {value}, {anotherValue}");
        }

        async Task ConsumingMethodAsync4()
        {
            Task<int> task1 = MethodAsync2().AsTask();
            Task<int> task2 = MethodAsync2().AsTask();
            int[] results = await Task.WhenAll(task1, task2);
            Console.WriteLine($"ConsumingMethodAsync4: {results[0]}, {results[1]}");
        }

        #endregion
    }
}
