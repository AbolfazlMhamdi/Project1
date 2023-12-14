using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using Microsoft.Data.SqlClient;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace SmartInvestorSystem
{
    public class CloudMiningData
    {
        [LoadColumn(0)]
        public string Site { get; set; }
        [LoadColumn(1)]
        public float InterestRate { get; set; }
        [LoadColumn(2)]
        public float ContractPeriod { get; set; }
        [LoadColumn(3)]
        public float InitialPaymentAmount { get; set; }
        [LoadColumn(4)]
        public float MiningLicenses { get; set; }
        [LoadColumn(5)]
        public string Category { get; set; }
    }

    public class SmartInvestor
    {
        private readonly string _connectionString;
        private const string BaseUrl = "https://www.trustpilot.com/"; // Change it to the cloud mining validator site URL

        public SmartInvestor(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void TrainModel()
        {
            var mlContext = new MLContext();

            // Load the data from the database
            var connectionString = _connectionString;
            var query = "SELECT Site, InterestRate, ContractPeriod, InitialPaymentAmount, MiningLicenses, Category FROM CloudMiningData";
            var data = mlContext.Data.LoadFromSqlServer(connectionString, query);

            // Split the data into training and testing datasets
            var dataSplit = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

            // Define the data process pipeline
            var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
                .Append(mlContext.Transforms.Concatenate("Features", "InterestRate", "ContractPeriod", "InitialPaymentAmount", "MiningLicenses"))
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("Category"));

            // Define the trainer and algorithm
            var trainer = mlContext.MulticlassClassification.Trainers.SdcaNonCalibrated();
            var trainingPipeline = dataProcessPipeline.Append(trainer)
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // Train the model
            var model = trainingPipeline.Fit(dataSplit.TrainSet);

            // Evaluate the model
            var predictions = model.Transform(dataSplit.TestSet);
            var metrics = mlContext.MulticlassClassification.Evaluate(predictions);

            // Print evaluation results
            Console.WriteLine($"Evaluation Metrics (Test Data) - Accuracy: {metrics.MacroAccuracy}");

            // Save the model
            mlContext.Model.Save(model, data.Schema, "trainedModel.zip");
        }

        public void CategorizeAndRecommend()
        {
            var mlContext = new MLContext();
            var modelPath = "trainedModel.zip";
            var model = mlContext.Model.Load(modelPath, out _);

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("SELECT * FROM CloudMiningData", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var cloudMiningData = new CloudMiningData();
                            cloudMiningData.Site = reader.GetString(0);
                            cloudMiningData.InterestRate = reader.GetFloat(1);
                            cloudMiningData.ContractPeriod = reader.GetFloat(2);
                            cloudMiningData.InitialPaymentAmount = reader.GetFloat(3);
                            cloudMiningData.MiningLicenses = reader.GetFloat(4);

                            var predictionEngine = mlContext.Model.CreatePredictionEngine<CloudMiningData, CloudMiningData>(model);
                            var categorizedData = predictionEngine.Predict(cloudMiningData);

                            connection.Execute($"UPDATE CloudMiningData SET Category = '{categorizedData.Category}' WHERE Site = '{cloudMiningData.Site}'");
                        }
                    }
                }
            }

            var topThreeCategories = GetTopThreeCategories();

            Console.WriteLine("Top 3 Cloud Mining Categories:");

            foreach (var category in topThreeCategories)
            {
                Console.WriteLine($"Category: {category}, Count: {topThreeCategories[category]}");
            }
        }

        private Dictionary<string, int> GetTopThreeCategories()
        {
            var categories = new Dictionary<string, int>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("SELECT Category, COUNT(*) AS Count FROM CloudMiningData GROUP BY Category ORDER BY Count DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var category = reader.GetString(0);
                            var count = reader.GetInt32(1);
                            categories.Add(category, count);
                        }
                    }
                }
            }

            var topThreeCategories = categories.OrderByDescending(c => c.Value).Take(3).ToDictionary(x => x.Key, x => x.Value);
            return topThreeCategories;
        }

        public void Run()
        {
            TrainModel();

            CategorizeAndRecommend();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var connectionString = "your_database_connection_string";

            var smartInvestor = new SmartInvestor(connectionString);
            smartInvestor.Run();

            Console.WriteLine("Smart Investing completed!");

            Console.ReadLine();
        }
    }
}