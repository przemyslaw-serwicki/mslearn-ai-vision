using System;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using Azure;
using System.IO;

// Import namespaces
using Azure.AI.Vision.Common;
using Azure.AI.Vision.ImageAnalysis;


namespace image_analysis
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

                // Get image
                string imageFile = "images/street.jpg";
                imageFile = "images/person.jpg";
                imageFile = "images/building.jpg";
                if (args.Length > 0)
                {
                    imageFile = args[0];
                }

                // Authenticate Azure AI Vision client
                var cvClient = new VisionServiceOptions(
                    aiSvcEndpoint,
                    new AzureKeyCredential(aiSvcKey));


                // Analyze image
                AnalyzeImage(imageFile, cvClient);

                // Remove the background or generate a foreground matte from the image
                BackgroundForeground(imageFile, cvClient);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void AnalyzeImage(string imageFile, VisionServiceOptions serviceOptions)
        {
            Console.WriteLine($"\nAnalyzing {imageFile} \n");

            var analysisOptions = new ImageAnalysisOptions()
            {
                // Specify features to be retrieved
                Features =
                    ImageAnalysisFeature.Caption
                    | ImageAnalysisFeature.DenseCaptions
                    | ImageAnalysisFeature.Objects
                    | ImageAnalysisFeature.People
                    | ImageAnalysisFeature.Text
                    | ImageAnalysisFeature.Tags
            };

            // Get image analysis
            using var imageSource = VisionSource.FromFile(imageFile);

            using var analyzer = new ImageAnalyzer(serviceOptions, imageSource, analysisOptions);

            var result = analyzer.Analyze();

            if (result.Reason == ImageAnalysisResultReason.Analyzed)
            {
                // get image captions
                if (result.Caption != null)
                {
                    Console.WriteLine(" Caption:");
                    Console.WriteLine($"   \"{result.Caption.Content}\", Confidence {result.Caption.Confidence:0.0000}");
                }

                //get image dense captions
                if (result.DenseCaptions != null)
                {
                    Console.WriteLine(" Dense Captions:");
                    foreach (var caption in result.DenseCaptions)
                    {
                        Console.WriteLine($"   \"{caption.Content}\", Confidence {caption.Confidence:0.0000}");
                    }
                    Console.WriteLine($"\n");
                }

                // Get image tags
                if (result.Tags != null)
                {
                    Console.WriteLine($" Tags:");
                    foreach (var tag in result.Tags)
                    {
                        Console.WriteLine($"   \"{tag.Name}\", Confidence {tag.Confidence:0.0000}");
                    }
                    Console.WriteLine($"\n");
                }


                // Get objects in the image
                if (result.Objects != null)
                {
                    Console.WriteLine(" Objects:");

                    // Prepare image for drawing
                    System.Drawing.Image image = System.Drawing.Image.FromFile(imageFile);
                    Graphics graphics = Graphics.FromImage(image);
                    Pen pen = new Pen(Color.Cyan, 3);
                    Font font = new Font("Arial", 16);
                    SolidBrush brush = new SolidBrush(Color.WhiteSmoke);

                    foreach (var detectedObject in result.Objects)
                    {
                        Console.WriteLine($"   \"{detectedObject.Name}\", Confidence {detectedObject.Confidence:0.0000}");

                        // Draw object bounding box
                        var r = detectedObject.BoundingBox;
                        Rectangle rect = new Rectangle(r.X, r.Y, r.Width, r.Height);
                        graphics.DrawRectangle(pen, rect);
                        graphics.DrawString(detectedObject.Name, font, brush, r.X, r.Y);
                    }

                    // Save annotated image
                    String output_file = "objects.jpg";
                    image.Save(output_file);
                    Console.WriteLine("  Results saved in " + output_file + "\n");
                }


                // Get people in the image
                if (result.People != null)
                {
                    Console.WriteLine($" People:");

                    // Prepare image for drawing
                    System.Drawing.Image image = System.Drawing.Image.FromFile(imageFile);
                    Graphics graphics = Graphics.FromImage(image);
                    Pen pen = new Pen(Color.Cyan, 3);
                    Font font = new Font("Arial", 16);
                    SolidBrush brush = new SolidBrush(Color.WhiteSmoke);

                    foreach (var person in result.People)
                    {
                        // Draw object bounding box
                        var r = person.BoundingBox;
                        Rectangle rect = new Rectangle(r.X, r.Y, r.Width, r.Height);
                        graphics.DrawRectangle(pen, rect);

                        // Return the confidence of the person detected
                        Console.WriteLine($"   Bounding box {person.BoundingBox}, Confidence {person.Confidence:0.0000}");
                    }

                    // Save annotated image
                    String output_file = "persons.jpg";
                    image.Save(output_file);
                    Console.WriteLine("  Results saved in " + output_file + "\n");
                }
            }
            else
            {
                var errorDetails = ImageAnalysisErrorDetails.FromResult(result);
                Console.WriteLine(" Analysis failed.");
                Console.WriteLine($"   Error reason : {errorDetails.Reason}");
                Console.WriteLine($"   Error code : {errorDetails.ErrorCode}");
                Console.WriteLine($"   Error message: {errorDetails.Message}\n");
            }

        }
        static void BackgroundForeground(string imageFile, VisionServiceOptions serviceOptions)
        {
            // Remove the background from the image or generate a foreground matte
            Console.WriteLine($"\nRemove the background from the image or generate a foreground matte");

            using var imageSource = VisionSource.FromFile(imageFile);

            var analysisOptions = new ImageAnalysisOptions()
            {
                // Set the image analysis segmentation mode to background or foreground
                SegmentationMode = ImageSegmentationMode.BackgroundRemoval
                //SegmentationMode = ImageSegmentationMode.ForegroundMatting
            };

            using var analyzer = new ImageAnalyzer(serviceOptions, imageSource, analysisOptions);

            var result = analyzer.Analyze();

            // Remove the background or generate a foreground matte
            if (result.Reason == ImageAnalysisResultReason.Analyzed)
            {
                using var segmentationResult = result.SegmentationResult;

                var imageBuffer = segmentationResult.ImageBuffer;
                Console.WriteLine($"\n Segmentation result:");
                Console.WriteLine($"   Output image buffer size (bytes) = {imageBuffer.Length}");
                Console.WriteLine($"   Output image height = {segmentationResult.ImageHeight}");
                Console.WriteLine($"   Output image width = {segmentationResult.ImageWidth}");

                string outputImageFile = "newimage.jpg";
                using (var fs = new FileStream(outputImageFile, FileMode.Create))
                {
                    fs.Write(imageBuffer.Span);
                }
                Console.WriteLine($"   File {outputImageFile} written to disk\n");
            }
            else
            {
                var errorDetails = ImageAnalysisErrorDetails.FromResult(result);
                Console.WriteLine(" Analysis failed.");
                Console.WriteLine($"   Error reason : {errorDetails.Reason}");
                Console.WriteLine($"   Error code : {errorDetails.ErrorCode}");
                Console.WriteLine($"   Error message: {errorDetails.Message}");
                Console.WriteLine(" Did you set the computer vision endpoint and key?\n");
            }
        }
    }
}
