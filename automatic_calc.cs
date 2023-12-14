using System;
using System.Linq;
using System.Threading.Tasks;
using MiningPlatform.Models;

namespace MiningPlatform
{
    public class User
    {
        public decimal Profit { get; set; }
    }

    public class MiningCompany
    {
        public double Profit { get; set; }
    }

    public class Platform
    {
        private readonly User _user;
        private readonly MiningCompany _miningCompany;

        public Platform(User user, MiningCompany miningCompany)
        {
            _user = user;
            _miningCompany = miningCompany;
        }

        public async Task CalculateAndDistributeProfits()
        {
            var miningCompanyProfit = _miningCompany.Profit;

            while (true)
            {
                // Check if it's time to start deposit
                if (!(PersianDateTime.Now.Hour == 0 && PersianDateTime.Now.Minute == 0))
                {
                    await Task.Delay(50000);
                    continue;
                }

                var depositProfitPersianDateTime = PersianDateTime.Now;

                await CalculateAndDeposit(depositProfitPersianDateTime, miningCompanyProfit);

                await Task.Delay(60000);
            }
        }

        private async Task CalculateAndDeposit(PersianDateTime depositProfitPersianDateTime, double miningCompanyProfit)
        {
            // Retrieve active contracts and necessary data
            var activeContracts = GetActiveContracts();
            var miningCompanyProfitFraction = (decimal) miningCompanyProfit / 100;
            var i = 1;

            foreach (var contract in activeContracts)
            {
                try
                {
                    // Check if profit is already deposited for today
                    var isProfitDepositedToday = IsProfitDepositedToday(contract.Id, depositProfitPersianDateTime);
                    if (isProfitDepositedToday)
                        continue;

                    // Get the start date for profit withdrawal
                    var startDateTimeToWithdraw = GetStartDateToWithdraw(depositProfitPersianDateTime,
                        contract.PersianCreateDateTime);

                    // Calculate the profit for the current contract
                    var calculatedContractProfit =
                        await _contractService.CalculatedContractProfitForTodayAsync(contract.Id);
                    if (calculatedContractProfit.Item1 != 25 && calculatedContractProfit.Item1 != 10036)
                    {
                        ApplyProfitLogic(calculatedContractProfit, miningCompanyProfitFraction, startDateTimeToWithdraw,
                            depositProfitPersianDateTime, WalletTransactionType.Daily_Contract_Profit);
                    }

                    // Calculate the marketing profit for the current contract
                    var calculatedMarketingProfit =
                        await _contractService.CalculatedContractMarketingProfitForTodayAsync(contract.Id);
                    if (calculatedMarketingProfit.Item1 != 25 && calculatedMarketingProfit.Item1 != 0)
                    {
                        ApplyProfitLogic(calculatedMarketingProfit, miningCompanyProfitFraction, startDateTimeToWithdraw,
                            depositProfitPersianDateTime, WalletTransactionType.Daily_Marketing_Profit);
                    }

                    await _uow.SaveChangesAsync();

                    Console.Write($"\r {i} ==> contract {contract.Id} is done! ");
                    i++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" {i} ==> contract {contract.Id} Exception !!! \n {ex.Message}");
                }
            }
        }

        // Helper method to apply profit logic
        private void ApplyProfitLogic((long, decimal) calculatedProfit, decimal miningCompanyProfitFraction,
            DateTime startDateTimeToWithdraw, PersianDateTime depositProfitPersianDateTime,
            WalletTransactionType transactionType)
        {
            var userId = calculatedProfit.Item1;
            var profitAmount = calculatedProfit.Item2;
            var adjustedProfit = 0m;

            if (miningCompanyProfitFraction < 0.13m)
            {
                // Reduce user profit by 5% at most
                adjustedProfit = Math.Max(profitAmount * 0.95m, profitAmount * (1 - miningCompanyProfitFraction + 0.01m));

                // Save the adjusted profit back to the database or perform other necessary operations
                SaveAdjustedProfit(userId, adjustedProfit, "Adjusted profit due to mining company's profit",
                    transactionType, startDateTimeToWithdraw, depositProfitPersianDateTime);
            }
            else
            {
                // Increase legal reserve, platform cost, and currency exchange risk for profits greater than 13%

                // Calculate legal reserve as 3% of profit amount
                var legalReserve = profitAmount * 0.03m;

                // Calculate platform cost as 2% of profit amount
                var platformCost = profitAmount * 0.02m;

                // Calculate currency exchange risk as 2% of profit amount
                var currencyRisk = profitAmount * 0.02m;

                // Adjust user profit to 5%
                adjustedProfit = profitAmount * 0.05m;

                // Save the adjusted profit back to the database or perform other necessary operations
                SaveAdjustedProfit(userId, adjustedProfit, "User profit", transactionType, startDateTimeToWithdraw,
                    depositProfitPersianDateTime);

                // Save legal reserve, platform cost, and currency exchange risk to the database or perform other necessary operations
                SavePlatformDetails(userId, legalReserve, platformCost, currencyRisk, depositProfitPersianDateTime);
            }
        }

        // Helper method to check whether profit is already deposited for the specified contract and date
        private bool IsProfitDepositedToday(long contractId, PersianDateTime depositProfitPersianDateTime)
        {
            var depositDateTime = depositProfitPersianDateTime.ToDateTime();
            return _uow.Set<Wallet>().Any(w =>
                w.ContractId == contractId && w.ShamsiYear == depositProfitPersianDateTime.Year &&
                w.ShamsiMonth == depositProfitPersianDateTime.Month && w.ShamsiDay == depositProfitPersianDateTime.Day &&
                w.OperationType == WalletOperationType.Deposiot &&
                (w.TransactionType == WalletTransactionType.Daily_Contract_Profit ||
                 w.TransactionType == WalletTransactionType.Daily_Marketing_Profit) &&
                w.CreateDateTime.Year == depositDateTime.Year && w.CreateDateTime.Month == depositDateTime.Month &&
                w.CreateDateTime.Day == depositDateTime.Day);
        }

        // Helper method to save adjusted profit back to the database
        private void SaveAdjustedProfit(long userId, decimal adjustedProfit, string description,
            WalletTransactionType transactionType, DateTime startDateTimeToWithdraw,
            PersianDateTime depositProfitPersianDateTime)
        {
            _uow.Set<Wallet>().Add(
                new Wallet
                {
                    CreateDateTime = depositProfitPersianDateTime.ToDateTime(),
                    ShamsiYear = depositProfitPersianDateTime.Year,
                    ShamsiMonth = depositProfitPersianDateTime.Month,
                    ShamsiDay = depositProfitPersianDateTime.Day,
                    UserId = userId,
                    Price = adjustedProfit,
                    OperationType = WalletOperationType.Deposiot,
                    Description = description,
                    TransactionType = transactionType,
                    StartDateTimeToWithdraw = startDateTimeToWithdraw,
                    CreateDateTime = DateTime.UtcNow
                });
        }

        // Helper method to save legal reserve, platform cost, and currency exchange risk back to the database
        private void SavePlatformDetails(long userId, decimal legalReserve, decimal platformCost, decimal currencyRisk,
            PersianDateTime depositProfitPersianDateTime)
        {
            _uow.Set<PlatformCost>().Add(
                new PlatformCost
                {
                    CreateDateTime = depositProfitPersianDateTime.ToDateTime(),
                    ShamsiYear = depositProfitPersianDateTime.Year,
                    ShamsiMonth = depositProfitPersianDateTime.Month,
                    ShamsiDay = depositProfitPersianDateTime.Day,
                    UserId = userId,
                    LegalReserve = legalReserve,
                    PlatformCost = platformCost,
                    CurrencyExchangeRisk = currencyRisk,
                    CreateDateTime = DateTime.UtcNow
                });

            // Save legal reserve, platform cost, and currency exchange risk to the database or perform other necessary operations
            // ...
        }

        private IQueryable<ContractOutputDto> GetActiveContracts()
        {
            return _uow.Set<Core.Domain.Contract>()
                .Where(c => c.State == ContractState.Active && c.ExpireDateTime > DateTime.Now)
                .Select(c => new ContractOutputDto(c.Id, c.CreateDateTime));
        }

        private DateTime GetStartDateToWithdraw(PersianDateTime calculateDateTime, PersianDateTime contractCreateDateTime)
        {
            var now = calculateDateTime.ToDateTime();
            var startDateTimeToWithdraw = contractCreateDateTime.ToDateTime();

            if (now.Date == startDateTimeToWithdraw.Date || now < startDateTimeToWithdraw)
            {
                startDateTimeToWithdraw = adjustDate(startDateTimeToWithdraw,
                    new PersianDateTime(calculateDateTime.Year, calculateDateTime.Month, contractCreateDateTime.Day));
            }

            if (now.Date == startDateTimeToWithdraw.Date || now < startDateTimeToWithdraw)
            {
                startDateTimeToWithdraw = adjustDate(startDateTimeToWithdraw,
                    new PersianDateTime(calculateDateTime.Year, calculateDateTime.Month, contractCreateDateTime.Day - 1));
            }

            if (now.Date == startDateTimeToWithdraw.Date || now < startDateTimeToWithdraw)
            {
                startDateTimeToWithdraw = adjustDate(startDateTimeToWithdraw,
                    new PersianDateTime(calculateDateTime.Year, calculateDateTime.Month - 1, contractCreateDateTime.Day));
            }

            if (now.Date == startDateTimeToWithdraw.Date || now < startDateTimeToWithdraw)
            {
                startDateTimeToWithdraw = adjustDate(startDateTimeToWithdraw,
                    new PersianDateTime(calculateDateTime.Year, calculateDateTime.Month - 1, contractCreateDateTime.Day - 1));
            }

            return startDateTimeToWithdraw;
        }

        private DateTime adjustDate(DateTime dateTime, PersianDateTime persianDateTime)
        {
            var newDateTime = persianDateTime.ToDateTime();
            while (newDateTime.Date <= dateTime.Date)
            {
                newDateTime = newDateTime.AddMonths(1);
            }

            return newDateTime.AddDays(-1);
        }
    }
