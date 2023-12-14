using System;
using System.Collections.Generic;
using System.Net.Http;
using HtmlAgilityPack;

namespace CloudMiningCrawler
{
    public class CloudMiningData
    {
        public string Site { get; set; }
        public string InterestRate { get; set; }
        public string ContractPeriod { get; set; }
        public string InitialPaymentAmount { get; set; }
        public string MiningLicenses { get; set; }
    }

    public class Crawler
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://www.trustpilot.com/"; // Change it to the cloud mining validator site URL

        public Crawler()
        {
            _httpClient = new HttpClient();
        }

        public List<CloudMiningData> ScrapeData()
        {
            var cloudMiningDataList = new List<CloudMiningData>();

            var trustPilotUrl = BaseUrl + "/www.trustpilot.com"; // Change it to the appropriate TrustPilot URL

            var trustPilotHtml = _httpClient.GetStringAsync(trustPilotUrl).Result;
            var trustPilotDocument = new HtmlDocument();
            trustPilotDocument.LoadHtml(trustPilotHtml);

            var reviewNodes = trustPilotDocument.DocumentNode.SelectNodes("//div[@class='review']");

            foreach (var reviewNode in reviewNodes)
            {
                var cloudMiningData = new CloudMiningData();

                // Extract the required data from the review node
                cloudMiningData.Site = reviewNode.SelectSingleNode(".//span[@class='site']").InnerText;
                cloudMiningData.InterestRate = reviewNode.SelectSingleNode(".//span[@class='interest-rate']").InnerText;
                cloudMiningData.ContractPeriod = reviewNode.SelectSingleNode(".//span[@class='contract-period']").InnerText;
                cloudMiningData.InitialPaymentAmount = reviewNode.SelectSingleNode(".//span[@class='initial-payment-amount']").InnerText;
                cloudMiningData.MiningLicenses = reviewNode.SelectSingleNode(".//span[@class='mining-licenses']").InnerText;

                cloudMiningDataList.Add(cloudMiningData);
            }

            return cloudMiningDataList;
        }
    }

    public class DatabaseManager
    {
        public void SaveData(List<CloudMiningData> data)
        {
            // Code to save the data in the database
            Console.WriteLine("Saving data to the database...");

            foreach (var cloudMiningData in data)
            {
                // Save each item in the database
                Console.WriteLine($"Site: {cloudMiningData.Site}");
                Console.WriteLine($"Interest Rate: {cloudMiningData.InterestRate}");
                Console.WriteLine($"Contract Period: {cloudMiningData.ContractPeriod}");
                Console.WriteLine($"Initial Payment Amount: {cloudMiningData.InitialPaymentAmount}");
                Console.WriteLine($"Mining Licenses: {cloudMiningData.MiningLicenses}");
                Console.WriteLine();
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var crawler = new Crawler();
            var data = crawler.ScrapeData();

            var dbManager = new DatabaseManager();
            dbManager.SaveData(data);

            Console.WriteLine("Data saved successfully!");

            Console.ReadLine();
        }
    }
}
