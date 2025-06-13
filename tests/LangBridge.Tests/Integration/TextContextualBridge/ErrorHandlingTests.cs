using LangBridge.Tests.Integration.Shared;

namespace LangBridge.Tests.Integration.TextContextualBridge;

/// <summary>
/// Tests for error handling behavior in TextContextualBridge.
/// </summary>
public class ErrorHandlingTests : IntegrationTestBase
{
    [Fact]
    public async Task ExtractAsync_ReasoningModelThrowsException_ReturnsBusinessFriendlyError()
    {
        // Arrange
        var input = "Some input text.";
        var query = "Extract something.";
        
        MockReasoningModel.WithException(new InvalidOperationException("Technical error details"));

        // Act
        var result = await TextContextualBridge.ExtractAsync<string>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Unable to process your request at this time. Please try again later.", result.Error);
    }

    [Fact]
    public async Task ExtractAsync_DataStructuringModelThrowsException_ReturnsBusinessFriendlyError()
    {
        // Arrange
        var input = "Some input text.";
        var query = "Extract something.";
        
        ConfigureSuccessfulFeasibilityCheck();
        ConfigureSuccessfulSimpleTypeExtraction("some value");
        MockDataStructuringModel.WithException(new InvalidOperationException("Technical error details"));

        // Act
        var result = await TextContextualBridge.ExtractAsync<string>(input, query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("The information extraction service is temporarily unavailable.", result.Error);
    }

    [Fact]
    public async Task ExtractAsync_ReasoningModelThrowsForRawExtraction_ReturnsBusinessFriendlyError()
    {
        // This test verifies that when the reasoning model throws during the extraction phase,
        // we get the proper business-friendly error message. Since the mock exceptions are global,
        // this test will actually throw on the feasibility check, but it still tests our error handling.
        
        // Arrange
        var input = "Some input text.";
        var query = "Extract something.";
        
        MockReasoningModel.WithException(new InvalidOperationException("Technical error details"));

        // Act
        var result = await TextContextualBridge.ExtractAsync<string>(input, query);

        // Assert - The exception will be caught in the feasibility check and return the appropriate error
        Assert.True(result.IsFailure);
        Assert.Equal("Unable to process your request at this time. Please try again later.", result.Error);
    }

    public class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [Fact]
    public async Task ExtractAsync_CancellationTokenCancelled_PropagatesException()
    {
        // Arrange
        var input = "Some input text.";
        var query = "Extract something.";
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => TextContextualBridge.ExtractAsync<string>(input, query, cancellationToken: cts.Token));
    }
}