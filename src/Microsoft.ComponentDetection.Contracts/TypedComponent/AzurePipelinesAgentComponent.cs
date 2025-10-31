namespace Microsoft.ComponentDetection.Contracts.TypedComponent;

using PackageUrl;

public class AzurePipelinesAgentComponent : TypedComponent
{
    private AzurePipelinesAgentComponent()
    {
        // Reserved for deserialization
    }

    public AzurePipelinesAgentComponent(string version) =>
        this.Version = this.ValidateRequiredInput(version, nameof(this.Version), nameof(ComponentType.AzurePipelinesAgent));

    public string Version { get; }

    public override ComponentType Type => ComponentType.AzurePipelinesAgent;

    public override PackageURL PackageUrl => new PackageURL("generic", null, "azure-pipelines-agent", this.Version, null, null);

    protected override string ComputeId() => $"azure-pipelines-agent {this.Version} - {this.Type}";
}
