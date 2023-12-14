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

            // Apply the logic you provided
            ApplyProfitLogic(calculatedContractProfit);

            var calculatedMarketingProfit = await _contractService.CalculatedContractMarketingProfitForTodayAsync(contract.Id);

            // Apply the logic you provided
            ApplyProfitLogic(calculatedMarketingProfit);

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
private static void ApplyProfitLogic((long, long) calculatedProfit)
{
    var userId = calculatedProfit.Item1;
    var profitAmount = calculatedProfit.Item2;

    // Your logic for adjusting profit based on the mining company's profit

    // Example: Reducing profit by 5%
    var adjustedProfit = profitAmount * 0.95;

    // Save the adjusted profit back to the database or perform other necessary operations
    // ...

    description = $"Adjusted profit due to mining company's profit. Original: {profitAmount}, Adjusted: {adjustedProfit}";
    _wallets.Add(IncWallet(userId: userId, price: (long)adjustedProfit, contractId: contract.Id, description: description, transactionType: WalletTransactionType.Daily_Contract_Profit, startDateTimeToWithdraw: startDateTimeToWithdraw, createDateTime: depositProfitPersianDateTime);
}

// Other methods remain unchanged
