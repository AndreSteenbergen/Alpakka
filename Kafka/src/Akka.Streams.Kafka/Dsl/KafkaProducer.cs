﻿using System.Threading.Tasks;
using Akka.Streams.Dsl;
using Akka.Streams.Kafka.Messages;
using Akka.Streams.Kafka.Settings;
using Akka.Streams.Kafka.Stages;
using Confluent.Kafka;

namespace Akka.Streams.Kafka.Dsl
{
    /// <summary>
    /// Akka Stream connector for publishing messages to Kafka topics.
    /// </summary>
    public static class KafkaProducer
    {
        /// <summary>
        /// The `PlainSink` can be used for publishing records to Kafka topics.
        /// </summary>
        public static Sink<MessageAndMeta<TKey, TValue>, Task> PlainSink<TKey, TValue>(ProducerSettings<TKey, TValue> settings)
        {
            return Flow
                .Create<MessageAndMeta<TKey, TValue>>()
                .Via(PlainFlow(settings))
                .ToMaterialized(Sink.Ignore<DeliveryReport<TKey, TValue>>(), Keep.Right);
        }

        /// <summary>
        /// The `PlainSink` can be used for publishing records to Kafka topics.
        /// </summary>
        public static Sink<MessageAndMeta<TKey, TValue>, Task> PlainSink<TKey, TValue>(ProducerSettings<TKey, TValue> settings, IProducer<TKey, TValue> producer)
        {
            return Flow
                .Create<MessageAndMeta<TKey, TValue>>()
                .Via(PlainFlow(settings, producer))
                .ToMaterialized(Sink.Ignore<DeliveryReport<TKey, TValue>>(), Keep.Right);
        }

        /// <summary>
        /// Publish records to Kafka topics and then continue the flow. Possibility to pass through a message, which
        /// can for example be a <see cref="CommittedOffsets"/> that can be committed later in the flow.
        /// </summary>
        public static Flow<MessageAndMeta<TKey, TValue>, DeliveryReport<TKey, TValue>, NotUsed> PlainFlow<TKey, TValue>(ProducerSettings<TKey, TValue> settings)
        {
            var flow = Flow.FromGraph(new ProducerStage<TKey, TValue>(
                    settings,
                    closeProducerOnStop: true,
                    producerProvider : settings.CreateKafkaProducer))
                .SelectAsync(settings.Parallelism, x => x);

            return string.IsNullOrEmpty(settings.DispatcherId) 
                ? flow
                : flow.WithAttributes(ActorAttributes.CreateDispatcher(settings.DispatcherId));
        }

        /// <summary>
        /// Publish records to Kafka topics and then continue the flow. Possibility to pass through a message, which
        /// can for example be a <see cref="CommitableOffset"/> that can be committed later in the flow.
        /// </summary>
        public static Flow<MessageAndMeta<TKey, TValue>, DeliveryReport<TKey, TValue>, NotUsed> PlainFlow<TKey, TValue>(ProducerSettings<TKey, TValue> settings, IProducer<TKey, TValue> producer)
        {
            var flow = Flow.FromGraph(new ProducerStage<TKey, TValue>(
                    settings,
                    closeProducerOnStop: false,
                    producerProvider: () => producer))
                .SelectAsync(settings.Parallelism, x => x);

            return string.IsNullOrEmpty(settings.DispatcherId)
                ? flow
                : flow.WithAttributes(ActorAttributes.CreateDispatcher(settings.DispatcherId));
        }
    }
}
