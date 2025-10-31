namespace Microsoft.ComponentDetection.Contracts.Tests;

using System;
using AwesomeAssertions;
using Microsoft.ComponentDetection.Contracts.TypedComponent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
[TestCategory("Governance/All")]
[TestCategory("Governance/ComponentDetection")]
public class AzurePipelinesAgentComponentTests
{
    [TestMethod]
    public void Constructor_WithValidVersion_SetsProperties()
    {
        // Arrange
        var version = "2.214.1";

        // Act
        var component = new AzurePipelinesAgentComponent(version);

        // Assert
        component.Version.Should().Be(version);
        component.Type.Should().Be(ComponentType.AzurePipelinesAgent);
        component.Id.Should().Be($"azure-pipelines-agent {version}");
    }

    [TestMethod]
    public void Constructor_WithNullVersion_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action action = () => new AzurePipelinesAgentComponent(null);
        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Constructor_WithEmptyVersion_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action action = () => new AzurePipelinesAgentComponent(string.Empty);
        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void PackageUrl_ReturnsCorrectPackageUrl()
    {
        // Arrange
        var version = "2.214.1";
        var component = new AzurePipelinesAgentComponent(version);

        // Act
        var packageUrl = component.PackageUrl;

        // Assert
        packageUrl.Type.Should().Be("generic");
        packageUrl.Name.Should().Be("azure-pipelines-agent");
        packageUrl.Version.Should().Be(version);
        packageUrl.ToString().Should().Be($"pkg:generic/azure-pipelines-agent@{version}");
    }

    [TestMethod]
    public void Equals_SameVersion_ReturnsTrue()
    {
        // Arrange
        var version = "2.214.1";
        var component1 = new AzurePipelinesAgentComponent(version);
        var component2 = new AzurePipelinesAgentComponent(version);

        // Act & Assert
        component1.Equals(component2).Should().BeTrue();
        component1.GetHashCode().Should().Be(component2.GetHashCode());
    }

    [TestMethod]
    public void Equals_DifferentVersion_ReturnsFalse()
    {
        // Arrange
        var component1 = new AzurePipelinesAgentComponent("2.214.1");
        var component2 = new AzurePipelinesAgentComponent("2.215.0");

        // Act & Assert
        component1.Equals(component2).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_CaseInsensitiveVersion_ReturnsTrue()
    {
        // Arrange
        var component1 = new AzurePipelinesAgentComponent("2.214.1");
        var component2 = new AzurePipelinesAgentComponent("2.214.1");

        // Act & Assert
        component1.Equals(component2).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_DifferentComponentType_ReturnsFalse()
    {
        // Arrange
        var agentComponent = new AzurePipelinesAgentComponent("2.214.1");
        var npmComponent = new NpmComponent("some-package", "1.0.0");

        // Act & Assert
        agentComponent.Equals(npmComponent).Should().BeFalse();
    }

    [TestMethod]
    public void ToString_ReturnsId()
    {
        // Arrange
        var version = "2.214.1";
        var component = new AzurePipelinesAgentComponent(version);

        // Act
        var result = component.ToString();

        // Assert
        result.Should().Be($"azure-pipelines-agent {version}");
    }
}
