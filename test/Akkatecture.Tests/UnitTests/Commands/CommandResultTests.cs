// The MIT License (MIT)
//
// Copyright (c) 2018 - 2021 Lutando Ngqakaza
// Copyright (c) 2022-2023 AfterLutz Contributors
//    https://github.com/AfterLutz/Akkatecture 
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
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Akkatecture.Aggregates;
using Akkatecture.Aggregates.CommandResults;
using Akkatecture.Commands;
using Akkatecture.TestHelpers.Aggregates;
using Akkatecture.TestHelpers.Aggregates.Commands;
using Akkatecture.TestHelpers.Aggregates.Events;
using FluentAssertions;
using Xunit;

namespace Akkatecture.Tests.UnitTests.Commands;

public class CommandResultTests : TestKit
{
    
    private const string Category = "CommandResults";
    [Fact]
    [Category(Category)]
    public async Task InitialState_AfterAggregateCreation_TestCommandSuccessResultReturned()
    {
        var eventProbe = CreateTestProbe("event-probe");
        var aggregateManager = Sys.ActorOf(Props.Create(() => new TestAggregateManager()), "test-aggregatemanager");
    
        var aggregateId = TestAggregateId.New;
        var commandId = CommandId.New;
        var command = new CreateTestCommandRequestingCommandResult(aggregateId, commandId);
        var commandResult = await aggregateManager.Ask<ICommandResult>(command);

        commandResult.IsSuccess.Should().BeTrue();
        commandResult.CommandId.Should().Be(commandId);
        (commandResult is SuccessCommandResult).Should().BeTrue();
    }
    
    [Fact]
    [Category(Category)]
    public async Task InitialState_AfterAggregateCreation_TestCommandFailResultReturned()
    {
        var eventProbe = CreateTestProbe("event-probe");
        var aggregateManager = Sys.ActorOf(Props.Create(() => new TestAggregateManager()), "test-aggregatemanager");
    
        var aggregateId = TestAggregateId.New;
        var commandId = CommandId.New;
        var command = new CreateTestCommandRequestingCommandResult(aggregateId, commandId);
        var commandResult = await aggregateManager.Ask<ICommandResult>(command);
        
        // Send the command a second time, triggering a Failure response
        commandResult = await aggregateManager.Ask<ICommandResult>(command);

        commandResult.IsSuccess.Should().BeFalse();
        commandResult.CommandId.Should().Be(commandId);
        (commandResult as FailedCommandResult).Errors.Should().Contain("Aggregate already exists");
    }

     [Fact]
     public void SuccessCommandResults_Should_Show_Success_Using_IIdentity()
     {
         var id = TestAggregateId.New;
         var createTest = new CreateTestCommand(id, CommandId.New); 
        
         // Pass ICommand as param
         var commandResult = CommandResult.SucceedWith(createTest);
         commandResult.CommandId.Should().BeEquivalentTo(createTest.SourceId);
         commandResult.IsSuccess.Should().BeTrue();
     }
     
    [Fact]
    public void SuccessCommandResults_Should_Show_Success_Using_String()
    {
        var id = TestAggregateId.New;
        var createTest = new CreateTestCommand(id, CommandId.New); 

        // Pass string as param
        var commandResult = CommandResult.SucceedWith(createTest.SourceId.Value);
        commandResult.CommandId.Should().BeEquivalentTo(createTest.SourceId);
        commandResult.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public void FailedCommandResults_Should_Show_Failure_Using_String_Array()
    {
        var id = TestAggregateId.New;
        var createTest = new CreateTestCommand(id, CommandId.New); 

        // Pass List of errors
        var errorArray = new List<string> { "something bad", "very bad" };
        
        // Pass string for sourceId
        var commandResult = CommandResult.FailWith(createTest.SourceId.ToString(), errorArray);
        commandResult.CommandId.Should().BeEquivalentTo(createTest.SourceId);
        commandResult.IsSuccess.Should().BeFalse();
        commandResult.ToString().Should().ContainAll("Failed", createTest.SourceId.Value, "something bad", "very bad");
    }
    
    [Fact]
    public void FailedCommandResults_Should_Show_Failure_Using_Multiple_Strings()
    {
        var id = TestAggregateId.New;
        var createTest = new CreateTestCommand(id, CommandId.New); 

        // Pass string for CommandId
        // Pass multiple string for error messages
        var commandResult = CommandResult.FailWith(createTest.SourceId.Value, "something bad", "very bad");
        commandResult.CommandId.Should().BeEquivalentTo(createTest.SourceId);
        commandResult.IsSuccess.Should().BeFalse();
        commandResult.ToString().Should().ContainAll("Failed", createTest.SourceId.Value, "something bad", "very bad");
    }

    [Fact]
    public void FailedCommandResults_From_Command_Should_Show_Failure_Using_Multiple_Strings()
    {
        var id = TestAggregateId.New;
        var createTest = new CreateTestCommand(id, CommandId.New); 

        // Pass multiple string for error messages
        var commandResult = CommandResult.FailWith(createTest, "something bad", "very bad");
        commandResult.CommandId.Should().BeEquivalentTo(createTest.SourceId);
        commandResult.IsSuccess.Should().BeFalse();
        commandResult.ToString().Should().ContainAll("Failed", createTest.SourceId.Value, "something bad", "very bad");
    }
    
    [Fact]
    public void FailedCommandResults_Should_Show_Failure_Without_Errors_Supplied()
    {
        var id = TestAggregateId.New;
        var createTest = new CreateTestCommand(id, CommandId.New); 

        // Pass IIdentity for sourceId
        // Pass nothing for error messages
        var commandResult = CommandResult.FailWith(createTest.SourceId.Value);
        commandResult.CommandId.Should().BeEquivalentTo(createTest.SourceId);
        commandResult.IsSuccess.Should().BeFalse();
        commandResult.ToString().Should().Contain($"Failed execution for command {createTest.SourceId}");
    }    
    
    [Fact]
    public void FailedCommandResults_From_Command_Should_Show_Failure_Without_Errors_Supplied()
    {
        var id = TestAggregateId.New;
        var createTest = new CreateTestCommand(id, CommandId.New); 

        // Pass IIdentity for sourceId
        // Pass nothing for error messages
        var commandResult = CommandResult.FailWith(createTest, new List<string>());
        commandResult.CommandId.Should().BeEquivalentTo(createTest.SourceId);
        commandResult.IsSuccess.Should().BeFalse();
        commandResult.ToString().Should().Contain($"Failed execution for command {createTest.SourceId}");
    }
}
