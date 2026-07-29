using CsvHelper;
using Moq;
using Xunit;
using PayrollBackendProject.Application.Services;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using System.Globalization;
using System.Text;

public class StatementExportServiceTests
{
    private readonly Mock<IPayRunRepository> _payRunRepo = new();
    private readonly Mock<IPayStatementRepository> _payStatementRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly string[] ExpectedHeaders =
    {
        "PayRunId", "PayRunStartDate", "PayRunEndDate", "PayRunPaymentDate", "PayStatementId", "ClinicianId",
        "ClinicianFirstName", "ClinicianLastName", "ClinicianEmail", "CostShareSnapshot",
        "TotalPayment", "TotalAdjustment", "Code500Deductions", "CostShareAdjustedPayment",
        "PsychTodayPayout", "TotalPayout", "StatementApprovedOn", "LineItemId", "PatientId",
        "DateOfService", "CPTCode", "PaymentId", "Payer", "AdjustmentCode", "AdjustmentCodeName",
        "PaymentAmount", "AdjustmentAmount", "AppliedDate", "PaymentDate", "RowNumber"
    };

    private StatementExportService CreateService()
    {
        return new StatementExportService(
            _payRunRepo.Object,
            _payStatementRepo.Object,
            _auditRepo.Object,
            _unitOfWork.Object
        );
    }

    private static UserAccount GenerateApprover()
    {
        return new UserAccount
        {
            Role = RoleEnum.BACKEND
        };
    }

    private static PaymentLineItem GeneratePaymentLineItem(
        Clinician clinician,
        decimal paymentAmount,
        decimal adjustmentAmount,
        PaymentAdjustmentCodeEnum code,
        string patientId)
    {
        return PaymentLineItem.GeneratePaymentLineItem(
            "raw",
            clinician,
            "Raw clin name",
            paymentAmount,
            adjustmentAmount,
            code,
            DateTime.UtcNow.AddDays(-3),
            patientId,
            "90837",
            Guid.NewGuid().ToString(),
            "payer",
            new EHRUser("test", "test", "test"),
            new ImportBatch("filename", Guid.NewGuid().ToString()),
            10,
            Guid.NewGuid().ToString(),
            DateTime.UtcNow.AddDays(-4),
            DateTime.UtcNow.AddDays(-4));
    }

    private static PayStatement GenerateStatement(
        PayRun payRun,
        Clinician clinician,
        bool approve,
        params PaymentLineItem[] lineItems)
    {
        var statement = PayStatement.GenerateDraftPayStatement(clinician, payRun);
        foreach (var lineItem in lineItems)
        {
            statement.AddPaymentLineItem(PaymentSnapshot.CreateSnapshot(lineItem, payRun));
        }
        statement.CalculateTotals();
        if (approve)
        {
            statement.Approve(GenerateApprover());
        }
        return statement;
    }

    private static List<string[]> ParseCsv(byte[] content, out string[] headers)
    {
        using var reader = new StreamReader(new MemoryStream(content), Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();
        headers = csv.HeaderRecord!;
        var rows = new List<string[]>();
        while (csv.Read())
        {
            var row = new string[headers.Length];
            for (int i = 0; i < headers.Length; i++)
            {
                row[i] = csv.GetField(i)!;
            }
            rows.Add(row);
        }
        return rows;
    }

    /*
    Validate that exporting throws an exception when the pay run does not exist
    */
    [Fact]
    public async Task ExportApprovedStatementsCsv_ShouldThrow_WhenPayRunNotFound()
    {
        var service = CreateService();

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync((PayRun?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ExportApprovedStatementsCsv(Guid.NewGuid(), Guid.NewGuid()));
    }

    /*
    Validate that exporting throws an exception when the pay run has no approved statements
    */
    [Fact]
    public async Task ExportApprovedStatementsCsv_ShouldThrow_WhenNoApprovedStatements()
    {
        var service = CreateService();
        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var clinician = new Clinician("A", "B", "AB@AB.com");
        var pendingStatement = GenerateStatement(payRun, clinician, approve: false,
            GeneratePaymentLineItem(clinician, 100m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT, "111"));

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(payRun);
        _payStatementRepo.Setup(r => r.GetPayStatementsForPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PayStatement> { pendingStatement });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportApprovedStatementsCsv(Guid.NewGuid(), Guid.NewGuid()));
    }

    /*
    Validate that only approved statements are included in the export and that
    pending statements are excluded
    */
    [Fact]
    public async Task ExportApprovedStatementsCsv_ShouldOnlyIncludeApprovedStatements()
    {
        var service = CreateService();
        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var approvedClinician = new Clinician("Approved", "Clinician", "a@a.com");
        var pendingClinician = new Clinician("Pending", "Clinician", "p@p.com");

        var approvedStatement = GenerateStatement(payRun, approvedClinician, approve: true,
            GeneratePaymentLineItem(approvedClinician, 100m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT, "111"),
            GeneratePaymentLineItem(approvedClinician, 50m, -10m, PaymentAdjustmentCodeEnum.CONTRACT_WRITEOFF, "222"));
        var pendingStatement = GenerateStatement(payRun, pendingClinician, approve: false,
            GeneratePaymentLineItem(pendingClinician, 75m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT, "333"));

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(payRun);
        _payStatementRepo.Setup(r => r.GetPayStatementsForPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PayStatement> { approvedStatement, pendingStatement });

        var result = await service.ExportApprovedStatementsCsv(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(1, result.StatementCount);
        Assert.Equal(2, result.RowCount);

        var rows = ParseCsv(result.Content, out var headers);
        Assert.Equal(2, rows.Count);
        int statementIdIndex = Array.IndexOf(headers, "PayStatementId");
        Assert.All(rows, row => Assert.Equal(approvedStatement.Id.ToString(), row[statementIdIndex]));
    }

    /*
    Validate that the CSV header row exactly matches the export schema contract
    consumed by the local statement rebuild tool
    */
    [Fact]
    public async Task ExportApprovedStatementsCsv_ShouldMatchSchemaContract()
    {
        var service = CreateService();
        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var clinician = new Clinician("A", "B", "AB@AB.com");
        var statement = GenerateStatement(payRun, clinician, approve: true,
            GeneratePaymentLineItem(clinician, 100m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT, "111"));

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(payRun);
        _payStatementRepo.Setup(r => r.GetPayStatementsForPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PayStatement> { statement });

        var result = await service.ExportApprovedStatementsCsv(Guid.NewGuid(), Guid.NewGuid());

        ParseCsv(result.Content, out var headers);
        Assert.Equal(ExpectedHeaders, headers);
    }

    /*
    Validate that amounts are exported with the stored domain sign convention,
    including negative 500-code adjustment amounts
    */
    [Fact]
    public async Task ExportApprovedStatementsCsv_ShouldPreserveStoredSignConvention()
    {
        var service = CreateService();
        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var clinician = new Clinician("A", "B", "AB@AB.com");
        var statement = GenerateStatement(payRun, clinician, approve: true,
            GeneratePaymentLineItem(clinician, 95m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT, "111"),
            GeneratePaymentLineItem(clinician, 0m, -25m, PaymentAdjustmentCodeEnum.INSURANCE_TAKEBACK, "222"));

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(payRun);
        _payStatementRepo.Setup(r => r.GetPayStatementsForPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PayStatement> { statement });

        var result = await service.ExportApprovedStatementsCsv(Guid.NewGuid(), Guid.NewGuid());

        var rows = ParseCsv(result.Content, out var headers);
        int paymentIndex = Array.IndexOf(headers, "PaymentAmount");
        int adjustmentIndex = Array.IndexOf(headers, "AdjustmentAmount");
        int codeNameIndex = Array.IndexOf(headers, "AdjustmentCodeName");

        var takebackRow = rows.Single(r => r[codeNameIndex] == "INSURANCE_TAKEBACK");
        Assert.Equal("0.00", takebackRow[paymentIndex]);
        Assert.Equal("-25.00", takebackRow[adjustmentIndex]);

        var paymentRow = rows.Single(r => r[codeNameIndex] == "INSURANCE_PAYMENT");
        Assert.Equal("95.00", paymentRow[paymentIndex]);
    }

    /*
    Validate that the export filename contains the pay run period
    */
    [Fact]
    public async Task ExportApprovedStatementsCsv_ShouldGeneratePeriodFilename()
    {
        var service = CreateService();
        var start = new DateTime(2026, 6, 1);
        var end = new DateTime(2026, 6, 30);
        var payRun = PayRun.GeneratePayRun(start, end);
        var clinician = new Clinician("A", "B", "AB@AB.com");
        var statement = GenerateStatement(payRun, clinician, approve: true,
            GeneratePaymentLineItem(clinician, 100m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT, "111"));

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(payRun);
        _payStatementRepo.Setup(r => r.GetPayStatementsForPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PayStatement> { statement });

        var result = await service.ExportApprovedStatementsCsv(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("approved-statements_20260601-20260630.csv", result.FileName);
    }

    /*
    Validate that the pay run's payment date is included on every exported row
    */
    [Fact]
    public async Task ExportApprovedStatementsCsv_ShouldIncludePaymentDate()
    {
        var service = CreateService();
        var paymentDate = DateTime.SpecifyKind(new DateTime(2026, 8, 15), DateTimeKind.Utc);
        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, paymentDate);
        var clinician = new Clinician("A", "B", "AB@AB.com");
        var statement = GenerateStatement(payRun, clinician, approve: true,
            GeneratePaymentLineItem(clinician, 100m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT, "111"));

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(payRun);
        _payStatementRepo.Setup(r => r.GetPayStatementsForPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PayStatement> { statement });

        var result = await service.ExportApprovedStatementsCsv(Guid.NewGuid(), Guid.NewGuid());

        var rows = ParseCsv(result.Content, out var headers);
        int paymentDateIndex = Array.IndexOf(headers, "PayRunPaymentDate");
        Assert.All(rows, row => Assert.Equal("2026-08-15", row[paymentDateIndex]));
    }

    /*
    Validate that exporting writes an audit log entry and persists it
    */
    [Fact]
    public async Task ExportApprovedStatementsCsv_ShouldWriteAuditLog()
    {
        var service = CreateService();
        var payRun = PayRun.GeneratePayRun(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var clinician = new Clinician("A", "B", "AB@AB.com");
        var statement = GenerateStatement(payRun, clinician, approve: true,
            GeneratePaymentLineItem(clinician, 100m, 0m, PaymentAdjustmentCodeEnum.INSURANCE_PAYMENT, "111"));

        _payRunRepo.Setup(r => r.GetPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(payRun);
        _payStatementRepo.Setup(r => r.GetPayStatementsForPayRun(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PayStatement> { statement });

        await service.ExportApprovedStatementsCsv(Guid.NewGuid(), Guid.NewGuid());

        _auditRepo.Verify(r => r.AddAuditLog(It.Is<AuditLog>(l => l.Action == AuditLogActionEnum.EXPORTED)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
