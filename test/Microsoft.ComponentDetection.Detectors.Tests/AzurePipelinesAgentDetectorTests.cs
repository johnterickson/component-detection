namespace Microsoft.ComponentDetection.Detectors.Tests;

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.ComponentDetection.Common.DependencyGraph;
using Microsoft.ComponentDetection.Contracts;
using Microsoft.ComponentDetection.Contracts.TypedComponent;
using Microsoft.ComponentDetection.Detectors.AzurePipelines;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
[TestCategory("Governance/All")]
[TestCategory("Governance/ComponentDetection")]
public class AzurePipelinesAgentDetectorTests
{
    private readonly Mock<IEnvironmentVariableService> mockEnvironmentVariableService;
    private readonly Mock<IFileUtilityService> mockFileUtilityService;
    private readonly Mock<ILogger<AzurePipelinesAgentDetector>> mockLogger;

    public AzurePipelinesAgentDetectorTests()
    {
        this.mockEnvironmentVariableService = new Mock<IEnvironmentVariableService>();
        this.mockFileUtilityService = new Mock<IFileUtilityService>();
        this.mockLogger = new Mock<ILogger<AzurePipelinesAgentDetector>>();
    }

    [TestMethod]
    public async Task ExecuteDetectorAsync_AgentHomeDirectoryNotSet_ReturnsSuccessWithNoComponents()
    {
        // Arrange
        this.mockEnvironmentVariableService.Setup(x => x.DoesEnvironmentVariableExist("AGENT_HOMEDIRECTORY"))
            .Returns(false);

        var detector = new AzurePipelinesAgentDetector(
            this.mockEnvironmentVariableService.Object,
            this.mockFileUtilityService.Object,
            this.mockLogger.Object);

        var componentRecorder = new ComponentRecorder();
        var scanRequest = new ScanRequest(
            new DirectoryInfo(Path.GetTempPath()),
            null,
            null,
            new Dictionary<string, string>(),
            null,
            componentRecorder);

        // Act
        var scanResult = await detector.ExecuteDetectorAsync(scanRequest);

        // Assert
        scanResult.ResultCode.Should().Be(ProcessingResultCode.Success);
        componentRecorder.GetDetectedComponents().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ExecuteDetectorAsync_AgentHomeDirectoryEmpty_ReturnsSuccessWithNoComponents()
    {
        // Arrange
        this.mockEnvironmentVariableService.Setup(x => x.DoesEnvironmentVariableExist("AGENT_HOMEDIRECTORY"))
            .Returns(true);
        this.mockEnvironmentVariableService.Setup(x => x.GetEnvironmentVariable("AGENT_HOMEDIRECTORY"))
            .Returns(string.Empty);

        var detector = new AzurePipelinesAgentDetector(
            this.mockEnvironmentVariableService.Object,
            this.mockFileUtilityService.Object,
            this.mockLogger.Object);

        var componentRecorder = new ComponentRecorder();
        var scanRequest = new ScanRequest(
            new DirectoryInfo(Path.GetTempPath()),
            null,
            null,
            new Dictionary<string, string>(),
            null,
            componentRecorder);

        // Act
        var scanResult = await detector.ExecuteDetectorAsync(scanRequest);

        // Assert
        scanResult.ResultCode.Should().Be(ProcessingResultCode.Success);
        componentRecorder.GetDetectedComponents().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ExecuteDetectorAsync_AgentWorkerExeNotFound_ReturnsSuccessWithNoComponents()
    {
        // Arrange
        var agentHomeDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\agent" : "/agent";
        var agentWorkerExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Agent.Worker.exe" : "Agent.Worker";
        var expectedAgentWorkerPath = Path.Combine(agentHomeDirectory, "bin", agentWorkerExecutable);

        this.mockEnvironmentVariableService.Setup(x => x.DoesEnvironmentVariableExist("AGENT_HOMEDIRECTORY"))
            .Returns(true);
        this.mockEnvironmentVariableService.Setup(x => x.GetEnvironmentVariable("AGENT_HOMEDIRECTORY"))
            .Returns(agentHomeDirectory);
        this.mockFileUtilityService.Setup(x => x.Exists(expectedAgentWorkerPath))
            .Returns(false);

        var detector = new AzurePipelinesAgentDetector(
            this.mockEnvironmentVariableService.Object,
            this.mockFileUtilityService.Object,
            this.mockLogger.Object);

        var componentRecorder = new ComponentRecorder();
        var scanRequest = new ScanRequest(
            new DirectoryInfo(Path.GetTempPath()),
            null,
            null,
            new Dictionary<string, string>(),
            null,
            componentRecorder);

        // Act
        var scanResult = await detector.ExecuteDetectorAsync(scanRequest);

        // Assert
        scanResult.ResultCode.Should().Be(ProcessingResultCode.Success);
        componentRecorder.GetDetectedComponents().Should().BeEmpty();
    }

    [TestMethod]
    public void Id_ReturnsExpectedDetectorId()
    {
        // Arrange & Act
        var detector = new AzurePipelinesAgentDetector(
            this.mockEnvironmentVariableService.Object,
            this.mockFileUtilityService.Object,
            this.mockLogger.Object);

        // Assert
        detector.Id.Should().Be("AzurePipelinesAgent");
    }

    [TestMethod]
    public void Categories_ReturnsExpectedCategory()
    {
        // Arrange & Act
        var detector = new AzurePipelinesAgentDetector(
            this.mockEnvironmentVariableService.Object,
            this.mockFileUtilityService.Object,
            this.mockLogger.Object);

        // Assert
        detector.Categories.Should().Contain("AzurePipelinesAgent");
    }

    [TestMethod]
    public void SupportedComponentTypes_ReturnsExpectedTypes()
    {
        // Arrange & Act
        var detector = new AzurePipelinesAgentDetector(
            this.mockEnvironmentVariableService.Object,
            this.mockFileUtilityService.Object,
            this.mockLogger.Object);

        // Assert
        detector.SupportedComponentTypes.Should().Contain(ComponentType.AzurePipelinesAgent);
    }

    [TestMethod]
    public void Version_ReturnsExpectedVersion()
    {
        // Arrange & Act
        var detector = new AzurePipelinesAgentDetector(
            this.mockEnvironmentVariableService.Object,
            this.mockFileUtilityService.Object,
            this.mockLogger.Object);

        // Assert
        detector.Version.Should().Be(1);
    }

    [TestMethod]
    public void NeedsAutomaticRootDependencyCalculation_ReturnsFalse()
    {
        // Arrange & Act
        var detector = new AzurePipelinesAgentDetector(
            this.mockEnvironmentVariableService.Object,
            this.mockFileUtilityService.Object,
            this.mockLogger.Object);

        // Assert
        detector.NeedsAutomaticRootDependencyCalculation.Should().BeFalse();
    }
}
