﻿// The MIT License (MIT)
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akkatecture.Aggregates;
using Akkatecture.Examples.Api.Domain.Sagas;
using Akkatecture.Examples.Api.Domain.Sagas.Events;
using Akkatecture.Subscribers;

namespace Akkatecture.Examples.Api.Domain.Repositories.Operations
{
    public class OperationsStorageHandler : DomainEventSubscriber,
        ISubscribeToAsync<ResourceCreationSaga,ResourceCreationSagaId,ResourceCreationStartedEvent>,
        ISubscribeToAsync<ResourceCreationSaga,ResourceCreationSagaId,ResourceCreationProgressEvent>,
        ISubscribeToAsync<ResourceCreationSaga,ResourceCreationSagaId,ResourceCreationEndedEvent>
    {
        private readonly List<OperationsProjection> _operations = new List<OperationsProjection>();

        public OperationsStorageHandler()
        {
            Receive<GetOperationsQuery>(Handle);
        }
        
        public Task HandleAsync(IDomainEvent<ResourceCreationSaga, ResourceCreationSagaId, ResourceCreationStartedEvent> domainEvent)
        {
            var operation = new OperationsProjection(domainEvent.AggregateEvent.ResourceId.GetGuid(),0,0, domainEvent.AggregateEvent.StartedAt);
            
            _operations.Add(operation);
            return Task.CompletedTask;
        }

        public Task HandleAsync(IDomainEvent<ResourceCreationSaga, ResourceCreationSagaId, ResourceCreationProgressEvent> domainEvent)
        {
            var oldOperation = _operations.Single(x => x.Id == domainEvent.AggregateEvent.ResourceId.GetGuid());
            var operation = new OperationsProjection(domainEvent.AggregateEvent.ResourceId.GetGuid(),domainEvent.AggregateEvent.Progress,domainEvent.AggregateEvent.Elapsed, oldOperation.StartedAt);

            _operations.RemoveAll(x => x.Id == domainEvent.AggregateEvent.ResourceId.GetGuid());
            _operations.Add(operation);
            return Task.CompletedTask;
        }

        public Task HandleAsync(IDomainEvent<ResourceCreationSaga, ResourceCreationSagaId, ResourceCreationEndedEvent> domainEvent)
        {
            var oldOperation = _operations.Single(x => x.Id == domainEvent.AggregateEvent.ResourceId.GetGuid());
            var operation = new OperationsProjection(domainEvent.AggregateEvent.ResourceId.GetGuid(),domainEvent.AggregateEvent.Progress,domainEvent.AggregateEvent.Elapsed, oldOperation.StartedAt);
            
            _operations.RemoveAll(x => x.Id == domainEvent.AggregateEvent.ResourceId.GetGuid());
            _operations.Add(operation);
            return Task.CompletedTask;
        }

        public bool Handle(GetOperationsQuery query)
        {
            Sender.Tell(_operations,Self);
            return true;
        }
    }
}
