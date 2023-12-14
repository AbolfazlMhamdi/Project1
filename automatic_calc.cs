private static async Task AutomaticDepositContractsProfit()
{
    while (true)
    {
        #region Check Time To Start Deposite

        if (!(PersianDateTime.Now.Hour == 0 && PersianDateTime.Now.Minute == 0))
        {
            await Task.Delay(50000);
            continue;
        }

        #endregion

        await CalculateAndDeposit(PersianDateTime.Now);

        await Task.Delay(60000);
    }
}

private static async Task CalculateAndDeposit(PersianDateTime depositProfitPersianDateTime)
{
    #region Variables

    var activeContracts = GetActiveContracts();
    var _wallets = _uow.Set<Wallet>();
    var _users = _uow.Set<User>();
    string description = "";

    var i = 1;

    #endregion

    #region Calculate and Deposit

    foreach (var contract in activeContracts)
    {
        try
        {
            var profitIsDepositedBefore = _wallets.Any(w => w.ContractId == contract.Id && w.ShamsiYear == depositProfitPersianDateTime.Year && w.ShamsiMonth == depositProfitPersianDateTime.Month && w.ShamsiDay == depositProfitPersianDateTime.Day);
            if (profitIsDepositedBefore)
                continue;

            var startDateTimeToWithdraw = GetStartDateToWithdraw(depositProfitPersianDateTime, contract.PersianCreateDateTime);

            var calculatedContractProfit = await _contractService.CalculatedContractProfitForTodayAsync(contract.Id);

            // Apply the updated logic for adjusting profit based on the mining company's profit
            ApplyProfitLogic(calculatedContractProfit, miningCompanyProfit);

            var calculatedMarketingProfit = await _contractService.CalculatedContractMarketingProfitForTodayAsync(contract.Id);

            // Apply the updated logic for adjusting profit based on the mining company's profit
            ApplyProfitLogic(calculatedMarketingProfit, miningCompanyProfit);

            await _uow.SaveChangesAsync();

            System.Console.Write($"\r {i} ==> contract {contract.Id} is done! ");
            i++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($" {i} ==> contract {contract.Id} Exception !!! \n {ex.Message}");
        }
    }

    #endregion
}

// Helper method to apply profit logic
private static void ApplyProfitLogic((long, long) calculatedProfit, double miningCompanyProfit)
{
    var userId = calculatedProfit.Item1;
    var profitAmount = calculatedProfit.Item2;
    var adjustedProfit = 0.0;

    if (miningCompanyProfit < 13)
    {
        // Reduce user profit by 5% at most
        adjustedProfit = Math.Max(profitAmount * 0.95, profitAmount * 0.97);

        // Save the adjusted profit back to the database or perform other necessary operations
        SaveAdjustedProfit(userId, adjustedProfit, "Adjusted profit due to mining company's profit", WalletTransactionType.Daily_Contract_Profit, startDateTimeToWithdraw, depositProfitPersianDateTime);
    }
    else
    {
        // Increase legal reserve, platform cost, and currency exchange risk for profits greater than 13%
        var legalReserve = profitAmount * 0.03;
        var platformCost = profitAmount * 0.02;
        var currencyRisk = profitAmount * 0.02;

        // Adjust user profit to 5%
        adjustedProfit = profitAmount * 0.05;

        // Save the adjusted profit back to the database or perform other necessary operations
        SaveAdjustedProfit(userId, adjustedProfit, "User profit", WalletTransactionType.Daily_Contract_Profit, startDateTimeToWithdraw, depositProfitPersianDateTime);

        // Save legal reserve, platform cost, and currency exchange risk to the database or perform other necessary operations
        SavePlatformDetails(userId, legalReserve, platformCost, currencyRisk);
    }
}

private static void SaveAdjustedProfit(long userId, double adjustedProfit, string description, WalletTransactionType transactionType, DateTime startDateTimeToWithdraw, PersianDateTime depositProfitPersianDateTime)
{
    var adjustedProfitAsLong = (long)adjustedProfit;
    _wallets.Add(IncWallet(userId, adjustedProfitAsLong, contract.Id, description, transactionType, startDateTimeToWithdraw, depositProfitPersianDateTime));
}

private static void SavePlatformDetails(long userId, double legalReserve, double platformCost, double currencyRisk)
{
    // Save legal reserve, platform cost, and currency exchange risk to the database or perform other necessary operations
    // ...
}
