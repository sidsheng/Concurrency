﻿using System;

namespace Concurrency
{
    class Program
    {
        static void Main(string[] args)
        {
            //var asyncBasics = new AsyncBasics();
            //asyncBasics.Start(11).Wait();

            var asyncStreams = new AsyncStreams();
            asyncStreams.Start(2).Wait();
        }
    }
}
