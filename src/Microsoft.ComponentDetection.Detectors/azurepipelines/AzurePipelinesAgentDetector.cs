namespace Microsoft.ComponentDetection.Detectors.AzurePipelines;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ComponentDetection.Contracts;
using Microsoft.ComponentDetection.Contracts.TypedComponent;
using Microsoft.Extensions.Logging;

/// <summary>
/// Detector for Azure Pipelines Agent components by checking the version of Agent.Worker(.exe)
/// in the AGENT_HOMEDIRECTORY environment variable location.
/// </summary>
public class AzurePipelinesAgentDetector : IComponentDetector, IDefaultOffComponentDetector
{
    private const string AgentHomeDirectoryEnvVar = "AGENT_HOMEDIRECTORY";
    private const string AgentBinDirectory = "bin";

    private static readonly string AgentWorkerExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "Agent.Worker.exe"
        : "Agent.Worker";

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

        this.logger.LogInformation("Starting Azure Pipelines Agent detection");

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
                this.logger.LogInformation("Azure Pipelines Agent not detected");
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
            this.logger.LogInformation("Environment variable {EnvVar} not found", AgentHomeDirectoryEnvVar);
            return null;
        }

        var agentHomeDirectory = this.environmentVariableService.GetEnvironmentVariable(AgentHomeDirectoryEnvVar);
        if (string.IsNullOrEmpty(agentHomeDirectory))
        {
            this.logger.LogInformation("Environment variable {EnvVar} is empty", AgentHomeDirectoryEnvVar);
            return null;
        }

        // Build path to Agent.Worker executable
        var agentWorkerPath = Path.Combine(agentHomeDirectory, AgentBinDirectory, AgentWorkerExecutable);

        this.logger.LogInformation("Looking for {Executable} at: {Path}", AgentWorkerExecutable, agentWorkerPath);

        // Check if Agent.Worker executable exists
        if (!this.fileUtilityService.Exists(agentWorkerPath))
        {
            this.logger.LogInformation("{Executable} not found at: {Path}", AgentWorkerExecutable, agentWorkerPath);
            return null;
        }

        // Get the assembly version
        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(agentWorkerPath);
            var version = assemblyName.Version?.ToString();

            if (!string.IsNullOrEmpty(version))
            {
                this.logger.LogInformation("Found {Executable} version: {Version}", AgentWorkerExecutable, version);
                return version;
            }
            else
            {
                this.logger.LogInformation("{Executable} found but no version information available", AgentWorkerExecutable);
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

        return AgentWorkerExecutable;
    }
}
