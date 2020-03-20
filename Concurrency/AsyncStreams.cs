using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        }

        #region 1
        
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

        #region 2

        async Task ProcessValueAsync()
        {
            await foreach (var value in GetValuesAsync1().ConfigureAwait(false))
            {
                await Task.Delay(100).ConfigureAwait(false);
                Console.WriteLine(value);
            }
        }
        
        #endregion
    }
}