using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.UnitTests;

public class ImportBatchUnitTests
{
    /*
    Test that constructor sets provided values correctly
    */
    [Theory]
    [InlineData("file1.csv", "fingerprint1")]
    [InlineData("batch.csv", "abc123")]
    public void Constructor_ShouldSetProvidedValues(string filename, string fingerprint)
    {
        var batch = new ImportBatch(filename, fingerprint);

        Assert.Equal(filename, batch.Filename);
        Assert.Equal(fingerprint, batch.Fingerprint);
        Assert.Equal(ImportStatusEnum.PENDING, batch.ImportStatus);
        Assert.Equal(0, batch.FailedItems);
        Assert.Equal(0, batch.SuccessfulItems);
        Assert.Equal(0, batch.TotalRows);
    }

    /*
    Test that constructor throws when filename or fingerprint is invalid
    */
    [Theory]
    [InlineData("", "fingerprint")]
    [InlineData("file.csv", "")]
    [InlineData("", "")]
    public void Constructor_ShouldThrow_WhenFilenameOrFingerprintInvalid(string filename, string fingerprint)
    {
        Assert.Throws<ArgumentException>(() =>
            new ImportBatch(filename, fingerprint)
        );
    }

    /*
    Test that a valid Guid is assigned
    */
    [Fact]
    public void Constructor_ShouldAssignValidGuid()
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        Assert.NotEqual(Guid.Empty, batch.Id);
    }

    /*
    Test that UploadTime is set correctly
    */
    [Fact]
    public void Constructor_ShouldSetUploadTime()
    {
        var before = DateTime.UtcNow;

        var batch = new ImportBatch("file.csv", "fingerprint");

        var after = DateTime.UtcNow;

        Assert.InRange(batch.UploadTime, before, after);
    }

    /*
    Test UpdateStatus sets status and timestamp correctly
    */
    [Theory]
    [InlineData(ImportStatusEnum.PROCESSING)]
    [InlineData(ImportStatusEnum.SUCCESS)]
    [InlineData(ImportStatusEnum.FAILED)]
    public void UpdateStatus_ShouldSetStatusAndTimestamp(ImportStatusEnum status)
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        var before = DateTime.UtcNow;

        batch.UpdateStatus(status);

        var after = DateTime.UtcNow;

        Assert.Equal(status, batch.ImportStatus);
        Assert.InRange(batch.StatusTime!.Value, before, after);
    }

    /*
    Test SetResults sets all fields correctly
    */
    [Fact]
    public void SetResults_ShouldSetAllFields()
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        var errors = new List<string> { "error1" };

        batch.SetResults(1, 4, 5, 0, errors);

        Assert.Equal(1, batch.FailedItems);
        Assert.Equal(4, batch.SuccessfulItems);
        Assert.Equal(5, batch.TotalRows);
        Assert.Equal(0, batch.UnresolvedRows);
        Assert.Equal(errors, batch.Errors);
    }

    /*
    Test SetResults throws when invalid row counts are provided
    */
    [Theory]
    [InlineData(-1, 1, 5, 0)] // failed < 0
    [InlineData(6, 1, 5, 0)]  // failed > total
    public void SetResults_ShouldThrow_WhenFailedRowsInvalid(int failed, int success, int total, int unresolved)
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        Assert.Throws<ArgumentException>(() =>
            batch.SetResults(failed, success, total, unresolved, new List<string>())
        );
    }

    [Theory]
    [InlineData(1, -1, 5, 0)] // success < 0
    [InlineData(1, 6, 5, 0)]  // success > total
    public void SetResults_ShouldThrow_WhenSuccessfulRowsInvalid(int failed, int success, int total, int unresolved)
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        Assert.Throws<ArgumentException>(() =>
            batch.SetResults(failed, success, total, unresolved, new List<string>())
        );
    }

    [Theory]
    [InlineData(1, 1, 5, -1)] // unresolved < 0
    [InlineData(1, 1, 5, 6)]  // unresolved > total
    public void SetResults_ShouldThrow_WhenUnresolvedRowsInvalid(int failed, int success, int total, int unresolved)
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        Assert.Throws<ArgumentException>(() =>
            batch.SetResults(failed, success, total, unresolved, new List<string>())
        );
    }

    /*
    Test SetResults sets SUCCESS
    */
    [Fact]
    public void SetResults_ShouldSetStatusSuccess()
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        batch.SetResults(0, 5, 5, 0, new List<string>());

        Assert.Equal(ImportStatusEnum.SUCCESS, batch.ImportStatus);
    }

    /*
    Test SetResults sets SUCCESS_WITH_ERRORS
    */
    [Theory]
    [InlineData(1, 4, 5, 0)]
    [InlineData(0, 5, 5, 1)]
    public void SetResults_ShouldSetStatusSuccessWithErrors(int failed, int success, int total, int unresolved)
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        batch.SetResults(failed, success, total, unresolved, new List<string>());

        Assert.Equal(ImportStatusEnum.SUCCESS_WITH_ERRORS, batch.ImportStatus);
    }

    /*
    Test SetResults sets FAILED
    */
    [Fact]
    public void SetResults_ShouldSetStatusFailed()
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        batch.SetResults(5, 0, 5, 0, new List<string>());

        Assert.Equal(ImportStatusEnum.FAILED, batch.ImportStatus);
    }

    /*
    Test AssignFilepath sets value correctly
    */
    [Theory]
    [InlineData("/tmp/file.csv")]
    [InlineData("C:\\uploads\\file.csv")]
    public void AssignFilepath_ShouldSetFilepath(string path)
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        batch.AssignFilepath(path);

        Assert.Equal(path, batch.Filepath);
    }

    /*
    Test AssignFilepath throws when invalid
    */
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AssignFilepath_ShouldThrow_WhenInvalid(string path)
    {
        var batch = new ImportBatch("file.csv", "fingerprint");

        Assert.Throws<ArgumentException>(() =>
            batch.AssignFilepath(path)
        );
    }
}