﻿// The MIT License (MIT)
//
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
// Modified from original source https://github.com/eventflow/EventFlow
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

using System.ComponentModel;
using Akkatecture.Specifications.Provided;
using Akkatecture.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Akkatecture.Tests.UnitTests.Specifications
{
    [Category(Categories.Unit)]
    public class ExpressionSpecificationTests
    {
        [Fact]
        public void StringIsRight()
        {
            var specification = new ExpressionSpecification<int>(i => (i > 1 && i < 10) || i == 42);

            var str = specification.ToString();

            str.Should().Be("i => (((i > 1) && (i < 10)) || (i == 42))");
        }

        [Theory]
        [InlineData(42, true)]
        [InlineData(-42, false)]
        public void ExpressionIsEvaluated(int value, bool expectedIsSatisfied)
        {
            var is42 = new ExpressionSpecification<int>(i => i == 42);

            var result = is42.IsSatisfiedBy(value);

            result.Should().Be(expectedIsSatisfied);
        }
    }
}
