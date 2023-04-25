﻿// The MIT License (MIT)
//
// Copyright (c) 2009 - 2020 Lightbend Inc.
// Copyright (c) 2013 - 2020 .NET Foundation
// Modified from original source https://github.com/akkadotnet/akka.net
//
// Copyright (c) 2018 - 2021 Lutando Ngqakaza
// Copyright (c) 2022-2023 AfterLutz Contributors  
//    https://github.com/AfterLutz/Akketecture
// 
// 
// Permission is hereby granted free of charge, to any person obtaining a copy of
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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Akka.Actor;
using Akka.IO;
using Akka.MultiNodeTestRunner.Shared.Sinks;
using Akka.Remote.TestKit;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Akkatecture.NodeTestRunner
{
    class Program
    {
        private static readonly TimeSpan MaxProcessWaitTimeout = TimeSpan.FromMinutes(5);
        private static IActorRef _logger;

        public static int Main(string[] args)
        {
            var nodeIndex = CommandLine.GetInt32("multinode.index");
            var nodeRole = CommandLine.GetProperty("multinode.role");
            var assemblyFileName = CommandLine.GetProperty("multinode.test-assembly");
            var typeName = CommandLine.GetProperty("multinode.test-class");
            var testName = CommandLine.GetProperty("multinode.test-method");
            var displayName = testName;

            var listenAddress = IPAddress.Parse(CommandLine.GetProperty("multinode.listen-address"));
            var listenPort = CommandLine.GetInt32("multinode.listen-port");
            var listenEndpoint = new IPEndPoint(listenAddress, listenPort);

            var system = ActorSystem.Create("NoteTestRunner-" + nodeIndex);
            var tcpClient = _logger = system.ActorOf<RunnerTcpClient>();
            system.Tcp().Tell(new Tcp.Connect(listenEndpoint), tcpClient);


            // In NetCore, if the assembly file hasn't been touched, 
            // XunitFrontController would fail loading external assemblies and its dependencies.
            AssemblyLoadContext.Default.Resolving += (assemblyLoadContext, assemblyName) => DefaultOnResolving(assemblyLoadContext, assemblyName, assemblyFileName);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFileName);
            DependencyContext.Load(assembly)
                .CompileLibraries
                .Where(dep => dep.Name.ToLower()
                    .Contains(assembly.FullName.Split(new[] { ',' })[0].ToLower()))
                .Select(dependency => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(dependency.Name)));

            Thread.Sleep(TimeSpan.FromSeconds(10));
            using (var controller = new XunitFrontController(AppDomainSupport.IfAvailable, assemblyFileName))
            {
                /* need to pass in just the assembly name to Discovery, not the full path
                 * i.e. "Akka.Cluster.Tests.MultiNode.dll"
                 * not "bin/Release/Akka.Cluster.Tests.MultiNode.dll" - this will cause
                 * the Discovery class to actually not find any individual specs to run
                 */
                var assemblyName = Path.GetFileName(assemblyFileName);
                Console.WriteLine("Running specs for {0} [{1}]", assemblyName, assemblyFileName);
                using (var discovery = new Discovery(assemblyName, typeName))
                {
                    using (var sink = new Sink(nodeIndex, nodeRole, tcpClient))
                    {
                        try
                        {
                            controller.Find(true, discovery, TestFrameworkOptions.ForDiscovery());
                            discovery.Finished.WaitOne();
                            controller.RunTests(discovery.TestCases, sink, TestFrameworkOptions.ForExecution());
                        }
                        catch (AggregateException ex)
                        {
                            var specFail = new SpecFail(nodeIndex, nodeRole, displayName);
                            specFail.FailureExceptionTypes.Add(ex.GetType().ToString());
                            specFail.FailureMessages.Add(ex.Message);
                            specFail.FailureStackTraces.Add(ex.StackTrace);
                            foreach (var innerEx in ex.Flatten().InnerExceptions)
                            {
                                specFail.FailureExceptionTypes.Add(innerEx.GetType().ToString());
                                specFail.FailureMessages.Add(innerEx.Message);
                                specFail.FailureStackTraces.Add(innerEx.StackTrace);
                            }
                            _logger.Tell(specFail.ToString());
                            Console.WriteLine(specFail);

                            //make sure message is send over the wire
                            FlushLogMessages();
                            Environment.Exit(1); //signal failure
                            return 1;
                        }
                        catch (Exception ex)
                        {
                            var specFail = new SpecFail(nodeIndex, nodeRole, displayName);
                            specFail.FailureExceptionTypes.Add(ex.GetType().ToString());
                            specFail.FailureMessages.Add(ex.Message);
                            specFail.FailureStackTraces.Add(ex.StackTrace);
                            _logger.Tell(specFail.ToString());
                            Console.WriteLine(specFail);

                            //make sure message is send over the wire
                            FlushLogMessages();
                            Environment.Exit(1); //signal failure
                            return 1;
                        }

                        var timedOut = false;
                        if (!sink.Finished.WaitOne(MaxProcessWaitTimeout)) //timed out
                        {
                            var line = string.Format("Timed out while waiting for test to complete after {0} ms",
                                MaxProcessWaitTimeout);
                            _logger.Tell(line);
                            Console.WriteLine(line);
                            timedOut = true;
                        }

                        FlushLogMessages();
                        system.Terminate().Wait();

                        Environment.Exit(sink.Passed && !timedOut ? 0 : 1);
                        return sink.Passed ? 0 : 1;
                    }
                }
            }
        }

        private static void FlushLogMessages()
        {
            try
            {
                _logger.GracefulStop(TimeSpan.FromSeconds(2)).Wait();
            }
            catch
            {
                Console.WriteLine("Exception thrown while waiting for TCP transport to flush - not all messages may have been logged.");
            }
        }


        private static Assembly DefaultOnResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName, string assemblyPath)
        {
            string dllName = assemblyName.Name.Split(new[] { ',' })[0] + ".dll";
            return assemblyLoadContext.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(assemblyPath), dllName));
        }

    }

    class RunnerTcpClient : ReceiveActor, IWithUnboundedStash
    {
        public RunnerTcpClient()
        {
            Become(WaitingForConnection);
        }

        private void WaitingForConnection()
        {
            Receive<Tcp.Connected>(connected =>
            {
                Sender.Tell(new Tcp.Register(Self));
                Become(Connected(Sender));
            });
            Receive<string>(_ => Stash.Stash());
        }

        private Receive Connected(IActorRef connection)
        {
            Stash.UnstashAll();

            return message =>
            {
                var bytes = ByteString.FromString(message.ToString());
                connection.Tell(Tcp.Write.Create(bytes));

                return true;
            };
        }

        public IStash Stash { get; set; }
    }
}
