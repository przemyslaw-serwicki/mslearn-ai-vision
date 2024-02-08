using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure;

// Import namespaces
using Azure.AI.Vision.Common;
using Azure.AI.Vision.ImageAnalysis;


namespace read_text
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string aiSvcEndpoint = configuration["AIServicesEndpoint"];
                string aiSvcKey = configuration["AIServicesKey"];

                // Authenticate Azure AI Vision client
                var cvClient = new VisionServiceOptions(
                    aiSvcEndpoint,
                    new AzureKeyCredential(aiSvcKey));


                // Menu for text reading functions
                Console.WriteLine("\n1: Use Read API for image (Lincoln.jpg)\n2: Read handwriting (Note.jpg)\nAny other key to quit\n");
                Console.WriteLine("Enter a number:");
                string command = Console.ReadLine();
                string imageFile;

                switch (command)
                {
                    case "1":
                        imageFile = "images/Lincoln.jpg";
                        GetTextRead(imageFile, cvClient);
                        break;
                    case "2":
                        imageFile = "images/Note.jpg";
                        GetTextRead(imageFile, cvClient);
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void GetTextRead(string imageFile, VisionServiceOptions serviceOptions)
        {
            // Use Analyze image function to read text in image
            Console.WriteLine($"\nReading text in {imageFile}\n");

            using (var imageData = File.OpenRead(imageFile))
            {
                var analysisOptions = new ImageAnalysisOptions()
                {
                    // Specify features to be retrieved
                    Features = ImageAnalysisFeature.Text
                };

                using var imageSource = VisionSource.FromFile(imageFile);

                using var analyzer = new ImageAnalyzer(serviceOptions, imageSource, analysisOptions);

                var result = analyzer.Analyze();

                if (result.Reason == ImageAnalysisResultReason.Analyzed)
                {
                    // get image captions
                    if (result.Text != null)
                    {
                        Console.WriteLine($"Text:");

                        // Prepare image for drawing
                        System.Drawing.Image image = System.Drawing.Image.FromFile(imageFile);
                        Graphics graphics = Graphics.FromImage(image);
                        Pen pen = new Pen(Color.Cyan, 3);

                        foreach (var line in result.Text.Lines)
                        {
                            // Return the text detected in the image
                            // Return the text detected in the image
                            Console.WriteLine(line.Content);

                            var drawLinePolygon = true;

                            // Return each line detected in the image and the position bounding box around each line
                            string pointsToString = "{" + string.Join(',', line.BoundingPolygon.Select(pointsToString => pointsToString.ToString())) + "}";
                            Console.WriteLine($"   Line: '{line.Content}', Bounding Polygon {pointsToString}");


                            // Return each word detected in the image and the position bounding box around each word with the confidence level of each word
                            foreach (var word in line.Words)
                            {
                                pointsToString = "{" + string.Join(',', word.BoundingPolygon.Select(pointsToString => pointsToString.ToString())) + "}";
                                Console.WriteLine($"     Word: '{word.Content}', Bounding polygon {pointsToString}, Confidence {word.Confidence:0.0000}");

                                // Draw word bounding polygon
                                drawLinePolygon = false;
                                var r = word.BoundingPolygon;

                                Point[] polygonPoints = {
                                    new Point(r[0].X, r[0].Y),
                                    new Point(r[1].X, r[1].Y),
                                    new Point(r[2].X, r[2].Y),
                                    new Point(r[3].X, r[3].Y)
                                };

                                graphics.DrawPolygon(pen, polygonPoints);
                            }


                            // Draw line bounding polygon
                            if (drawLinePolygon)
                            {
                                var r = line.BoundingPolygon;

                                Point[] polygonPoints = {
                                    new Point(r[0].X, r[0].Y),
                                    new Point(r[1].X, r[1].Y),
                                    new Point(r[2].X, r[2].Y),
                                    new Point(r[3].X, r[3].Y)
                                };

                                graphics.DrawPolygon(pen, polygonPoints);
                            }
                        }

                        // Save image
                        String output_file = "text3.jpg";
                        image.Save(output_file);
                        Console.WriteLine("\nResults saved in " + output_file + "\n");
                    }
                }

            }

        }
    }
}
