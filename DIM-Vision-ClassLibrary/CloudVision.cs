﻿using Google.Cloud.Vision.V1;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace DIM_Vision_ClassLibrary
{
    public static class CloudVision
    {
        private const TextRecognitionMode textRecognitionMode =
            TextRecognitionMode.Handwritten;

        private const string remoteImageUrl =
            "https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/" +
            "Cursive_Writing_on_Notebook_paper.jpg/" +
            "800px-Cursive_Writing_on_Notebook_paper.jpg";

        private const int numberOfCharsInOperationId = 36;

        public static void VisionUseGoogle(System.Drawing.Image img)
        {

            using (MemoryStream memStream = new MemoryStream())
            {
                img.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                memStream.Seek(0, SeekOrigin.Begin);

                ImageAnnotatorClient client = ImageAnnotatorClient.Create();
                Image image = Image.FromStream(memStream);
                var response = client.DetectLabels(image);
            }

        }

        public static void VisionUserCognitive(System.Drawing.Image img)
        {
            string subscriptionKey = ConfigurationManager.AppSettings.Get("AzureVisionKey");
            ComputerVisionClient computerVision = new ComputerVisionClient(
                    new ApiKeyServiceClientCredentials(subscriptionKey),
                    new System.Net.Http.DelegatingHandler[] { });

            // You must use the same region as you used to get your subscription
            // keys. For example, if you got your subscription keys from westus,
            // replace "westcentralus" with "westus".
            //
            // Free trial subscription keys are generated in the "westus"
            // region. If you use a free trial subscription key, you shouldn't
            // need to change the region.

            // Specify the Azure region
            computerVision.Endpoint = "https://eastus.api.cognitive.microsoft.com/";

            Console.WriteLine("Images being analyzed ...");
            var t1 = ExtractRemoteTextAsync(computerVision, remoteImageUrl);
            var t2 = ExtractLocalTextAsync(computerVision, img);

            Task.WhenAll(t1, t2).Wait(5000);
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        // Recognize text from a remote image
        private static async Task ExtractRemoteTextAsync(
            ComputerVisionClient computerVision, string imageUrl)
        {
            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                Console.WriteLine(
                    "\nInvalid remoteImageUrl:\n{0} \n", imageUrl);
                return;
            }

            // Start the async process to recognize the text
            RecognizeTextHeaders textHeaders =
                await computerVision.RecognizeTextAsync(
                    imageUrl, textRecognitionMode);

            await GetTextAsync(computerVision, textHeaders.OperationLocation);
        }

        // Recognize text from a local image
        private static async Task ExtractLocalTextAsync(
            ComputerVisionClient computerVision, System.Drawing.Image img)
        {

            using (Stream imageStream = new MemoryStream())
            {
                img.Save(imageStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                imageStream.Seek(0, SeekOrigin.Begin);

                // Start the async process to recognize the text
                RecognizeTextInStreamHeaders textHeaders =
                    await computerVision.RecognizeTextInStreamAsync(
                        imageStream, textRecognitionMode);

                await GetTextAsync(computerVision, textHeaders.OperationLocation);
            }
        }

        // Retrieve the recognized text
        private static async Task GetTextAsync(
            ComputerVisionClient computerVision, string operationLocation)
        {
            // Retrieve the URI where the recognized text will be
            // stored from the Operation-Location header
            string operationId = operationLocation.Substring(
                operationLocation.Length - numberOfCharsInOperationId);

            Console.WriteLine("\nCalling GetHandwritingRecognitionOperationResultAsync()");
            TextOperationResult result =
                await computerVision.GetTextOperationResultAsync(operationId);

            // Wait for the operation to complete
            int i = 0;
            int maxRetries = 10;
            while ((result.Status == TextOperationStatusCodes.Running ||
                    result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
            {
                Console.WriteLine(
                    "Server status: {0}, waiting {1} seconds...", result.Status, i);
                await Task.Delay(1000);

                result = await computerVision.GetTextOperationResultAsync(operationId);
            }

            // Display the results
            Console.WriteLine();
            var lines = result.RecognitionResult.Lines;
            foreach (Line line in lines)
            {
                Console.WriteLine(line.Text);
            }
            Console.WriteLine();
        }

        //public static PerformGoogleVision()
        //{
        //    var client = ImageAnnotatorClient.Create();
        //    var image = System.Drawing.Image.FromStream("gs://cloud-vision-codelab/otter_crossing.jpg");
        //    var response = client.DetectText(image);
        //    foreach (var annotation in response)
        //    {
        //        if (annotation.Description != null)
        //        {
        //            Console.WriteLine(annotation.Description);
        //        }
        //    }
        //}

    }
}
