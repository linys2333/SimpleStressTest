using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SimpleStressTest
{
    /// <summary>
    /// 简单的读写压测
    /// 不要以调试模式运行。调试状态下，vs会加载很多跟踪、诊断信息，影响实际耗时
    /// </summary>
    class Program
    {
        private static ILogger _Logger;
        private static ConcurrentBag<long> _Statistics = new ConcurrentBag<long>();

        private delegate void TestMethod(int threadIndex, int loopIndex, int id);
        private static TestMethod _TestMethod;

        // 测试对象
        private static TestObject _TestObject;

        static void Main(string[] args)
        {
            Console.WriteLine($"请选择测试方法：1-{nameof(TestWrite)} 2-{nameof(TestRead)}");
            var num = Console.ReadLine();
            switch (num)
            {
                case "1":
                    _TestMethod = TestWrite;
                    break;
                case "2":
                    _TestMethod = TestRead;
                    break;
                default:
                    Console.ReadLine();
                    return;
            }

            Init();
            
            // 线程配置
            var threadCount = 100;  // 线程数
            var loop = 100;  // 每个线程循环执行数

            // 压测开始
            Console.WriteLine("start");
            var sw = Stopwatch.StartNew();

            try
            {
                ThreadsStart(true, threadCount, loop);
            }
            catch (Exception e)
            {
                _Logger.LogError(e, e.Message);
            }

            sw.Stop();

            // 统计
            var sort = _Statistics.OrderBy(s => s).ToArray();
            var runCount = sort.Length;
            var sum = sw.ElapsedMilliseconds / 1000.0;
            var min = sort.First() / 1000.0;
            var max = sort.Last() / 1000.0;
            var avg = sort.Average() / 1000.0;
            var p50 = sort[runCount * 50 / 100 - 1] / 1000.0;
            var p90 = sort[runCount * 90 / 100 - 1] / 1000.0;
            var p95 = sort[runCount * 95 / 100 - 1] / 1000.0;
            var p99 = sort[runCount * 99 / 100 - 1] / 1000.0;

            _Logger.LogInformation($"{_TestMethod.Method.Name}测试结果\r\n{threadCount}个线程，每个请求{loop}次，总次数：{runCount}，耗时：{sum}s，最小：{min}s，最大：{max}s，平均：{avg}s，p50：{p50}s，p90：{p90}s，p95：{p95}s，p99：{p99}s，QPS：{runCount / sum}");

            Console.WriteLine("end");
            Console.ReadLine();
        }

        static void Init()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddNLog(new NLogProviderOptions {CaptureMessageTemplates = true, CaptureMessageProperties = true});
            LogManager.LoadConfiguration("nlog.config");
            _Logger = loggerFactory.CreateLogger("Test");

            // 测试对象初始化、预热
            _TestObject = new TestObject();
            _TestObject.Init();
        }

        /// <summary>
        /// 线程启动
        /// </summary>
        /// <param name="useThreadPool">是否使用线程池</param>
        /// <param name="threadCount">线程数</param>
        /// <param name="loop">循环执行数</param>
        public static void ThreadsStart(bool useThreadPool, int threadCount, int loop)
        {
            var count = threadCount * loop;
            
            var taskOption = useThreadPool ? TaskCreationOptions.None : TaskCreationOptions.LongRunning;

            var tasks = new List<Task>();
            for (var i = 0; i < threadCount; i++)
            {
                var index = i;
                var thread = Task.Factory.StartNew(() => Test(new { ThreadIndex = index, Loop = loop }), taskOption);
                tasks.Add(thread);
            }

            while (tasks.Any(t => !t.IsCompleted))
            {
                Console.WriteLine($"{(_Statistics.Count + 0.0) / count:P}");
                Thread.Sleep(100);
            }

            // 通过Thread创建线程，不使用线程池
            // var threads = new List<Thread>();
            //
            // for (var i = 0; i < threadCount; i++)
            // {
            //     var thread = new Thread(Test);
            //     thread.Start(new { ThreadIndex = i, Loop = loop });
            //     threads.Add(thread);
            // }
            //
            // while (threads.Any(t => t.IsAlive))
            // {
            //     Console.WriteLine($"{(_Statistics.Count + 0.0) / count:P}");
            //     Thread.Sleep(100);
            // }
        }

        public static void Test(dynamic obj)
        {
            var threadIndex = obj.ThreadIndex;
            var loop = obj.Loop;

            // 自定义的额外参数
            var id = 0;

            for (var i = 0; i < loop; i++)
            {
                _TestMethod(threadIndex + 1, i + 1, id + i);
            }
        }

        public static void TestWrite(int threadIndex, int loopIndex, int id)
        {
            var sw = Stopwatch.StartNew();

            // 调用测试对象写方法
            _TestObject.Write(id);

            sw.Stop();

            _Statistics.Add(sw.ElapsedMilliseconds);
            _Logger.LogInformation($"{nameof(TestWrite)}，第{threadIndex}个线程，第{loopIndex}次执行：{sw.ElapsedMilliseconds}ms");
        }

        public static void TestRead(int threadIndex, int loopIndex, int id)
        {
            var sw = Stopwatch.StartNew();

            // 调用测试对象读方法
            _TestObject.Read(id);

            sw.Stop();

            _Statistics.Add(sw.ElapsedMilliseconds);
            _Logger.LogInformation($"{nameof(TestRead)}，第{threadIndex}个线程，第{loopIndex}次执行：{sw.ElapsedMilliseconds}ms");
        }
    }
}
