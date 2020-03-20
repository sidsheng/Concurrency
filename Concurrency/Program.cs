using System;

namespace Concurrency
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncBasics asyncBasics = new AsyncBasics();
            asyncBasics.Start(6).Wait();
        }
    }
}
