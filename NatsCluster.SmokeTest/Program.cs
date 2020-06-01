using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client.Rx.Ops;
using STAN.Client;
using STAN.Client.Rx;

namespace NatsCluster.SmokeTest
{
    public class Program
    {
        private const string Dc1 = "dc1";
        private const string Dc2 = "dc2";

        private readonly Dictionary<string, string> _natsClusterNodes = GetDockerNatsClusterNodes();

        private const string ClusterId = "test-cluster";

        private static Dictionary<string, string> GetDockerNatsClusterNodes() =>
            new Dictionary<string, string>
            {
                { Dc1, "nats://127.0.0.1:4221, nats://127.0.0.1:4222, nats://127.0.0.1:4223" },
                { Dc2, "nats://127.0.0.1:4224, nats://127.0.0.1:4225, nats://127.0.0.1:4226" }
            };

        public static void Main()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            
            Console.Out.WriteLine("Starting test ...");
            var program = new Program();

            var tasks = new[]
            {
                Task.Run(() => program.RxSubscriber(Dc1, cancellationTokenSource.Token), cancellationTokenSource.Token),
                Task.Run(() => program.RxSubscriber(Dc2, cancellationTokenSource.Token), cancellationTokenSource.Token)
            };
            
            program.Publish(Dc1);
//             program.Publish(Dc2);

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
            cancellationTokenSource.Cancel();

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException)
            {
                Console.WriteLine("Canceled successfully...");
            }
        }

        private async Task RxSubscriber(string dataCentre, CancellationToken token)
        {
            var clusterId = "test-cluster";
            var clientId = Guid.NewGuid().ToString("N");
            var stanOptions = StanOptions.GetDefaultOptions();
            stanOptions.NatsURL = _natsClusterNodes[dataCentre];

            using var stanConnection = new StanConnectionFactory().CreateConnection(clusterId, clientId, stanOptions);
            
            var messagesSubject = stanConnection.Observe("foo");

            var loopCount = 0;
            messagesSubject.Subscribe(onNext: (message) =>
            {
                Console.WriteLine(
                    $"{dataCentre}: received '{System.Text.Encoding.UTF8.GetString(message.Data)}'. Redelivered: {message.Redelivered}. Sequence: {message.Sequence}");
                Console.WriteLine($"loopCount: {Interlocked.Increment(ref loopCount)}");
            });
            
            await Task.Delay(1000000, token);
            Console.WriteLine("Exiting subscription");
        }

        private async Task Subscriber(string dataCentre, CancellationToken token)
        {
            var subscriberOptions = StanOptions.GetDefaultOptions();
            subscriberOptions.NatsURL = _natsClusterNodes[dataCentre];
            var loopCount = 0;
            using var stanConnection = new StanConnectionFactory().CreateConnection(ClusterId, $"{Environment.MachineName.ToLower()}-{dataCentre}-receiving", subscriberOptions);
            using var subscribe = stanConnection.Subscribe("foo", CreateStanSubscriptionOptions(dataCentre, subscriberOptions), (obj, context) =>
            {
                Console.WriteLine(
                    $"{dataCentre}: received '{System.Text.Encoding.UTF8.GetString(context.Message.Data)}'. Redelivered: {context.Message.Redelivered}. Sequence: {context.Message.Sequence}");
                Console.WriteLine($"loopCount: {Interlocked.Increment(ref loopCount)}");
            });

            await Task.Delay(1000000, token);
            
            subscribe.Unsubscribe();
            Console.WriteLine("Exiting subscription");
        }

        private static StanSubscriptionOptions CreateStanSubscriptionOptions(string dataCentre, StanOptions subscriberOptions)
        {
            var subscriptionOptions = StanSubscriptionOptions.GetDefaultOptions();
            subscriptionOptions.ManualAcks = true;
            subscriberOptions.PubAckWait = 60000;
            subscriberOptions.MaxPubAcksInFlight = 1;
            subscriptionOptions.DurableName = $"test-client-{dataCentre}";
            return subscriptionOptions;
        }

        private void Publish(string dataCentre)
        {
            var stanOptions = StanOptions.GetDefaultOptions();
            stanOptions.NatsURL = _natsClusterNodes[dataCentre];
            var iteration = 0;
            const int numberOfMessagesToSend = 10;
            using var stanConnection = new StanConnectionFactory().CreateConnection(ClusterId, $"{Environment.MachineName.ToLower()}-{dataCentre}-sending", stanOptions);

            var messages = Enumerable.Range(1, numberOfMessagesToSend).Select(r =>
            {
                Console.WriteLine($"{dataCentre}: sent 'Sending message from test {++iteration}'");
                stanConnection.Publish("foo",
                    System.Text.Encoding.UTF8.GetBytes($"Sending message from test {iteration}"));
                return r;
            });
            Console.WriteLine($"Published {messages.Count()} messages...");
        }
    }
}
