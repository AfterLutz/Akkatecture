﻿// The MIT License (MIT)
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Akkatecture.Aggregates;
using Akkatecture.Aggregates.Snapshot;
using Akkatecture.Core;
using Akkatecture.Events;
using Akkatecture.Jobs;
using Akkatecture.Sagas;
using Akkatecture.Sagas.SagaTimeouts;
using Akkatecture.Subscribers;

namespace Akkatecture.Extensions
{
    public static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<Type, string> PrettyPrintCache = new();

        public static string PrettyPrint(this Type type)
        {
            return PrettyPrintCache.GetOrAdd(
                type,
                t =>
                {
                    try
                    {
                        return PrettyPrintRecursive(t, 0);
                    }
                    catch (Exception)
                    {
                        return t.Name;
                    }
                });
        }

        private static string PrettyPrintRecursive(Type type, int depth)
        {
            if (depth > 3)
                return type.Name;

            var nameParts = type.Name.Split('`');
            if (nameParts.Length == 1)
                return nameParts[0];

            var genericArguments = type.GetTypeInfo().GetGenericArguments();
            return !type.IsConstructedGenericType
                ? $"{nameParts[0]}<{new string(',', genericArguments.Length - 1)}>"
                : $"{nameParts[0]}<{string.Join(",", genericArguments.Select(t => PrettyPrintRecursive(t, depth + 1)))}>";
        }

        private static readonly ConcurrentDictionary<Type, AggregateName> AggregateNames = new();

        public static AggregateName GetAggregateName(
            this Type aggregateType)
        {
            return AggregateNames.GetOrAdd(
                aggregateType,
                t =>
                {
                    if (!typeof(IAggregateRoot).GetTypeInfo().IsAssignableFrom(t))
                        throw new ArgumentException($"Type '{t.PrettyPrint()}' is not an aggregate root");

                    return new AggregateName(
                        t.GetTypeInfo().GetCustomAttributes<AggregateNameAttribute>().SingleOrDefault()?.Name ??
                        t.Name);
                });
        }

        private static readonly ConcurrentDictionary<Type, AggregateName> SagaNames = new();

        public static AggregateName GetSagaName(
            this Type sagaType)
        {
            return SagaNames.GetOrAdd(
                sagaType,
                t =>
                {
                    if (!typeof(IAggregateRoot).GetTypeInfo().IsAssignableFrom(t))
                        throw new ArgumentException($"Type '{t.PrettyPrint()}' is not a saga.");

                    return new AggregateName(
                        t.GetTypeInfo().GetCustomAttributes<SagaNameAttribute>().SingleOrDefault()?.Name ??
                        t.Name);
                });
        }

        private static readonly ConcurrentDictionary<Type, JobName> JobNames = new();

        public static JobName GetJobName(
            this Type jobType)
        {
            return JobNames.GetOrAdd(
                jobType,
                t =>
                {
                    if (!typeof(IJob).GetTypeInfo().IsAssignableFrom(t))
                        throw new ArgumentException($"Type '{t.PrettyPrint()}' is not a job");

                    return new JobName(
                        t.GetTypeInfo().GetCustomAttributes<JobNameAttribute>().SingleOrDefault()?.Name ??
                        t.Name);
                });
        }

        internal static IReadOnlyDictionary<Type, Action<T, IAggregateEvent>> GetAggregateEventApplyMethods<TAggregate, TIdentity, T>(this Type type)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregateEventType = typeof(IAggregateEvent<TAggregate, TIdentity>);

            return type
                .GetTypeInfo()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi =>
                {
                    if (mi.Name != "Apply") return false;
                    var parameters = mi.GetParameters();
                    return
                        parameters.Length == 1 &&
                        aggregateEventType.GetTypeInfo().IsAssignableFrom(parameters[0].ParameterType);
                })
                .ToDictionary(
                    mi => mi.GetParameters()[0].ParameterType,
                    mi => ReflectionHelper.CompileMethodInvocation<Action<T, IAggregateEvent>>(type, "Apply", mi.GetParameters()[0].ParameterType));
        }

        internal static IReadOnlyDictionary<Type, Action<T, IAggregateSnapshot>> GetAggregateSnapshotHydrateMethods<TAggregate, TIdentity, T>(this Type type)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregateSnapshot = typeof(IAggregateSnapshot<TAggregate, TIdentity>);

            return type
                .GetTypeInfo()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi =>
                {
                    if (mi.Name != "Hydrate") return false;
                    var parameters = mi.GetParameters();
                    return
                        parameters.Length == 1 &&
                        aggregateSnapshot.GetTypeInfo().IsAssignableFrom(parameters[0].ParameterType);
                })
                .ToDictionary(
                    mi => mi.GetParameters()[0].ParameterType,
                    mi => ReflectionHelper.CompileMethodInvocation<Action<T, IAggregateSnapshot>>(type, "Hydrate", mi.GetParameters()[0].ParameterType));
        }

        internal static IReadOnlyDictionary<Type, Action<TAggregateState, IAggregateEvent>> GetAggregateStateEventApplyMethods<TAggregate, TIdentity, TAggregateState>(this Type type)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TAggregateState : IEventApplier<TAggregate, TIdentity>
        {
            var aggregateEventType = typeof(IAggregateEvent<TAggregate, TIdentity>);

            return type
                .GetTypeInfo()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi =>
                {
                    if (mi.Name != "Apply") return false;
                    var parameters = mi.GetParameters();
                    return
                        parameters.Length == 1 &&
                        aggregateEventType.GetTypeInfo().IsAssignableFrom(parameters[0].ParameterType);
                })
                .ToDictionary(
                    mi => mi.GetParameters()[0].ParameterType,
                    mi => ReflectionHelper.CompileMethodInvocation<Action<TAggregateState, IAggregateEvent>>(type, "Apply", mi.GetParameters()[0].ParameterType));
        }

        internal static IReadOnlyList<Type> GetAsyncDomainEventSubscriberSubscriptionTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var domainEventTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISubscribeToAsync<,,>))
                .Select(i => typeof(IDomainEvent<,,>).MakeGenericType(i.GetGenericArguments()[0],i.GetGenericArguments()[1],i.GetGenericArguments()[2]))
                .ToList();

            return domainEventTypes;
        }

        internal static IReadOnlyList<Type> GetAggregateExecuteTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var domainEventTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExecute<>))
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            return domainEventTypes;
        }

        internal static IReadOnlyList<Type> GetDomainEventSubscriberSubscriptionTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var domainEventTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISubscribeTo<,,>))
                .Select(i => typeof(IDomainEvent<,,>).MakeGenericType(i.GetGenericArguments()[0],i.GetGenericArguments()[1],i.GetGenericArguments()[2]))
                .ToList();

            return domainEventTypes;
        }

        private static readonly ConcurrentDictionary<Type, AggregateName> AggregateNameCache = new();

        internal static AggregateName GetCommittedEventAggregateRootName(this Type type)
        {
            return AggregateNameCache.GetOrAdd(
                type,
                t =>
                {
                    var interfaces = t
                        .GetTypeInfo()
                        .GetInterfaces()
                        .Select(i => i.GetTypeInfo())
                        .ToList();

                    var aggregateType = interfaces
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommittedEvent<,>))
                        .Select(i => i.GetGenericArguments()[0]).SingleOrDefault();

                    if (aggregateType != null)
                        return aggregateType.GetAggregateName();

                    throw new ArgumentException(nameof(t));
                });
        }

        private static readonly ConcurrentDictionary<Type, Type> AggregateEventTypeCache = new();

        internal static Type GetCommittedEventAggregateEventType(this Type type)
        {
            return AggregateEventTypeCache.GetOrAdd(
                type,
                t =>
                {
                    var interfaces = t
                        .GetTypeInfo()
                        .GetInterfaces()
                        .Select(i => i.GetTypeInfo())
                        .ToList();

                    var aggregateEvent = interfaces
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommittedEvent<,,>))
                        .Select(i => i.GetGenericArguments()[2]).SingleOrDefault();

                    if (aggregateEvent != null)
                        return aggregateEvent;

                    throw new ArgumentException(nameof(t));
                });
        }

        internal static IReadOnlyList<Type> GetJobRunTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var jobRunTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRun<>))
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            return jobRunTypes;
        }

        internal static IReadOnlyList<Type> GetAsyncSagaEventSubscriptionTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var handleEventTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaHandlesAsync<,,>))
                .Select(t => typeof(IDomainEvent<,,>).MakeGenericType(t.GetGenericArguments()[0],
                    t.GetGenericArguments()[1], t.GetGenericArguments()[2]))
                .ToList();

            var startedByEventTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaIsStartedByAsync<,,>))
                .Select(t => typeof(IDomainEvent<,,>).MakeGenericType(t.GetGenericArguments()[0],
                    t.GetGenericArguments()[1], t.GetGenericArguments()[2]))
                .ToList();

            startedByEventTypes.AddRange(handleEventTypes);

            return startedByEventTypes;
        }

        internal static IReadOnlyList<Type> GetSagaEventSubscriptionTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var handleEventTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaHandles<,,>))
                .Select(t => typeof(IDomainEvent<,,>).MakeGenericType(t.GetGenericArguments()[0],
                    t.GetGenericArguments()[1], t.GetGenericArguments()[2]))
                .ToList();

            var startedByEventTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaIsStartedBy<,,>))
                .Select(t => typeof(IDomainEvent<,,>).MakeGenericType(t.GetGenericArguments()[0],
                    t.GetGenericArguments()[1], t.GetGenericArguments()[2]))
                .ToList();

            startedByEventTypes.AddRange(handleEventTypes);

            return startedByEventTypes;
        }

        internal static IReadOnlyList<Type> GetAsyncSagaTimeoutSubscriptionTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var sagaTimeoutSubscriptionTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaHandlesTimeoutAsync<>))
                .Select(t => t.GetGenericArguments()[0])
                .ToList();

            return sagaTimeoutSubscriptionTypes;
        }

        internal static IReadOnlyList<Type> GetSagaTimeoutSubscriptionTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var sagaTimeoutSubscriptionTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISagaHandlesTimeout<>))
                .Select(t => t.GetGenericArguments()[0])
                .ToList();

            return sagaTimeoutSubscriptionTypes;
        }

        internal static IReadOnlyDictionary<Type, Func<T,IAggregateEvent, IAggregateEvent>> GetAggregateEventUpcastMethods<TAggregate, TIdentity, T>(this Type type)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregateEventType = typeof(IAggregateEvent<TAggregate, TIdentity>);

            return type
                .GetTypeInfo()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi =>
                {
                    if (mi.Name != "Upcast")
                        return false;

                    var parameters = mi.GetParameters();
                    return
                        parameters.Length == 1 &&
                        aggregateEventType.GetTypeInfo().IsAssignableFrom(parameters[0].ParameterType);

                })
                .ToDictionary(
                    //problem might be here
                    mi => mi.GetParameters()[0].ParameterType,
                    mi => ReflectionHelper.CompileMethodInvocation<Func<T,IAggregateEvent, IAggregateEvent>>(type, "Upcast", mi.GetParameters()[0].ParameterType));
        }

        internal static IReadOnlyList<Type> GetAggregateEventUpcastTypes(this Type type)
        {
            var interfaces = type
                .GetTypeInfo()
                .GetInterfaces()
                .Select(i => i.GetTypeInfo())
                .ToList();

            var upcastableEventTypes = interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUpcast<,>))
                .Select(i =>   i.GetGenericArguments()[0])
                .ToList();

            return upcastableEventTypes;
        }

        internal static Type GetBaseType(this Type type, string name)
        {
            var currentType = type;

            while (currentType != null)
            {
                if (currentType.Name.Contains(name))
                    return currentType;

                currentType = currentType.BaseType;
            }

            return type;
        }
    }
}