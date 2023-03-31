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

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akkatecture.Akka
{
    public abstract class ActorRefProvider
    {
        public IActorRef ActorRef { get; }

        protected ActorRefProvider(IActorRef actorRef)
            => ActorRef = actorRef;

        public virtual void Tell(object message, IActorRef sender)
            => ActorRef.Tell(message, sender);

        public virtual void Tell(object message)
            => ActorRef.Tell(message, ActorRefs.NoSender);

        public virtual Task<object> Ask(object message, TimeSpan? timeout = null)
            => ActorRef.Ask(message, timeout);

        public virtual Task<object> Ask(object message, CancellationToken cancellationToken)
            => ActorRef.Ask(message, cancellationToken);

        public virtual Task<object> Ask(object message, TimeSpan? timeout, CancellationToken cancellationToken)
            => ActorRef.Ask(message, timeout, cancellationToken);

        public virtual Task<T> Ask<T>(object message, TimeSpan? timeout = null)
            => ActorRef.Ask<T>(message, timeout);

        public virtual Task<T> Ask<T>(object message, CancellationToken cancellationToken)
            => ActorRef.Ask<T>(message, cancellationToken);

        public virtual Task<T> Ask<T>(object message, TimeSpan? timeout, CancellationToken cancellationToken)
            => ActorRef.Ask<T>(message, timeout, cancellationToken);

        public virtual Task<T> Ask<T>(Func<IActorRef, object> messageFactory, TimeSpan? timeout, CancellationToken cancellationToken)
            => ActorRef.Ask<T>(messageFactory, timeout, cancellationToken);
    }

    public class ActorRefProvider<T> : ActorRefProvider
    {
        public ActorRefProvider(IActorRef actorRef)
            : base(actorRef) { }
    }
}