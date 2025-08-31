using Confluent.Kafka;
using Contracts.Dto;
using Microsoft.EntityFrameworkCore;
using StatisticsService.Data;
using StatisticsService.Models;
using System.Text.Json;

namespace StatisticsService.Kafka
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _config;

        public KafkaConsumerService(
            ILogger<KafkaConsumerService> logger,
            IServiceProvider services,
            IConfiguration config)
        {
            _logger = logger;
            _services = services;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cfg = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = "statistics-service",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
            };

            using var consumer = new ConsumerBuilder<string, string>(cfg).Build();
            consumer.Subscribe("user-actions");

            _logger.LogInformation("Kafka consumer started, groupId={Group}", cfg.GroupId);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var cr = consumer.Consume(stoppingToken);
                    if (cr?.Message?.Value is null) continue;

                    UserAction? dto = null;
                    try
                    {
                        dto = JsonSerializer.Deserialize<UserAction>(cr.Message.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize message: {Value}", cr.Message.Value);
                        consumer.Commit(cr);
                        continue;
                    }
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<StatisticsDbContext>();

                    var entity = new UserActionEntity
                    {
                        UserId = dto!.UserId,
                        Username = dto.Username,
                        Service = dto.Service,
                        Action = dto.Action,
                        Status = dto.Status,
                        Timestamp = dto.Timestamp,
                        MetadataJson = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null,

                        Topic = cr.Topic,
                        Partition = cr.Partition.Value,
                        Offset = cr.Offset.Value
                    }; try
                    {
                        db.UserActions.Add(entity);
                        await db.SaveChangesAsync(stoppingToken);
                        consumer.Commit(cr);
                    }
                    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                    {
                        _logger.LogWarning("Duplicate message (Partition={P}, Offset={O}) — skipping", entity.Partition, entity.Offset);
                        consumer.Commit(cr);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist message. Will retry (no commit).");
                        await Task.Delay(500, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer is stopping...");
            }
            finally
            {
                consumer.Close();
            }
        }
        private static bool IsUniqueViolation(DbUpdateException ex) => ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
          || ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

}