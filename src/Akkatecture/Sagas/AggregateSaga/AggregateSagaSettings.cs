﻿// The MIT License (MIT)
//
// Copyright (c) 2018 - 2021 Lutando Ngqakaza
// https://github.com/Lutando/Akkatecture 
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

using Akka.Configuration;
using Akkatecture.Configuration;

namespace Akkatecture.Sagas.AggregateSaga
{
    public class AggregateSagaSettings
    {
        private static string _section = "akkatecture.aggregate-saga";
        public readonly bool AutoReceive;
        public readonly bool UseDefaultEventRecover;
        public readonly bool UseDefaultSnapshotRecover;
        public readonly bool UseSagaTimeouts;
        public AggregateSagaSettings(Config config)
        {
            var aggregateSagaConfig = config.WithFallback(AkkatectureDefaultSettings.DefaultConfig());
            aggregateSagaConfig = aggregateSagaConfig.GetConfig(_section);

            AutoReceive = aggregateSagaConfig.GetBoolean("auto-receive");
            UseDefaultEventRecover = aggregateSagaConfig.GetBoolean("use-default-event-recover");
            UseDefaultSnapshotRecover = aggregateSagaConfig.GetBoolean("use-default-snapshot-recover");
            UseSagaTimeouts = aggregateSagaConfig.GetBoolean("use-sagatimeouts", true);
        }
    }
}