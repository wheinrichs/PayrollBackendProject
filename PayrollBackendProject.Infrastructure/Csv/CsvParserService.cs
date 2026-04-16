using CsvHelper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Infrastructure.Utilities;
using System.Globalization;

namespace PayrollBackendProject.Application.Services
{
    public class CsvParserService : ICsvParserService
    {
        private readonly IPaymentRepository _repo;
        private readonly IClinicianRepository _clinicianRepo;
        private readonly IUserAccountRepository _userRepo;
        private readonly IEHRUserAccountRepository _ehrUserRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFingerprintGenerator _fingerprintGenerator;

        public CsvParserService(IPaymentRepository repo, 
            IClinicianRepository clinicianRepo, 
            IUserAccountRepository userRepo, 
            IEHRUserAccountRepository ehrRepo, 
            IUnitOfWork unitOfWork,
            IFingerprintGenerator fingerprintGenerator)
        {
            _repo = repo;
            _clinicianRepo = clinicianRepo;
            _userRepo = userRepo;
            _ehrUserRepo = ehrRepo;
            _unitOfWork = unitOfWork;
            _fingerprintGenerator = fingerprintGenerator;
        }

        public async Task<UploadResult?> Parse(ImportBatch batch)
        {
            // Check if the batch has already been processed in the past
            bool parsingExistingBatch = false;
            if (batch.ImportStatus != ImportStatusEnum.PENDING)
            {
                // Modify the existing batch
                parsingExistingBatch = true;
            }
            else
            {
                parsingExistingBatch = false;
            }

            // Create a result item for the action
            UploadResult result = new(batch.Filename);

            // Open the file as an input stream and parse the csv contents into line items
            using var stream = new FileStream(batch.Filepath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CsvMapper>();
            int rowNumber = 0;
            int failedRows = 0;
            int skippedRows = 0;
            int unresolvedRows = 0;
            int successfulRows = 0;

            // Retrieve all the line items to check against
            var payments = await _repo.GetAllPaymentFingerprints();
            var paymentHashSet = new HashSet<string>(payments);
            var clinicians = await _clinicianRepo.GetAllClinicians();
            var users = await _ehrUserRepo.GetAllUsers();
            List<PaymentLineItem> existingBatchPayments = await _repo.GetPaymentsFromBatch(batch.Id);
            var existingMap = existingBatchPayments.ToDictionary(p => p.Fingerprint);
            var existingBatchPaymentsHashSet = new HashSet<string>(existingBatchPayments.Select(p => p.Fingerprint));


            batch.UpdateStatus(ImportStatusEnum.PROCESSING);

            while (csv.Read())
            {
                // Generate the object and the fingerprint from the csv row
                rowNumber++;
                string rawData = "";
                PaymentCsvRow? mappedRow = null;
                try
                {
                    rawData = csv.Context?.Parser?.RawRecord.TrimEnd() ?? "";
                    mappedRow = csv.GetRecord<PaymentCsvRow>();
                }
                catch (Exception e)
                {
                    result.Errors.Add($"Row {rowNumber} failed to parse: {e.Message}");
                    failedRows++;
                    continue;
                }

                string lineItemFingerprint = await _fingerprintGenerator.LineItemComputeSHA256Async(batch.Id.ToString(), rawData, rowNumber.ToString());

                try
                {
                    // Check if this is a new line item to add
                    if (!paymentHashSet.Contains(lineItemFingerprint))
                    {
                        // Line item does not exist so add 
                        Clinician? clinician = FindClinician(mappedRow.ClinicianName, clinicians);
                        if (clinician == null)
                        {
                            result.Errors.Add($"Failed to resolve clinician for line item {rowNumber}: Could not find corresponding clinician for {mappedRow.ClinicianName}");
                            unresolvedRows++;
                        }
                        EHRUser appliedBy = FindAppliedByUser(mappedRow.AppliedBy, users);
                        PaymentLineItem domainLineItem = PaymentLineItemMapper.DtoToDomain(mappedRow, rawData, clinician, appliedBy, batch, rowNumber, lineItemFingerprint);

                        // Add to the repo
                        await _repo.AddLineItem(domainLineItem);
                        paymentHashSet.Add(lineItemFingerprint);
                        successfulRows++;
                    }
                    // If the line item already exists in the DB and not parsing an existing batch or the existing batch does not contain the item
                    else if (!parsingExistingBatch || !existingBatchPaymentsHashSet.Contains(lineItemFingerprint))
                    {
                        result.Errors.Add($"Failed to add line item {rowNumber}: Fingerprint already exists");
                        failedRows++;
                        continue;
                    }
                    // The line item is in the DB and this is an existing batch
                    else
                    {
                        var existingLineItem = existingMap[lineItemFingerprint];
                        // Check if the line item is null even though the fingerprint is in the hashset
                        if (existingLineItem == null)
                        {
                            result.Errors.Add($"Line item {rowNumber} expected but not found in DB");
                            failedRows++;
                            continue;
                        }
                        // If this line item originally had a null clinician see if it exists at the time of running this 
                        if (existingLineItem.Clinician == null)
                        {
                            bool matchResult = MatchUnresolvedPaymentLineItem(existingLineItem, clinicians);
                            if(!matchResult)
                            {
                                result.Errors.Add($"Failed to resolve clinician for line item {rowNumber}: Could not find corresponding clinician for {mappedRow.ClinicianName}");
                                unresolvedRows++;
                            }
                        }
                        // Skip the row if it is already processed with a valid clinician 
                        else
                        {
                            skippedRows++;
                        }
                        continue;
                    }
                }
                catch (NullReferenceException)
                {
                    result.Errors.Add($"Failed to add line item {rowNumber}: Could not find corresponding clinician for {mappedRow.ClinicianName}");
                    failedRows++;
                    continue;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to add line item {rowNumber}: {ex}");
                    failedRows++;
                    continue;
                }
            }

            // Update the failed rows in the batch and save all changes
            result.FailedRows = failedRows;
            result.TotalRows = rowNumber;
            result.SkippedRows = skippedRows;
            result.SuccessfulRows = successfulRows;
            result.UnresolvedRows = unresolvedRows;
            return result;
        }

        private Clinician? FindClinician(string clinicianName, List<Clinician> clinicians)
        {
            clinicianName = clinicianName.ToLower();
            string[] splitName = clinicianName.Split([' ', ',']);
            string firstName = splitName[0];
            string lastName = splitName[1];

            Clinician? existingClinician = clinicians.Find(c => 
                String.Equals(c.FirstName, firstName, StringComparison.OrdinalIgnoreCase) && 
                String.Equals(c.LastName, lastName, StringComparison.OrdinalIgnoreCase));
            return existingClinician;
        }

        private EHRUser FindAppliedByUser(string username, List<EHRUser> users) {
            EHRUser? existingUser = users.Find(u => u.EHRUsername == username);
            if (existingUser != null)
            {
                return existingUser!;
            }
            else 
            {
                EHRUser newUser = new();
                newUser.EHRUsername = username;
                _ehrUserRepo.AddNewUser(newUser);
                users.Add(newUser);
                return newUser;
            }
        }

        public async Task<ClinicianMatchResult> MatchUnresolvedPaymentLineItems()
        {
            var payments = await _repo.GetPaymentsWithUnresolvedClinician();
            var clinicians = await _clinicianRepo.GetAllClinicians();
            var result = new ClinicianMatchResult(payments.Count);

            foreach (var payment in payments)
            {
                if(MatchUnresolvedPaymentLineItem(payment, clinicians))
                {
                    result.ResolvedRows++;
                }
                else
                {
                    result.FailedRows++;
                    result.Errors.Add($"Failed to resolve payment ID: {payment.Id} for Clincian: {payment.RawClinicianName}");
                }
            }
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        private bool MatchUnresolvedPaymentLineItem(PaymentLineItem lineItem, List<Clinician> clinicians)
        {
            // Look for the clinician now and see if it exists
            Clinician? newClinician = FindClinician(lineItem.RawClinicianName, clinicians);
            // If the clinician still does not exist, its an unresolved row
            if (newClinician == null)
            {
                return false;
            }
            // If the clinician does now exist, update the clinician
            else
            { 
                lineItem.UpdateClinician(newClinician);
                return true;
            }
        }
    }
}
