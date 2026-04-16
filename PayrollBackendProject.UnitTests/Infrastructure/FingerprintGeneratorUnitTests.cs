using Xunit;
using PayrollBackendProject.Infrastructure.Utilities;
using System.Text;

namespace PayrollBackendProject.UnitTests;

public class FingerprintGeneratorUnitTests
{
    /*
    Test that FileComputeSHA256Async returns a non-empty hash
    */
    [Fact]
    public async Task FileComputeSHA256Async_ReturnsNonEmptyString()
    {
        // Arrange
        var generator = new FingerprintGenerator();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test data"));

        // Act
        var hash = await generator.FileComputeSHA256Async(stream);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    /*
    Test that FileComputeSHA256Async returns the same hash for the same input
    */
    [Fact]
    public async Task FileComputeSHA256Async_SameInput_ReturnsSameHash()
    {
        // Arrange
        var generator = new FingerprintGenerator();
        var data = Encoding.UTF8.GetBytes("same data");

        var stream1 = new MemoryStream(data);
        var stream2 = new MemoryStream(data);

        // Act
        var hash1 = await generator.FileComputeSHA256Async(stream1);
        var hash2 = await generator.FileComputeSHA256Async(stream2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    /*
    Test that FileComputeSHA256Async returns different hashes for different input
    */
    [Fact]
    public async Task FileComputeSHA256Async_DifferentInput_ReturnsDifferentHash()
    {
        // Arrange
        var generator = new FingerprintGenerator();

        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("data1"));
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("data2"));

        // Act
        var hash1 = await generator.FileComputeSHA256Async(stream1);
        var hash2 = await generator.FileComputeSHA256Async(stream2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    /*
    Test that LineItemComputeSHA256Async returns a non-empty hash
    */
    [Fact]
    public async Task LineItemComputeSHA256Async_ReturnsNonEmptyString()
    {
        // Arrange
        var generator = new FingerprintGenerator();

        // Act
        var hash = await generator.LineItemComputeSHA256Async("raw", "batch1", "1");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    /*
    Test that LineItemComputeSHA256Async returns same hash for same input
    */
    [Fact]
    public async Task LineItemComputeSHA256Async_SameInput_ReturnsSameHash()
    {
        // Arrange
        var generator = new FingerprintGenerator();

        // Act
        var hash1 = await generator.LineItemComputeSHA256Async("raw", "batch1", "1");
        var hash2 = await generator.LineItemComputeSHA256Async("raw", "batch1", "1");

        // Assert
        Assert.Equal(hash1, hash2);
    }

    /*
    Test that LineItemComputeSHA256Async changes when any input changes
    */
    [Fact]
    public async Task LineItemComputeSHA256Async_InputChange_ChangesHash()
    {
        // Arrange
        var generator = new FingerprintGenerator();

        // Act
        var baseHash = await generator.LineItemComputeSHA256Async("raw", "batch1", "1");

        var differentRaw = await generator.LineItemComputeSHA256Async("raw2", "batch1", "1");
        var differentBatch = await generator.LineItemComputeSHA256Async("raw", "batch2", "1");
        var differentRow = await generator.LineItemComputeSHA256Async("raw", "batch1", "2");

        // Assert
        Assert.NotEqual(baseHash, differentRaw);
        Assert.NotEqual(baseHash, differentBatch);
        Assert.NotEqual(baseHash, differentRow);
    }

    /*
    Test that hash output is uppercase hex string
    */
    [Fact]
    public async Task LineItemComputeSHA256Async_ReturnsUppercaseHex()
    {
        // Arrange
        var generator = new FingerprintGenerator();

        // Act
        var hash = await generator.LineItemComputeSHA256Async("raw", "batch1", "1");

        // Assert
        Assert.Matches("^[A-F0-9]+$", hash);
    }

    /*
    Test that FileComputeSHA256Async handles empty stream
    */
    [Fact]
    public async Task FileComputeSHA256Async_EmptyStream_ReturnsValidHash()
    {
        // Arrange
        var generator = new FingerprintGenerator();
        var stream = new MemoryStream();

        // Act
        var hash = await generator.FileComputeSHA256Async(stream);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }
}