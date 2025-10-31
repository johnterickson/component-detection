namespace Microsoft.ComponentDetection.Detectors.AzurePipelines;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ComponentDetection.Contracts;
using Microsoft.ComponentDetection.Contracts.TypedComponent;
using Microsoft.Extensions.Logging;

/// <summary>
/// Detector for Azure Pipelines Agent components by checking the version of Agent.Worker.exe
/// in the AGENT_HOMEDIRECTORY environment variable location.
/// </summary>
public class AzurePipelinesAgentDetector : IComponentDetector, IDefaultOffComponentDetector
{
    private const string AgentHomeDirectoryEnvVar = "AGENT_HOMEDIRECTORY";
    private const string AgentWorkerExecutable = "Agent.Worker.exe";
    private const string AgentBinDirectory = "bin";

    private readonly IEnvironmentVariableService environmentVariableService;
    private readonly IFileUtilityService fileUtilityService;
    private readonly ILogger<AzurePipelinesAgentDetector> logger;

    public AzurePipelinesAgentDetector(
        IEnvironmentVariableService environmentVariableService,
        IFileUtilityService fileUtilityService,
        ILogger<AzurePipelinesAgentDetector> logger)
    {
        this.environmentVariableService = environmentVariableService;
        this.fileUtilityService = fileUtilityService;
        this.logger = logger;
    }

    public string Id => "AzurePipelinesAgent";

    public IEnumerable<string> Categories => [Enum.GetName(typeof(DetectorClass), DetectorClass.AzurePipelinesAgent)];

    public IEnumerable<ComponentType> SupportedComponentTypes => [ComponentType.AzurePipelinesAgent];

    public int Version => 1;

    public bool NeedsAutomaticRootDependencyCalculation => false;

    public Task<IndividualDetectorScanResult> ExecuteDetectorAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        var componentRecorder = request.ComponentRecorder;
        var singleFileComponentRecorder = componentRecorder.CreateSingleFileComponentRecorder(this.GetDetectorFilePath());

        this.logger.LogDebug("Starting Azure Pipelines Agent detection");

        try
        {
            var agentVersion = this.GetAzurePipelinesAgentVersion();
            if (!string.IsNullOrEmpty(agentVersion))
            {
                var component = new AzurePipelinesAgentComponent(agentVersion);
                singleFileComponentRecorder.RegisterUsage(new DetectedComponent(component));

                this.logger.LogInformation("Detected Azure Pipelines Agent version: {Version}", agentVersion);
            }
            else
            {
                this.logger.LogDebug("Azure Pipelines Agent not detected");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to detect Azure Pipelines Agent version");
        }

        return Task.FromResult(new IndividualDetectorScanResult
        {
            ResultCode = ProcessingResultCode.Success,
            AdditionalTelemetryDetails = [],
        });
    }

    private string GetAzurePipelinesAgentVersion()
    {
        // Check if AGENT_HOMEDIRECTORY environment variable exists
        if (!this.environmentVariableService.DoesEnvironmentVariableExist(AgentHomeDirectoryEnvVar))
        {
            this.logger.LogDebug("Environment variable {EnvVar} not found", AgentHomeDirectoryEnvVar);
            return null;
        }

        var agentHomeDirectory = this.environmentVariableService.GetEnvironmentVariable(AgentHomeDirectoryEnvVar);
        if (string.IsNullOrEmpty(agentHomeDirectory))
        {
            this.logger.LogDebug("Environment variable {EnvVar} is empty", AgentHomeDirectoryEnvVar);
            return null;
        }

        // Build path to Agent.Worker.exe
        var agentWorkerPath = Path.Combine(agentHomeDirectory, AgentBinDirectory, AgentWorkerExecutable);

        this.logger.LogDebug("Looking for Agent.Worker.exe at: {Path}", agentWorkerPath);

        // Check if Agent.Worker.exe exists
        if (!this.fileUtilityService.Exists(agentWorkerPath))
        {
            this.logger.LogDebug("Agent.Worker.exe not found at: {Path}", agentWorkerPath);
            return null;
        }

        // Get the file version
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(agentWorkerPath);
            var version = versionInfo.FileVersion;

            if (!string.IsNullOrEmpty(version))
            {
                this.logger.LogDebug("Found Agent.Worker.exe version: {Version}", version);
                return version;
            }
            else
            {
                this.logger.LogDebug("Agent.Worker.exe found but no version information available");
                return null;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to get version information from {Path}", agentWorkerPath);
            return null;
        }
    }

    private string GetDetectorFilePath()
    {
        var agentHomeDirectory = this.environmentVariableService.GetEnvironmentVariable(AgentHomeDirectoryEnvVar);
        if (!string.IsNullOrEmpty(agentHomeDirectory))
        {
            return Path.Combine(agentHomeDirectory, AgentBinDirectory, AgentWorkerExecutable);
        }

        return "Agent.Worker.exe";
    }
}
