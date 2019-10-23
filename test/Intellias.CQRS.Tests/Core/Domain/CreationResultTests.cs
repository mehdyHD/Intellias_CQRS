using System.Linq;
using FluentAssertions;
using Intellias.CQRS.Core.Domain.CreationResult;
using Intellias.CQRS.Core.Results;
using Intellias.CQRS.Core.Results.Errors;
using Intellias.CQRS.Tests.Utils;
using Intellias.CQRS.Tests.Utils.Pipelines.Fakes;
using Xunit;

namespace Intellias.CQRS.Tests.Core.Domain
{
    public class CreationResultTests
    {
        [Fact]
        public void Constructor_Always_CreatesFailedWithoutEntry()
        {
            var result = new CreationResult<DummyEntry>();

            result.Entry.Should().BeNull();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_WithErrors_CreatesRsultWithSameErrors()
        {
            var executionErrors = new[]
            {
                new ExecutionError(new ErrorCodeInfo(nameof(Core), FixtureUtils.String(), FixtureUtils.String())),
                new ExecutionError(new ErrorCodeInfo(nameof(Core), FixtureUtils.String(), FixtureUtils.String()))
            };

            var result = new CreationResult<DummyEntry>(executionErrors);

            var (errors, entry) = result;
            entry.Should().BeNull();
            errors.Should().BeEquivalentTo<ExecutionError>(executionErrors);
        }

        [Fact]
        public void Succeeded_Always_CreatesSuccessful()
        {
            var entry = new DummyEntry();

            CreationResult.Succeeded(entry).Should().BeEquivalentTo(new CreationResult<DummyEntry>(entry));
        }

        [Fact]
        public void Failed_WithErrorCode_ReturnsResultWithSameErrorCode()
        {
            var errorCode = new ErrorCodeInfo(nameof(Core), FixtureUtils.String(), FixtureUtils.String());
            var expectedError = new ExecutionError(errorCode);

            CreationResult.Failed<DummyEntry>(errorCode)
                .Should().BeEquivalentTo(new CreationResult<DummyEntry>(expectedError));
        }

        [Fact]
        public void Failed_WithErrorCodeAndCustomMessage_ReturnsResultWithSameErrorCodeAndMessage()
        {
            var errorCode = new ErrorCodeInfo(nameof(Core), FixtureUtils.String(), FixtureUtils.String());
            var customMessage = FixtureUtils.String();
            var expectedError = new ExecutionError(errorCode, null, customMessage);

            CreationResult.Failed<DummyEntry>(errorCode, customMessage)
                .Should().BeEquivalentTo(new CreationResult<DummyEntry>(expectedError));
        }

        [Fact]
        public void AsCreationResult_WithSuccesAndErrors_ReturnsResultWithInnerExecutionErrors()
        {
            var error1 = new ExecutionError(new ErrorCodeInfo(nameof(Core), FixtureUtils.String(), FixtureUtils.String()));
            var error2 = new ExecutionError(new ErrorCodeInfo(nameof(Core), FixtureUtils.String(), FixtureUtils.String()));

            var creationResults = new[]
            {
                new CreationResult<DummyEntry>(error1),
                new CreationResult<DummyEntry>(new DummyEntry()),
                new CreationResult<DummyEntry>(error2),
            };

            var result = CreationResult.AsCreationResult(creationResults);

            result.Entry.Should().BeNull();
            result.Errors.Count.Should().Be(2);
            result.Errors.Should().Contain(error1);
            result.Errors.Should().Contain(error2);
        }

        [Fact]
        public void ForCommand_SuccessResult_ShouldReturnTheSameResult()
        {
            var creationResult = CreationResult.Succeeded(new DummyEntry());

            creationResult.ForCommand<DummyEntry, FakeCreateCommand>(c => c.Data)
                .Should().BeEquivalentTo(creationResult);
        }

        [Fact]
        public void ForCommand_ResultWithErrors_ShouldReturnErrorWithCorrectSource()
        {
            var errorCode = new ErrorCodeInfo(nameof(Core), FixtureUtils.String(), FixtureUtils.String());
            var creationResult = CreationResult.Failed<DummyEntry>(errorCode);

            var result = creationResult.ForCommand<DummyEntry, FakeCreateCommand>(c => c.Data);
            result.Entry.Should().BeNull();

            var internalError = result.Errors.Single();
            internalError.CodeInfo.Should().Be(errorCode);
            internalError.Code.Should().Be(errorCode.Code);
            internalError.Message.Should().Be(errorCode.Message);
            internalError.Source.Should().Be(SourceBuilder.BuildErrorSource<FakeCreateCommand>(c => c.Data));
        }

        [Fact]
        public void ForCommand_ResultWith2Errors_ShouldReturn2Errors()
        {
            var errorCode = new ErrorCodeInfo(nameof(Core), FixtureUtils.String(), FixtureUtils.String());
            var creationResult = CreationResult.Failed<DummyEntry>(errorCode);

            var result = creationResult
                .ForCommand<DummyEntry, FakeCreateCommand>(c => c)
                .ForCommand<DummyEntry, FakeCreateCommand>(c => c.Data);

            result.Entry.Should().BeNull();
            result.Errors.Count.Should().Be(2);

            var expectedError1 = new ExecutionError(errorCode, SourceBuilder.BuildErrorSource<FakeCreateCommand>(c => c));
            var expectedError2 = new ExecutionError(errorCode, SourceBuilder.BuildErrorSource<FakeCreateCommand>(c => c.Data));

            result.Errors.Should().ContainEquivalentOf(expectedError1);
            result.Errors.Should().ContainEquivalentOf(expectedError2);
        }

        private class DummyEntry
        {
        }
    }
}