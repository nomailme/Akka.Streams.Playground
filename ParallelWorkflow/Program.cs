using System;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace ParallelWorkflow
{
    internal class Program
    {
        private static Flow<TIn, TOut, NotUsed> Balancer<TIn, TOut>(Flow<TIn, TOut, NotUsed> worker, int workerCount)
        {
            return Flow.FromGraph(GraphDsl.Create(b =>
            {
                var balancer = b.Add(new Balance<TIn>(workerCount, waitForAllDownstreams: true));
                var merge = b.Add(new Merge<TOut>(workerCount));

                for (var i = 0; i < workerCount; i++)
                {
                    b.From(balancer).Via(worker.Async()).To(merge);
                }

                return new FlowShape<TIn, TOut>(balancer.In, merge.Out);
            }));
        }

        private static async Task Main(string[] args)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var myJobs = Source.From<int>(Enumerable.Range(1, 10));
            var worker = Flow.Create<int>().SelectAsync(1,
                async j =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(random.Next(3)));
                    return $"Processed unit {j}";
                });

            var processedJobs = myJobs.Via(Balancer(worker, 3));
            var actorSystem = ActorSystem.Create("ActorSystem");
            using (var materializer = actorSystem.Materializer())
            {
                await processedJobs.RunWith(Sink.ForEach<string>(Console.WriteLine), materializer);
            }

            Console.ReadKey();
        }
    }
}
