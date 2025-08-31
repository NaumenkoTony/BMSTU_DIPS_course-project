namespace PaymentService.Services;

using Confluent.Kafka;
using System.Text.Json;

public interface IKafkaProducer
{
    Task PublishAsync(string topic, string key, object message, CancellationToken ct = default);
}

public class KafkaProducerService : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducerService(IConfiguration config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 5,
            LingerMs = 5,
            BatchSize = 32 * 1024
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync(string topic, string key, object message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message);
        var msg = new Message<string, string> { Key = key, Value = json };
        var result = await _producer.ProduceAsync(topic, msg, ct);
    }

    public void Dispose() => _producer?.Dispose();
}
