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

using System.Collections.Immutable;
using Akkatecture.Jobs.Commands;

namespace Akkatecture.Jobs
{
    public class SchedulerState<TJob, TIdentity>
        where TJob : IJob
        where TIdentity : IJobId
    {
        public static SchedulerState<TJob, TIdentity> New { get; } = new SchedulerState<TJob, TIdentity>(ImmutableDictionary<TIdentity, Schedule<TJob,TIdentity>>.Empty);

        public ImmutableDictionary<TIdentity, Schedule<TJob, TIdentity>> Entries { get; }
        public SchedulerState(
            ImmutableDictionary<TIdentity, Schedule<TJob,TIdentity>> entries)
        {
            Entries = entries;
        }
        
        public SchedulerState<TJob, TIdentity> AddEntry(Schedule<TJob,TIdentity> entry) => new SchedulerState<TJob, TIdentity>(Entries.SetItem(entry.JobId, entry));
        public SchedulerState<TJob, TIdentity> RemoveEntry(TIdentity jobId) => new SchedulerState<TJob, TIdentity>(Entries.Remove(jobId));
    }
}
