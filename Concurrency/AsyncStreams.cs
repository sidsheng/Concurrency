using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency
{
    public class AsyncStreams
    {
        public async Task Start(int testNumber)
        {
            #region 2

            if (testNumber == 2)
            {
                await ProcessValueAsync();
            }

            #endregion

            #region 3

            if (testNumber == 3)
            {
                await WhereAwaitExample();
                await WhereExample();
                await CountAsyncExample();
                await CountAsyncAwaitExample();
            }

            #endregion

            #region 4

            if (testNumber == 4)
            {
                try
                {
                    await AsyncStreamCancel();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                try
                {
                    await ConsumeSequence(SlowRangeCancel());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            #endregion
        }

        #region 1 Creating Async Streams
        
        async IAsyncEnumerable<int> GetValuesAsync1()
        {
            await Task.Delay(1000);
            yield return 182;
            await Task.Delay(1000);
            yield return 187;
        }

        /// <summary>
        /// Example URL only (do not test)
        /// </summary>
        async IAsyncEnumerable<string> GetValuesAsync2(HttpClient client)
        {
            int offset = 0;
            const int limit = 10;
            while (true)
            {
                string result = await client.GetStringAsync(
                    $"https://example.com/api/values?offset={offset}&limit={limit}");
                string[] values = result.Split('\n');

                foreach (string value in values)
                {
                    yield return value;
                }

                if (values.Length != limit)
                    break;
                
                offset += limit;
            }
        }

        #endregion

        #region 2 Consuming Async Streams

        async Task ProcessValueAsync()
        {
            await foreach (var value in GetValuesAsync1().ConfigureAwait(false))
            {
                await Task.Delay(100).ConfigureAwait(false);
                Console.WriteLine(value);
            }
        }
        
        #endregion

        #region 3 LINQ with Async Streams

        async IAsyncEnumerable<int> SlowRange()
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(i * 100);
                yield return i;
            }
        }

        async Task WhereAwaitExample()
        {
            IAsyncEnumerable<int> values = SlowRange().WhereAwait(
                async value =>
                {
                    await Task.Delay(10);
                    return value % 2 == 0;
                }
            );

            await foreach (int result in values)
            {
                Console.WriteLine($"WhereAwaitExample {result}");
            }
        }

        async Task WhereExample()
        {
            IAsyncEnumerable<int> values = SlowRange().Where(
                value => value % 2 == 0);

            await foreach (int result in values)
            {
                Console.WriteLine($"WhereExample {result}");
            }
        }

        async Task CountAsyncExample()
        {
            int count = await SlowRange().CountAsync(
                value => value % 2 == 0);
            Console.WriteLine($"CountAsyncExample {count}");
        }

        async Task CountAsyncAwaitExample()
        {
            int count = await SlowRange().CountAwaitAsync(
                async value =>
                {
                    await Task.Delay(10);
                    return value % 2 == 0;
                });
            Console.WriteLine($"CountAsyncExample {count}");
        }

        #endregion

        #region 4 Cancellation with Async Streams

        async Task AsyncStreamCancel()
        {
            using var cts = new CancellationTokenSource(500);
            CancellationToken token = cts.Token;
            await foreach (int result in SlowRangeCancel(token))
            {
                Console.WriteLine(result);
            }
        }

        async IAsyncEnumerable<int> SlowRangeCancel([EnumeratorCancellation] CancellationToken token = default)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(i * 100, token);
                yield return i;
            }
        }

        async Task ConsumeSequence(IAsyncEnumerable<int> items)
        {
            using var cts = new CancellationTokenSource(500);
            var token = cts.Token;
            await foreach (int result in items.WithCancellation(token))
            {
                Console.WriteLine(result);
            }
        }

        #endregion
    }
}