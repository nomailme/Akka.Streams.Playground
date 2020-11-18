using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace Conditional
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var source = Source.From(Enumerable.Range(1,10));
            var flow = CreateConditionalFlow();
            var sink = Sink.ForEach<string>(x=>Console.WriteLine(x));

            var actorSystem =ActorSystem.Create("actorSystem");
            using (var materializer =actorSystem.Materializer())
            {
                await source.Via(flow).RunWith(sink,materializer);
            }

            Console.ReadKey();

        }

        private static IGraph<FlowShape<int, string>, NotUsed> CreateConditionalFlow()
        {
            var result = GraphDsl.Create(builder =>
            {
                var partitioner = builder.Add(new Partition<int>(2, IsEven));
                var merge = builder.Add(new Merge<string>(2));

                var evenFlow = Flow.Create<int>().Select(x => $"{x} is even");
                var oddFlow = Flow.Create<int>().Select(x => $"{x} is odd");

                builder.From(partitioner).Via(evenFlow).To(merge.In(0));
                builder.From(partitioner).Via(oddFlow).To(merge.In(1));

                return new FlowShape<int,string>(partitioner.In,merge.Out);
            });
            return result;
        }

        private static int IsEven(int arg)
        {
            return (arg % 2) switch

            {
                0 => 0,
                _ => 1
            };
        }
    }
}
