// The MIT License (MIT)
//
// Copyright (c) 2018 - 2021 Lutando Ngqakaza
// Copyright (c) 2022-2023 AfterLutz Contributors  
//    https://github.com/AfterLutz/Akketecture
// 
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Akkatecture.Aggregates;
using Akkatecture.Core;
using Akkatecture.Extensions;
using Akkatecture.TestHelpers.Aggregates;
using Akkatecture.TestHelpers.Aggregates.Entities;
using Akkatecture.TestHelpers.Aggregates.Events;
using FluentAssertions;
using Xunit;

namespace Akkatecture.Tests.UnitTests.Aggregates
{
    public class ComittedEventTests
    {
        [Fact]
        public void InstantiatingCommittedEvent_ValidData_ConformsToContracts()
        {
            var aggregateSequenceNumber = 3;
            var aggregateId = TestAggregateId.New;
            var entityId = TestId.New;
            var entity = new Test(entityId);
            var aggregateEvent = new TestAddedEvent(entity);
            var now = DateTimeOffset.UtcNow;
            var eventId = EventId.NewDeterministic(
                GuidFactories.Deterministic.Namespaces.Events,
                $"{aggregateId.Value}-v{aggregateSequenceNumber}");
            var eventMetadata = new Metadata
            {
                Timestamp = now,
                AggregateSequenceNumber = aggregateSequenceNumber,
                AggregateName = typeof(TestAggregate).GetAggregateName().Value,
                AggregateId = aggregateId.Value,
                EventId = eventId
            };
            var committedEvent =
                new CommittedEvent<TestAggregate, TestAggregateId, TestAddedEvent>(
                    aggregateId, 
                    aggregateEvent,
                    eventMetadata,
                    now,
                    aggregateSequenceNumber);

            committedEvent.GetIdentity().Should().Be(aggregateId);
            committedEvent.GetAggregateEvent().Should().Be(aggregateEvent);
        }
        
        [Fact]
        public void InstantiatingCommittedEvent_WithNullAggregateEvent_ThrowsException()
        {
            var aggregateSequenceNumber = 3;
            var aggregateId = TestAggregateId.New;
            var now = DateTimeOffset.UtcNow;
            var eventId = EventId.NewDeterministic(
                GuidFactories.Deterministic.Namespaces.Events,
                $"{aggregateId.Value}-v{aggregateSequenceNumber}");
            var eventMetadata = new Metadata
            {
                Timestamp = now,
                AggregateSequenceNumber = aggregateSequenceNumber,
                AggregateName = typeof(TestAggregate).GetAggregateName().Value,
                AggregateId = aggregateId.Value,
                EventId = eventId
            };

            this.Invoking(test =>
                    new DomainEvent<TestAggregate, TestAggregateId, TestAddedEvent>(
                        aggregateId,
                        null,
                        eventMetadata,
                        now,
                        aggregateSequenceNumber))
                .Should().Throw<ArgumentNullException>().And.Message.Contains("aggregateEvent").Should().BeTrue();
        }
        
        [Fact]
        public void InstantiatingCommittedEvent_WithNullMetadata_ThrowsException()
        {
            var aggregateSequenceNumber = 3;
            var aggregateId = TestAggregateId.New;
            var entityId = TestId.New;
            var entity = new Test(entityId);
            var aggregateEvent = new TestAddedEvent(entity);
            var now = DateTimeOffset.UtcNow;

            this.Invoking(test =>
                    new CommittedEvent<TestAggregate, TestAggregateId, TestAddedEvent>(
                        aggregateId,
                        aggregateEvent,
                        null,
                        now,
                        aggregateSequenceNumber))
                .Should().Throw<ArgumentNullException>().And.Message.Contains("metadata").Should().BeTrue();
        }
        
        [Fact]
        public void InstantiatingCommittedEvent_WithDefaultTimeOffset_ThrowsException()
        {
            var aggregateSequenceNumber = 3;
            var aggregateId = TestAggregateId.New;
            var entityId = TestId.New;
            var entity = new Test(entityId);
            var aggregateEvent = new TestAddedEvent(entity);
            var now = DateTimeOffset.UtcNow;
            var eventId = EventId.NewDeterministic(
                GuidFactories.Deterministic.Namespaces.Events,
                $"{aggregateId.Value}-v{aggregateSequenceNumber}");
            
            var eventMetadata = new Metadata
            {
                Timestamp = now,
                AggregateSequenceNumber = aggregateSequenceNumber,
                AggregateName = typeof(TestAggregate).GetAggregateName().Value,
                AggregateId = aggregateId.Value,
                EventId = eventId
            };

            this.Invoking(test =>
                    new CommittedEvent<TestAggregate, TestAggregateId, TestAddedEvent>(
                        aggregateId,
                        aggregateEvent,
                        eventMetadata,
                        default(DateTimeOffset),
                        aggregateSequenceNumber))
                .Should().Throw<ArgumentNullException>().And.Message.Contains("timestamp").Should().BeTrue();
        }
        
        [Fact]
        public void InstantiatingCommittedEvent_WithNullId_ThrowsException()
        {
            var aggregateSequenceNumber = 3;
            var aggregateId = TestAggregateId.New;
            var entityId = TestId.New;
            var entity = new Test(entityId);
            var aggregateEvent = new TestAddedEvent(entity);
            var now = DateTimeOffset.UtcNow;
            var eventId = EventId.NewDeterministic(
                GuidFactories.Deterministic.Namespaces.Events,
                $"{aggregateId.Value}-v{aggregateSequenceNumber}");
            
            var eventMetadata = new Metadata
            {
                Timestamp = now,
                AggregateSequenceNumber = aggregateSequenceNumber,
                AggregateName = typeof(TestAggregate).GetAggregateName().Value,
                AggregateId = aggregateId.Value,
                EventId = eventId
            };

            this.Invoking(test =>
                    new CommittedEvent<TestAggregate, TestAggregateId, TestAddedEvent>(
                        null,
                        aggregateEvent,
                        eventMetadata,
                        now,
                        aggregateSequenceNumber))
                .Should().Throw<ArgumentNullException>().And.Message.Contains("aggregateIdentity").Should().BeTrue();
        }
        
        [Fact]
        public void InstantiatingCommittedEvent_WithNegativeSequenceNumber_ThrowsException()
        {
            var aggregateSequenceNumber = 3;
            var aggregateId = TestAggregateId.New;
            var entityId = TestId.New;
            var entity = new Test(entityId);
            var aggregateEvent = new TestAddedEvent(entity);
            var now = DateTimeOffset.UtcNow;
            var eventId = EventId.NewDeterministic(
                GuidFactories.Deterministic.Namespaces.Events,
                $"{aggregateId.Value}-v{aggregateSequenceNumber}");
            
            var eventMetadata = new Metadata
            {
                Timestamp = now,
                AggregateSequenceNumber = aggregateSequenceNumber,
                AggregateName = typeof(TestAggregate).GetAggregateName().Value,
                AggregateId = aggregateId.Value,
                EventId = eventId
            };

            this.Invoking(test =>
                    new CommittedEvent<TestAggregate, TestAggregateId, TestAddedEvent>(
                        aggregateId,
                        aggregateEvent,
                        eventMetadata,
                        now,
                        -4))
                .Should().Throw<ArgumentOutOfRangeException>().And.Message.Contains("aggregateSequenceNumber").Should().BeTrue();
        }
    }
}
