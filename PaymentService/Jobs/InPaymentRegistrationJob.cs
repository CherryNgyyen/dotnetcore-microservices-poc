using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PaymentService.Domain;

namespace PaymentService.Jobs;

public class InPaymentRegistrationJob
{
    private readonly IDataStore dataStore;
    private readonly BackgroundJobsConfig jobConfig;
    private readonly ILogger<InPaymentRegistrationJob> _logger;

    public InPaymentRegistrationJob(IDataStore dataStore, BackgroundJobsConfig jobConfig, ILogger<InPaymentRegistrationJob> logger)
    {
        this.dataStore = dataStore;
        this.jobConfig = jobConfig;
        this._logger = logger;
    }

    public async Task Run()
    {
        _logger.LogInformation($"InPayment import started. Looking for file in {jobConfig.InPaymentFileFolder}");

        var importService = new InPaymentRegistrationService(dataStore);
        await importService.RegisterInPayments(jobConfig.InPaymentFileFolder, DateTimeOffset.Now);

        _logger.LogInformation("InPayment import finished.");
    }
}