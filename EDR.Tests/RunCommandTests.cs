using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using CommandDotNet;
using CommandDotNet.IoC.MicrosoftDependencyInjection;
using CommandDotNet.TestTools;
using FluentAssertions;
using MELT;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Reductech.EDR;
using Reductech.EDR.ConnectorManagement;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Xunit;

namespace EDR.Tests
{

public class RunCommandTests
{
    private const string TheUltimateTestString = "Hello World";

    private static IServiceProvider GetDefaultServiceProvider() =>
        GetDefaultServiceProvider(null, null, null);

    private static IServiceProvider GetDefaultServiceProvider(ILoggerFactory loggerFactory) =>
        GetDefaultServiceProvider(loggerFactory, null, null);

    private static IServiceProvider GetDefaultServiceProvider(
        ILoggerFactory? loggerFactory,
        IFileSystem? fileSystem,
        IConnectorManager? connectorManager)
    {
        var lf = loggerFactory ?? TestLoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.Debug));
        var fs = fileSystem ?? new MockFileSystem();
        var connMan = connectorManager ?? new FakeConnectorManager();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(new ConnectorCommand(connMan))
            .AddSingleton(new RunCommand(lf.CreateLogger<RunCommand>(), fs, connMan))
            .AddSingleton(new StepsCommand(connMan))
            .AddSingleton(new ValidateCommand(lf.CreateLogger<ValidateCommand>(), fs, connMan))
            .AddSingleton<EDRMethods>()
            .BuildServiceProvider();

        return serviceProvider;
    }

    [Fact]
    public void RunSCL_WhenSCLFunctionIsSuccess_ReturnsSuccess()
    {
        var factory = TestLoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.Debug));

        var sp = GetDefaultServiceProvider(factory);

        var result = new AppRunner<EDRMethods>()
            .UseMicrosoftDependencyInjection(sp)
            .UseDefaultMiddleware()
            .RunInMem($"run scl \"Log '{TheUltimateTestString}'\"");

        result.ExitCode.Should().Be(0);
        result.Console.OutText().Should().Be(string.Empty);

        factory.Sink.LogEntries.Select(x => x.Message)
            .Should()
            .BeEquivalentTo(
                "EDR Sequence Started",
                TheUltimateTestString,
                "EDR Sequence Completed"
            );
    }

    [Fact]
    public void RunSCL_WhenSCLFunctionIsFailure_ReturnsFailure()
    {
        var factory = TestLoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.Debug));

        var sp = GetDefaultServiceProvider(factory);

        var result = new AppRunner<EDRMethods>()
            .UseMicrosoftDependencyInjection(sp)
            .UseDefaultMiddleware()
            .RunInMem($"run scl \"Loog '{TheUltimateTestString}'\"");

        result.ExitCode.Should().Be(1);
        result.Console.OutText().Should().Be(string.Empty);

        Assert.Contains(
            factory.Sink.LogEntries,
            l => l.LogLevel == LogLevel.Error
              && l.Message!.Contains("The step 'Loog' does not exist")
        );
    }

    [Fact]
    public void RunSCL_WhenSCLIsEmpty_Throws()
    {
        var sp = GetDefaultServiceProvider();

        var error = Assert.Throws<CommandLineArgumentException>(
            () => new AppRunner<EDRMethods>()
                .UseMicrosoftDependencyInjection(sp)
                .UseDefaultMiddleware()
                .RunInMem("run scl \"\"")
        );

        Assert.Equal("Please provide a valid SCL string.", error.Message);
    }

    [Fact]
    public void RunSCL_WhenRunnerIsFailure_LogsErrorAndReturnsFailure()
    {
        var factory = TestLoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.Debug));
        var fs      = new MockFileSystem();
        var connMan = new FakeConnectorManager();

        var run = new Mock<RunCommand>(factory.CreateLogger<RunCommand>(), fs, connMan);

        run.Setup(r => r.GetInjectedContexts(It.IsAny<StepFactoryStore>(), It.IsAny<SCLSettings>()))
            .Returns(() => new ErrorBuilder(ErrorCode.Unknown, "Just Testing"));

        var sp = new ServiceCollection()
            .AddSingleton(new ConnectorCommand(connMan))
            .AddSingleton(run.Object)
            .AddSingleton(new StepsCommand(connMan))
            .AddSingleton(new ValidateCommand(factory.CreateLogger<ValidateCommand>(), fs, connMan))
            .AddSingleton<EDRMethods>()
            .BuildServiceProvider();

        var result = new AppRunner<EDRMethods>()
            .UseMicrosoftDependencyInjection(sp)
            .UseDefaultMiddleware()
            .RunInMem($"run scl \"Log '{TheUltimateTestString}'\"");

        result.ExitCode.Should().Be(1);
        result.Console.OutText().Should().Be(string.Empty);

        Assert.Contains(
            factory.Sink.LogEntries,
            l => l.LogLevel == LogLevel.Error
              && l.Message!.Contains("Unknown Error: 'Just Testing'")
        );
    }

    [Fact]
    public void RunPath_WhenPathIsEmpty_Throws()
    {
        var sp = GetDefaultServiceProvider();

        var error = Assert.Throws<CommandLineArgumentException>(
            () => new AppRunner<EDRMethods>()
                .UseMicrosoftDependencyInjection(sp)
                .UseDefaultMiddleware()
                .RunInMem("run \"\"")
        );

        Assert.Equal("Please provide a path to a valid SCL file.", error.Message);
    }

    [Fact]
    public void RunPath_WhenSCLFunctionIsSuccess_ReturnsSuccess()
    {
        const string path = @"c:\temp\file.scl";

        var factory = TestLoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.Debug));
        var fs      = new MockFileSystem();
        fs.AddFile(path, $"- Log '{TheUltimateTestString}'");

        var sp = GetDefaultServiceProvider(factory, fs, null);

        var result = new AppRunner<EDRMethods>()
            .UseMicrosoftDependencyInjection(sp)
            .UseDefaultMiddleware()
            .RunInMem($"run {path}");

        result.ExitCode.Should().Be(0);
        result.Console.OutText().Should().Be(string.Empty);

        factory.Sink.LogEntries.Select(x => x.Message)
            .Should()
            .BeEquivalentTo(
                "EDR Sequence Started",
                TheUltimateTestString,
                "EDR Sequence Completed"
            );
    }
}

}
