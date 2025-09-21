using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ReactiveNewsServer.MLModels
{
    public class ModelHelper
    {
        public static PredictionEngine<SentimentData, SentimentPrediction> CreatePredictionEngine()
        {
            var mlContext = new MLContext();
            
            // podaci sa primerima
            var trainingData = new List<SentimentData>
            {
                // Pozitivni primeri
                new SentimentData { Text = "This is great news", Sentiment = true },
                new SentimentData { Text = "I love this product", Sentiment = true },
                new SentimentData { Text = "Amazing achievement", Sentiment = true },
                new SentimentData { Text = "Revolutionary technology breakthrough", Sentiment = true },
                new SentimentData { Text = "Innovative solution changed the industry", Sentiment = true },
                new SentimentData { Text = "Record profits and growth this quarter", Sentiment = true },
                new SentimentData { Text = "Breakthrough discovery in AI research", Sentiment = true },
                
                // Negativni primeri
                new SentimentData { Text = "This is terrible", Sentiment = false },
                new SentimentData { Text = "I hate this situation", Sentiment = false },
                new SentimentData { Text = "Awful experience", Sentiment = false },
                new SentimentData { Text = "Major security breach exposed user data", Sentiment = false },
                new SentimentData { Text = "Company reports significant losses", Sentiment = false },
                new SentimentData { Text = "Critical vulnerability found in software", Sentiment = false },
                new SentimentData { Text = "Massive layoffs announced today", Sentiment = false },
            };

            var dataView = mlContext.Data.LoadFromEnumerable(trainingData);
            
            var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text))
                .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());

            var model = pipeline.Fit(dataView);
            return mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
        }
    }
}