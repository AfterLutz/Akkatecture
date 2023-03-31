// The MIT License (MIT)
//
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
//     Modified from original source https://github.com/eventflow/EventFlow
// Copyright (c) 2018 - 2021 Lutando Ngqakaza
//     https://github.com/Lutando/Akkatecture
// Copyright (c) 2022 AfterLutz Contributors
//     https://github.com/AfterLutz/Akketecture
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

using System.Linq;
using Akka;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using Akkatecture.Aggregates;
using Akkatecture.Events;
using Akkatecture.Extensions;

namespace Akkatecture.Query
{
    public class Consumer
    {
        public ActorSystem ActorSystem { get; set; }

        protected string Name { get; set; }

        internal Consumer(
            string name,
            ActorSystem actorSystem)
        {
            ActorSystem = actorSystem;
            Name = name;
        }

        private Consumer(
            string name,
            Config config)
        {
            var actorSystem = ActorSystem.Create(name, config);
            ActorSystem = actorSystem;
            Name = name;
        }

        public static Consumer Create(string name, Config config)
            => new(name,config);

        public static Consumer Create(ActorSystem actorSystem)
            => new(actorSystem.Name, actorSystem);

        public Consumer<TJournal> Using<TJournal>(
            string readJournalPluginId = null)
            where TJournal : IEventsByTagQuery, ICurrentEventsByTagQuery
        {
            var readJournal = PersistenceQuery
                .Get(ActorSystem)
                .ReadJournalFor<TJournal>(readJournalPluginId);

            return new Consumer<TJournal>(Name, ActorSystem, readJournal);
        }
    }

    public class Consumer<TJournal> : Consumer
        where TJournal : IEventsByTagQuery, ICurrentEventsByTagQuery
    {
        protected TJournal Journal { get; }

        internal Consumer(
            string name,
            ActorSystem actorSystem,
            TJournal journal)
            : base(name, actorSystem)
            => Journal = journal;

        public Source<EventEnvelope, NotUsed> EventsFromAggregate<TAggregate>(Offset offset = null)
            where TAggregate : IAggregateRoot
        {
            var mapper = new DomainEventReadAdapter();
            var aggregateName = typeof(TAggregate).GetAggregateName();

            return Journal
                .EventsByTag(aggregateName.Value, offset ?? Offset.NoOffset())
                .Select(x =>
                {
                    var domainEvent = mapper.FromJournal(x.Event, string.Empty).Events.Single();
                    return new EventEnvelope(x.Offset, x.PersistenceId, x.SequenceNr, domainEvent, x.Timestamp);
                });
        }

        public Source<EventEnvelope, NotUsed> CurrentEventsFromAggregate<TAggregate>(Offset offset = null)
            where TAggregate : IAggregateRoot
        {
            var mapper = new DomainEventReadAdapter();
            var aggregateName = typeof(TAggregate).GetAggregateName();

            return Journal
                .CurrentEventsByTag(aggregateName.Value, offset ?? Offset.NoOffset())
                .Select(x =>
                {
                    var domainEvent = mapper.FromJournal(x.Event, string.Empty).Events.Single();
                    return new EventEnvelope(x.Offset, x.PersistenceId, x.SequenceNr, domainEvent, x.Timestamp);
                });
        }
    }
}