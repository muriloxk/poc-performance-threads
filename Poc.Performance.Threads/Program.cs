using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Poc.Performance.Threads
{
    class Program
    {
        static void Main(string[] args)
        {
            //CalcularPerformance(PrimesInRange);
            //63905 prime number found in 123 seconds (4 processors)

            //CalcularPerformance(PrimesInRangeThreadWithLock);
            //63905 prime number found in 50 seconds (4 processors)

            //CalcularPerformance(PrimesInRangeThreadWithOutLock); 
            //63905 prime number found in 49 seconds(4 processors)

            //CalcularPerformance(PrimesInRangeThreadWithInterLocked);
            //63905 prime number found in 53 seconds(4 processors)

            //CalcularPerformance(PrimesInRangeWithThreadPool);
            //63905 prime number found in 32 seconds (4 processors)

            //CalcularPerformance(PrimesInRangeWithTPL);
            //63905 prime number found in 41 seconds (4 processors)

            CalcularPerformance(PrimesInRangeWithTasks);
            //63905 prime number found in 32 seconds (4 processors)
        }

        private static void CalcularPerformance(Func<long, long, long> codigo)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = codigo(200, 800_000);
            sw.Stop();

            Console.WriteLine($"{result} prime number found in {sw.ElapsedMilliseconds / 1000} seconds ({Environment.ProcessorCount} processors)");
        }

        private static async Task CalcularPerformanceAsync(Func<long, long, Task<long>> codigo)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = await codigo(200, 800_000);
            sw.Stop();

            Console.WriteLine($"{result} prime number found in {sw.ElapsedMilliseconds / 1000} seconds ({Environment.ProcessorCount} processors)");
        }

        static long PrimesInRange(long start, long end)
        {
            long result = 0;
            for(var number = start; number < end; number++)
            {
                if (IsPrime(number))
                    result++;
            }

            return result;
        }

        static long PrimesInRangeThreadWithLock(long start, long end)
        {
            long result = 0;
            var lockObject = new object();

            var range = end - start;
            var numberOfThreads = (long)Environment.ProcessorCount;

            var threads = new Thread[numberOfThreads];
            var chunkSize = range / numberOfThreads;

            for(long i = 0; i < numberOfThreads; i++)
            {
                var chunkStart = start + i * chunkSize;
                var chunkEnd = (i == (numberOfThreads - 1)) ? end : chunkStart + chunkSize;
                
                threads[i] = new Thread(() =>
                {
                    for (var number = chunkStart; number < chunkEnd; ++number)
                    {
                        if (IsPrime(number))
                            lock (lockObject)
                                result++;
                    }
                });

                threads[i].Start();
            }

            foreach (var thread in threads)
                thread.Join();

            return result;
        }


        static long PrimesInRangeThreadWithOutLock(long start, long end)
        {
        
            var lockObject = new object();

            var range = end - start;
            var numberOfThreads = (long)Environment.ProcessorCount;
            var results = new long[numberOfThreads];

            var threads = new Thread[numberOfThreads];
            var chunkSize = range / numberOfThreads;

            for (long i = 0; i < numberOfThreads; i++)
            {
                var chunkStart = start + i * chunkSize;
                var chunkEnd = (i == (numberOfThreads - 1)) ? end : chunkStart + chunkSize;
                var current = i;

                threads[i] = new Thread(() =>
                {
                    results[current] = 0;
                    for (var number = chunkStart; number < chunkEnd; ++number)
                    {
                        if (IsPrime(number))
                           results[current]++;
                    }
                });

                threads[i].Start();
            }

            foreach (var thread in threads)
                thread.Join();

            return results.Sum();
        }


        static long PrimesInRangeThreadWithInterLocked(long start, long end)
        {
            long result = 0;
            var lockObject = new object();

            var range = end - start;
            var numberOfThreads = (long)Environment.ProcessorCount;
       
            var threads = new Thread[numberOfThreads];
            var chunkSize = range / numberOfThreads;

            for (long i = 0; i < numberOfThreads; i++)
            {
                var chunkStart = start + i * chunkSize;
                var chunkEnd = (i == (numberOfThreads - 1)) ? end : chunkStart + chunkSize;

                threads[i] = new Thread(() =>
                {
                    for (var number = chunkStart; number < chunkEnd; ++number)
                    {
                        if (IsPrime(number))
                            Interlocked.Increment(ref result);                              
                    }
                });

                threads[i].Start();
            }

            foreach (var thread in threads)
                thread.Join();

            return result;
        }

        static long PrimesInRangeWithThreadPool(long start, long end)
        {
            long result = 0;
            const long chunkSize = 100;
            var completed = 0;
            var allDone = new ManualResetEvent(initialState: false);

            var chunks = (end - start) / chunkSize;

            for(long i = 0; i < chunks; i++)
            {
                var chunkStart = (start) + i * chunkSize;
                var chunkEnd = i == (chunks - 1) ? end : chunkStart + chunkSize;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    for (var number = chunkStart; number < chunkEnd; number++)
                    {
                        if (IsPrime(number))
                            Interlocked.Increment(ref result);
                    }

                    if (Interlocked.Increment(ref completed) == chunks)
                        allDone.Set();
                });
            }

            allDone.WaitOne();
            return result;
        }

        public static long PrimesInRangeWithTPL(long start, long end)
        {
            long result = 0;

            var locked = new object();

            Parallel.For(start, end, number =>
            {
                if(IsPrime(number))
                {
                    lock(locked)
                        result++;
                }
            });

            return result;
        }


        public static long PrimesInRangeWithTasks(long start, long end)
        {
            long result = 0;
            const long chunkSize = 100;
            var chunks = (end - start) / chunkSize;

            var locked = new object();
            Task[] tasks = new Task[chunks];

            for (long i = 0; i < chunks; i++)
            {
                var chunkStart = (start) + i * chunkSize;
                var chunkEnd = i == (chunks - 1) ? end : chunkStart + chunkSize;

                tasks[i] = Task.Factory.StartNew(() =>
                {
                    for (var number = chunkStart; number < chunkEnd; number++)
                    {
                        if (IsPrime(number))
                            lock (locked)
                                result++;
                    }
                });
            }

            Task.WaitAll(tasks);

            return result;
        }

        static bool IsPrime(long number)
        {
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            for(long divisor = 3; divisor < (number / 2); divisor += 2)
            {
                if (number % divisor == 0)
                    return false;
            }

            return true;
        }
    }
}
