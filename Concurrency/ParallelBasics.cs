using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency
{
    public class ParellelBasics
    {
        public ParellelBasics()
        {
        }

        public void Start(int testNumber)
        {
            #region 1

            if (testNumber == 1)
            {
                var list = new List<int>();
                list.Add(182);
                list.Add(187);

                PrintList1(list);
                DoStuffOnList1(list, true);
                DoStuffOnList1(list, false);
                PrintList2(list, new CancellationTokenSource(10).Token);
                int x = DoStuffOnList2(list, true);
                Console.WriteLine($"DoStuffOnList2(list, true): {x}");
                int y = DoStuffOnList2(list, false);
                Console.WriteLine($"DoStuffOnList2(list, true): {y}");
            }

            #endregion
        }

        #region 1

        void PrintList1(IEnumerable<int> list)
        {
            Parallel.ForEach(list, number => Console.WriteLine($"PrintList1: {number}"));
        }

        void DoStuffOnList1(IEnumerable<int> list, bool someCondition)
        {
            Parallel.ForEach(list, (number, state) =>
            {
                if (someCondition)
                {
                    // perform invert action on list
                    Console.WriteLine("DoStuffOnList1");
                }
                else
                {
                    // can run multiple times if already running (stops future loops)
                    Console.WriteLine("DoStuffOnList1 - Stop");
                    state.Stop();
                }
            });
        }

        void PrintList2(IEnumerable<int> list, CancellationToken token)
        {
            Parallel.ForEach(list, new ParallelOptions{ CancellationToken = token },
                async number => 
                {
                    await Task.Delay(20);
                    Console.WriteLine($"PrintList2: {number}");
                });
        }

        int DoStuffOnList2(IEnumerable<int> list, bool someCondition)
        {
            object mutex = new object();
            int cantDoStuffCount = 0;
            Parallel.ForEach(list, number =>
            {
                if (someCondition)
                {
                    // perform invert action on list
                    Console.WriteLine("DoStuffOnList2");
                }
                else
                {
                    lock (mutex)
                    {
                        cantDoStuffCount++;
                    }
                }
            });

            return cantDoStuffCount;
        }

        #endregion
    }
}