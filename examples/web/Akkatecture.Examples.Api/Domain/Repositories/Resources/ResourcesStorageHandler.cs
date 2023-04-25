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
using System.Threading.Tasks;
using Akkatecture.Aggregates;
using Akkatecture.Examples.Api.Domain.Sagas;
using Akkatecture.Examples.Api.Domain.Sagas.Events;
using Akkatecture.Subscribers;

namespace Akkatecture.Examples.Api.Domain.Repositories.Resources
{
    public class ResourcesStorageHandler : DomainEventSubscriber,
        ISubscribeToAsync<ResourceCreationSaga,ResourceCreationSagaId,ResourceCreationEndedEvent>
    {
        private readonly List<ResourcesProjection> _resources = new List<ResourcesProjection>();

        public ResourcesStorageHandler()
        {
            Receive<GetResourcesQuery>(Handle);
        }
        
        public Task HandleAsync(IDomainEvent<ResourceCreationSaga, ResourceCreationSagaId, ResourceCreationEndedEvent> domainEvent)
        {
            var readModel = new ResourcesProjection(domainEvent.AggregateEvent.ResourceId.GetGuid(),domainEvent.AggregateEvent.Elapsed,domainEvent.AggregateEvent.EndedAt);
            
            _resources.Add(readModel);
            
            return Task.CompletedTask;
        }

        public bool Handle(GetResourcesQuery query)
        {
            Sender.Tell(_resources,Self);
            return true;
        }
    }
}
