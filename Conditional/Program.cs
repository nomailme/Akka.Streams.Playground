using System;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace Conditional
{
    internal class Program
    {
        private static IGraph<FlowShape<int, string>, NotUsed> CreateConditionalFlow()
        {
            var result = GraphDsl.Create(builder =>
            {
                var partitioner = builder.Add(new Partition<int>(2, x => IsEven(x) ? 0 : 1));
                var merge = builder.Add(new Merge<string>(2));

                var evenFlow = Flow.Create<int>().Select(x => $"{x} is even");
                var oddFlow = Flow.Create<int>().Select(x => $"{x} is odd");

                builder.From(partitioner).Via(evenFlow).To(merge.In(0));
                builder.From(partitioner).Via(oddFlow).To(merge.In(1));

                return new FlowShape<int, string>(partitioner.In, merge.Out);
            });
            return result;
        }

        private static bool IsEven(int arg)
        {
            return (arg % 2) switch

            {
                0 => true,
                _ => false
            };
        }

        private static async Task Main(string[] args)
        {
            var source = Source.From(Enumerable.Range(1, 10));
            var flow = CreateConditionalFlow();
            var sink = Sink.ForEach<string>(x => Console.WriteLine(x));

            var actorSystem = ActorSystem.Create("actorSystem");
            using (var materializer = actorSystem.Materializer())
            {
                await source.Via(flow).RunWith(sink, materializer);
            }

            Console.ReadKey();
        }
    }
}
