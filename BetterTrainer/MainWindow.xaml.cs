using BetterTrainer.AML;
using BetterTrainer.FaceRecognition;
using BetterTrainer.PBI;
using edu.stanford.nlp.parser.lexparser;
using edu.stanford.nlp.process;
using Google.Cloud.Speech.V1;
using Microsoft.CognitiveServices.SpeechRecognition;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BetterTrainer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        // Face Recognition with Face API from Azure Cognitive Services
        #region Face Recognition

        private FaceServiceClient faceClient = null;
        private readonly FrameGrabber<LiveCameraResult> grabber = null;
        private int numCameras = 0;
        private LiveCameraResult latestResultsToDisplay = null;

        private static readonly ImageEncodingParam[] s_jpegParams = {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
        };

        private void InitializeFaceRecognition()
        {
            // delegate function when new video frame is provided
            grabber.NewFrameProvided += (s, e) =>
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // add frame image to left image preview window
                    imgLeft.Source = e.Frame.Image.ToBitmapSource();
                }));
            };

            // delegate function when new result from Face API is available
            grabber.NewResultAvailable += (s, e) =>
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Is Timeout?
                    if (e.TimedOut)
                    {
                        WriteLine_FR("API call timed out.");
                    }
                    // Is Exception?
                    else if (e.Exception != null)
                    {
                        string apiName = "";
                        string message = e.Exception.Message;
                        var faceEx = e.Exception as FaceAPIException;
                        var emotionEx = e.Exception as Microsoft.ProjectOxford.Common.ClientException;
                        var visionEx = e.Exception as Microsoft.ProjectOxford.Vision.ClientException;
                        if (faceEx != null)
                        {
                            apiName = "Face";
                            message = faceEx.ErrorMessage;
                        }
                        else if (emotionEx != null)
                        {
                            apiName = "Emotion";
                            message = emotionEx.Error.Message;
                        }
                        else if (visionEx != null)
                        {
                            apiName = "Computer Vision";
                            message = visionEx.Error.Message;
                        }
                        WriteLine_FR(string.Format("{0} API call failed on frame {1}. Exception: {2}", apiName, e.Frame.Metadata.Index, message));
                    }
                    // Is all OK?
                    else
                    {
                        latestResultsToDisplay = e.Analysis;

                        // show result with detected faces and attributes to right image preview window
                        imgRight.Source = VisualizeResult(e.Frame);
                    }
                }));
            };

            // Define Analysis Function
            grabber.AnalysisFunction = FacesAnalysisFunction;

            // Check if any camera is available
            numCameras = grabber.GetNumCameras();
            if (numCameras == 0)
                WriteLine_FR("No cameras found!");

            for (int i = 1; i <= numCameras; i++)
                cbKamera.Items.Add(i);

            if (cbKamera.Items.Count > 0)
                cbKamera.SelectedIndex = 0;
        }

        private async Task<LiveCameraResult> FacesAnalysisFunction(VideoFrame frame)
        {
            // Encode image 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);

            // Define attribute which you want to get back from Face API -> gender, emotion
            var attrs = new List<FaceAttributeType>
            {
                FaceAttributeType.Gender,
                FaceAttributeType.Emotion
            };

            // Submit image to API
            var faces = await faceClient.DetectAsync(jpg, returnFaceAttributes: attrs);

            // return result
            // in additional you can call Face Recognition functionality too -> in this scenario you could set Celebrity Names etc.
            return new LiveCameraResult
            {
                Faces = faces,
                EmotionScores = faces.Select(face => face.FaceAttributes.Emotion).ToArray(),
                //CelebrityNames = ...,
                //Tags = ...
            };
        }

        private BitmapSource VisualizeResult(VideoFrame frame)
        {
            BitmapSource visImage = frame.Image.ToBitmapSource();

            // if we have any results from Face API
            var result = latestResultsToDisplay;
            if (result != null)
            {
                // Add it to Faces in time queue with timestamp
                facesInTime.Add(new FacesInTime()
                {
                    TimeStamp = DateTime.Now,
                    CameraResult = result
                });

                // See if we have analysis results from an older frame -> we need to match it to correct frame
                var clientFaces = (OpenCvSharp.Rect[])frame.UserData;
                if (clientFaces != null && result.Faces != null)
                    MatchAndReplaceFaceRectangles(result.Faces, clientFaces);

                // Draw results on top of the image
                visImage = Visualization.DrawFaces(visImage, result.Faces, result.EmotionScores, result.CelebrityNames);
                visImage = Visualization.DrawTags(visImage, result.Tags);
            }

            return visImage;
        }

        private void MatchAndReplaceFaceRectangles(Face[] faces, OpenCvSharp.Rect[] clientRects)
        {
            var sortedResultFaces = faces
                .OrderBy(f => f.FaceRectangle.Left + 0.5 * f.FaceRectangle.Width)
                .ToArray();

            var sortedClientRects = clientRects
                .OrderBy(r => r.Left + 0.5 * r.Width)
                .ToArray();

            for (int i = 0; i < Math.Min(faces.Length, clientRects.Length); i++)
            {
                OpenCvSharp.Rect r = sortedClientRects[i];
                sortedResultFaces[i].FaceRectangle = new FaceRectangle { Left = r.Left, Top = r.Top, Width = r.Width, Height = r.Height };
            }
        }

        // When we want to start Face Recognition functionality
        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (numCameras == 0)
            {
                WriteLine_FR("No cameras found; cannot start processing");
                return;
            }

            // We define how often we want to analyze -> every 3 seconds 
            grabber.TriggerAnalysisOnInterval(TimeSpan.Parse("00:00:03"));

            // Reset log 
            tbLogsFR.Text = "";

            // Start processing from selected camera
            await grabber.StartProcessingCameraAsync(Int32.Parse(cbKamera.SelectedValue.ToString()) - 1);

            // save time when we start recognizing
            lastTimeCheck = DateTime.Now;

            WriteLine_FR("[Started]");
        }

        // When we want to end Face Recognition
        private async void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            // Stop processing
            await grabber.StopProcessingAsync();

            WriteLine_FR("[Ended]");
        }

        // Function for writing datas to Log window
        private void WriteLine_FR(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                tbLogsFR.Text += formattedStr + "\n";
                tbLogsFR.ScrollToEnd();
            });
        }

        #endregion

        // Speech Recognition with Bing Speech API from Azure Cognitive Services
        #region Speech Recognition

        private MicrophoneRecognitionClient micClient;

        // subscription key
        private string SubscriptionKey = "{enter-bing-speech-api-subscription-key}";

        // recognition mode
        // Long dictation -> an utterance up to 10 minutes long
        // Short Phase -> an utterance up to 15 seconds long
        private SpeechRecognitionMode Mode = SpeechRecognitionMode.LongDictation;

        // default locale
        private string DefaultLocale = "en-US";

        // When we want to start speech recognition
        private void btnStartSR_Click(object sender, RoutedEventArgs e)
        {
            // Set that we want to have speech recognition from microphone
            if (micClient == null)
                CreateMicrophoneRecoClient();

            // start microphone recognition
            micClient.StartMicAndRecognition();

            WriteLine_SR("[Started]");
        }

        // when we want to end speech recognition
        private void btnEndSR_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // end microphone recognition
                micClient.EndMicAndRecognition();

                WriteLine_SR("[Ended]");
            }
            catch { }
        }

        private void CreateMicrophoneRecoClient()
        {
            // create microphone client with specific mode, locale and subscription key
            micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                Mode,
                DefaultLocale,
                SubscriptionKey);
            micClient.AuthenticationUri = "";

            // Event handler for speech recognition results
            micClient.OnResponseReceived += OnResponseReceivedHandler;

            // Event handler for errors
            micClient.OnConversationError += OnConversationErrorHandler;
        }

        // When we receive response from Speech API
        private void OnResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation)
            {
                Dispatcher.Invoke(
                    (Action)(() =>
                    {
                        micClient.EndMicAndRecognition();
                        WriteLine_SR("[End Of Dictation]");
                    }));
            }

            // if we have any results
            if (e.PhraseResponse.Results.Length > 0)
            {
                // get result with best confidence mark
                string word = e.PhraseResponse.Results.OrderByDescending(x => x.Confidence).First().LexicalForm;

                // add it to words in time queue with timestamp and language mark
                wordsInTime.Add(new WordsInTime { TimeStamp = DateTime.Now, Word = word, Language = "English" });

                WriteLine_SR("{0}", word);
            }
        }

        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            WriteLine_SR("Error: {0}", e.SpeechErrorText);
        }

        private void WriteLine_SR(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                tbLogsSR.Text += formattedStr + "\n";
                tbLogsSR.ScrollToEnd();
            });
        }

        #endregion

        // Slovenian Speech Recognition with Speech API from Google Cloud Platform
        #region Speech Recognition (Google)

        // path to credential file which we can get it from Google Cloud Platform portal
        private string credentialPath = @"{enter-path-to-google-speech-api-subscription-file}";
        private SpeechClient speech;
        private SpeechClient.StreamingRecognizeStream streamingCall;
        private Task printResponses;
        private object writeLock;
        private bool writeMore;
        private NAudio.Wave.WaveInEvent waveIn;

        // when we want to start Speech Recognition
        private void btnStartSRG_Click(object sender, RoutedEventArgs e)
        {
            StartSRG();

            WriteLine_SRG("[Started]");
        }

        private async void StartSRG()
        {
            // We define that we want to recognition from microphone
            await FromMicrophone();

            // And we start recognition
            waveIn.StartRecording();
        }

        // when we want to end Speech Recognition
        private void btnEndSRG_Click(object sender, RoutedEventArgs e)
        {
            EndSRG();

            WriteLine_SRG("[Ended]");
        }

        private async void EndSRG()
        {
            try
            {
                // stop recognition
                waveIn.StopRecording();
                lock (writeLock) writeMore = false;
                await streamingCall.WriteCompleteAsync();
                await printResponses;
            }
            catch { }
        }

        private async Task<object> FromMicrophone()
        {
            // Check if any microphone is connected
            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                WriteLine_SRG("No microphone!");
                return -1;
            }

            // create client for speech recognition
            speech = SpeechClient.Create();

            streamingCall = speech.StreamingRecognize();
            // Write the initial request with the config (sample rate, encoding and language code)
            await streamingCall.WriteAsync(
                new StreamingRecognizeRequest()
                {
                    StreamingConfig = new StreamingRecognitionConfig()
                    {
                        Config = new RecognitionConfig()
                        {
                            Encoding =
                            RecognitionConfig.Types.AudioEncoding.Linear16,
                            SampleRateHertz = 16000,
                            LanguageCode = "sl",
                        },
                        // set if we want interim results
                        InterimResults = false,
                    }
                });

            // Print responses as they arrive
            printResponses = Task.Run(async () =>
            {
                while (await streamingCall.ResponseStream.MoveNext(
                    default(CancellationToken)))
                {
                    foreach (var result in streamingCall.ResponseStream
                        .Current.Results)
                    {
                        // get result with best confidence mark
                        string word = result.Alternatives.OrderByDescending(x => x.Confidence).First().Transcript;

                        // add it to words in time queue with timestamp and language mark
                        wordsInTime.Add(new WordsInTime { TimeStamp = DateTime.Now, Word = word, Language = "Slovenian" });

                        WriteLine_SRG(word);
                    }
                }
            });

            // Read from the microphone and stream to API
            writeLock = new object();
            writeMore = true;
            waveIn = new NAudio.Wave.WaveInEvent();
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            waveIn.DataAvailable +=
                (object sender, NAudio.Wave.WaveInEventArgs args) =>
                {
                    try
                    {
                        lock (writeLock)
                        {
                            if (!writeMore) return;
                            streamingCall.WriteAsync(
                                new StreamingRecognizeRequest()
                                {
                                    AudioContent = Google.Protobuf.ByteString
                                        .CopyFrom(args.Buffer, 0, args.BytesRecorded)
                                }).Wait();
                        }
                    }
                    catch (Exception _ex) {
                        // Catching exception if we stream too fast or too slow
                        try
                        {
                            WriteLine_SRG("ERROR: " + _ex.InnerException.Message);
                        }
                        catch { }

                        EndSRG();
                        StartSRG();
                    }
                };

            return 0;
        }

        private void WriteLine_SRG(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                tbLogsSRG.Text += formattedStr + "\n";
                tbLogsSRG.ScrollToEnd();
            });
        }

        #endregion

        // Merging Faces with Words In Time and sending to Power BI in JSON
        #region Processing to PBI

        private List<WordsInTime> wordsInTime;
        private List<FacesInTime> facesInTime;
        private DateTime lastTimeCheck = DateTime.MinValue;
        private bool sendToPBI = false;

        private void ProcessingToPBI()
        {
            // Remove faces and words that are too old
            facesInTime.RemoveAll(x => x.TimeStamp < lastTimeCheck);
            wordsInTime.RemoveAll(x => x.TimeStamp < lastTimeCheck);

            // Go through all faces ordered ascending by TimeStamp
            foreach (var faceInTime in facesInTime.OrderBy(x => x.TimeStamp))
            {
                // get timestamp of face
                DateTime date = faceInTime.TimeStamp;

                // find all sentences from last time check to timestamp of current frame
                List<WordsInTime> sentences = wordsInTime.Where(x => x.TimeStamp >= lastTimeCheck && x.TimeStamp <= date).ToList();

                // if we have any sentence that match to current frame with detected faces
                if (sentences.Count > 0)
                {
                    List<PBIItem> items = new List<PBIItem>();

                    // we call Standford Parser for removing unnecessary words
                    RemoveUnusedWords(ref sentences);

                    // split sentence to individual words for counts
                    List<string> words = new List<string>();
                    sentences.ForEach(x => x.Word.Split(' ').ToList().ForEach(y => words.Add(y)));

                    // Initialize Power BI JSON object for males
                    PBIItem mItem = InitializePBIItem(date);

                    // Check if we have any male faces detected and update JSON object
                    CreatePBIGenderItem(ref mItem, "Male", faceInTime.CameraResult, words.Count(), words.Distinct().Count());

                    // Initialize Power BI JSON object for females
                    PBIItem fItem = InitializePBIItem(date);

                    // Check if we have any female faces detected and update JSON object
                    CreatePBIGenderItem(ref fItem, "Female", faceInTime.CameraResult, words.Count(), words.Distinct().Count());

                    // go throught all sentences
                    foreach (var sentence in sentences)
                    {
                        // go throught all words in each sentence
                        foreach (var word in sentence.Word.Split(' '))
                        {
                            if (word.Trim().Length > 0)
                            {
                                // if any male is detected -> create Power BI item for any word
                                if (mItem != null)
                                {
                                    PBIItem mItemTemp = (PBIItem)mItem.Clone();

                                    mItemTemp.Beseda = word;
                                    mItemTemp.Language = sentence.Language;

                                    // Add it to items collection for Power BI
                                    items.Add(mItemTemp);
                                }

                                // if any female is detected -> create Power BI item for any word
                                if (fItem != null)
                                {
                                    PBIItem fItemTemp = (PBIItem)fItem.Clone();

                                    fItemTemp.Beseda = word;
                                    fItemTemp.Language = sentence.Language;

                                    // Add it to items collection for Power BI
                                    items.Add(fItemTemp);
                                }
                            }
                        }
                    }

                    // If we have any items for Power BI
                    if (items.Count > 0)
                    {
                        // write it down to log window
                        items.ForEach(x => WriteLine_PBI(x.Timestamp + " | Anger: " + x.Anger + " | Contempt: " + x.Contempt + " | Disgust: " + x.Disgust + " | Fear: " + x.Fear + " | Happiness: " + x.Happiness + " | Neutral: " + x.Neutral + " | Sadness: " + x.Sadness + " | Surprise: " + x.Surprise + " | " + x.Beseda + " | " + x.Gender));

                        // serialize JSON object with specific Date Formatter
                        string json = JsonConvert.SerializeObject(items, Formatting.None, new JsonSerializerSettings
                        {
                            DateFormatString = "yyyy-MM-ddTHH:mm:ssZ"
                        });

                        // If we want to send it to Power BI
                        if (sendToPBI)
                        {
                            // Create Web Request
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tbAPIURL.Text);
                            request.ContentType = "application/json; charset=utf-8";
                            request.Method = "POST";

                            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                            {
                                streamWriter.Write(json);
                                streamWriter.Flush();
                                streamWriter.Close();
                            }

                            // Check Web Response
                            WebResponse response = request.GetResponse();
                            var streamReader = new StreamReader(response.GetResponseStream());
                            var result = streamReader.ReadToEnd();

                            try
                            {
                                var wResp = (HttpWebResponse)response;
                                WriteLine_PBI(wResp.StatusCode.ToString());
                            }
                            catch (WebException we)
                            {
                                WriteLine_PBI(((HttpWebResponse)we.Response).StatusCode.ToString());
                            }
                        }

                        // If we want to send recognized datas to Azure Machine Learning
                        if (sendToAML)
                        {
                            //Thread threadAML = new Thread(x => SendToAML(items));
                            //threadAML.Start();
                            SendToAML(items);
                        }
                    }

                    // Set timestamp of current frame to last time check
                    lastTimeCheck = date;
                }
            }
        }

        private PBIItem InitializePBIItem(DateTime datum)
        {
            return new PBIItem()
            {
                Timestamp = datum,
                Hour = datum.Hour,
                Minute = datum.Minute,
                Anger = 0,
                Contempt = 0,
                Disgust = 0,
                Fear = 0,
                Happiness = 0,
                Neutral = 0,
                Sadness = 0,
                Surprise = 0,
                AvgHappiness = 0,
                HappinessHigh = 0,
                HappinessLow = 0,
                AvgSadness = 0,
                SadnessHigh = 0,
                SadnessLow = 0,
                AvgAnger = 0,
                AngerHigh = 0,
                AngerLow = 0,
                AvgNeutral = 0,
                NeutralHigh = 0,
                NeutralLow = 0,
                AvgContempt = 0,
                ContemptHigh = 0,
                ContemptLow = 0,
                AvgDisgust = 0,
                DisgustHigh = 0,
                DisgustLow = 0,
                AvgSurprise = 0,
                SurpriseHigh = 0,
                SurpriseLow = 0,
                AvgFear = 0,
                FearHigh = 0,
                FearLow = 0,
                NumOfInstances = 0,
                NumberOfWords = 0,
                NumberOfUniqueWords = 0,
                FacesRecognized = 0,
                Beseda = "",
                FacesInMeasurment = 0,
                ATAvgHappiness = 0,
                ATAvgSadness = 0,
                ATAvgAnger = 0,
                Speaker = "",
                ATAvgNeutral = 0,
                ATAvgContempt = 0,
                ATAvgDisgust = 0,
                ATAvgSurprise = 0,
                ATAvgFear = 0,
                Language = "",
                Gender = ""
            };
        }

        private void CreatePBIGenderItem(ref PBIItem item, string gender, LiveCameraResult cameraResult, int numberOfWords, int numberOfUniqueWords)
        {
            // get all male or female faces
            var genders = cameraResult.Faces.Where(x => x.FaceAttributes.Gender.Trim().ToUpper().Equals(gender.ToUpper()));
            // if we have no faces -> return nothing
            if (genders.Count() == 0)
            {
                item = null;
                return;
            }

            // sum all emotion attributes
            foreach (var emoItem in genders)
            {
                item.Anger += emoItem.FaceAttributes.Emotion.Anger;
                item.Contempt += emoItem.FaceAttributes.Emotion.Contempt;
                item.Disgust += emoItem.FaceAttributes.Emotion.Disgust;
                item.Fear += emoItem.FaceAttributes.Emotion.Fear;
                item.Happiness += emoItem.FaceAttributes.Emotion.Happiness;
                item.Neutral += emoItem.FaceAttributes.Emotion.Neutral;
                item.Sadness += emoItem.FaceAttributes.Emotion.Sadness;
                item.Surprise += emoItem.FaceAttributes.Emotion.Surprise;
            }

            // calculate average
            item.Anger /= genders.Count();
            item.Contempt /= genders.Count();
            item.Disgust /= genders.Count();
            item.Fear /= genders.Count();
            item.Happiness /= genders.Count();
            item.Neutral /= genders.Count();
            item.Sadness /= genders.Count();
            item.Surprise /= genders.Count();

            // if we have division by zero
            if (float.IsNaN(item.Anger)) item.Anger = 0;
            if (float.IsNaN(item.Contempt)) item.Contempt = 0;
            if (float.IsNaN(item.Disgust)) item.Disgust = 0;
            if (float.IsNaN(item.Fear)) item.Fear = 0;
            if (float.IsNaN(item.Happiness)) item.Happiness = 0;
            if (float.IsNaN(item.Neutral)) item.Neutral = 0;
            if (float.IsNaN(item.Sadness)) item.Sadness = 0;
            if (float.IsNaN(item.Surprise)) item.Surprise = 0;

            // other stuff like average happiness at all, hapiness high, hapiness low etc.
            item.AvgHappiness = sessionValues["AvgHappiness" + gender] = (sessionValues["AvgHappiness" + gender] + item.Happiness) / 2;
            if (float.IsNaN(item.AvgHappiness)) item.AvgHappiness = 0;

            item.HappinessHigh = sessionValues["HappinessHigh" + gender] = Math.Max(sessionValues["HappinessHigh" + gender], item.Happiness);
            item.HappinessLow = sessionValues["HappinessLow" + gender] = Math.Min(sessionValues["HappinessLow" + gender], item.Happiness);

            item.AvgSadness = sessionValues["AvgSadness" + gender] = (sessionValues["AvgSadness" + gender] + item.Sadness) / 2;
            if (float.IsNaN(item.AvgSadness)) item.AvgSadness = 0;

            item.SadnessHigh = sessionValues["SadnessHigh" + gender] = Math.Max(sessionValues["SadnessHigh" + gender], item.Sadness);
            item.SadnessLow = sessionValues["SadnessLow" + gender] = Math.Min(sessionValues["SadnessLow" + gender], item.Sadness);

            item.AvgAnger = sessionValues["AvgAnger" + gender] = (sessionValues["AvgAnger" + gender] + item.Anger) / 2;
            if (float.IsNaN(item.AvgAnger)) item.AvgAnger = 0;

            item.AngerHigh = sessionValues["AngerHigh" + gender] = Math.Max(sessionValues["AngerHigh" + gender], item.Anger);
            item.AngerLow = sessionValues["AngerLow" + gender] = Math.Min(sessionValues["AngerLow" + gender], item.Anger);

            item.AvgNeutral = sessionValues["AvgNeutral" + gender] = (sessionValues["AvgNeutral" + gender] + item.Neutral) / 2;
            if (float.IsNaN(item.AvgNeutral)) item.AvgNeutral = 0;

            item.NeutralHigh = sessionValues["NeutralHigh" + gender] = Math.Max(sessionValues["NeutralHigh" + gender], item.Neutral);
            item.NeutralLow = sessionValues["NeutralLow" + gender] = Math.Min(sessionValues["NeutralLow" + gender], item.Neutral);

            item.AvgContempt = sessionValues["AvgContempt" + gender] = (sessionValues["AvgContempt" + gender] + item.Contempt) / 2;
            if (float.IsNaN(item.AvgContempt)) item.AvgContempt = 0;

            item.ContemptHigh = sessionValues["ContemptHigh" + gender] = Math.Max(sessionValues["ContemptHigh" + gender], item.Contempt);
            item.ContemptLow = sessionValues["ContemptLow" + gender] = Math.Min(sessionValues["ContemptLow" + gender], item.Contempt);

            item.AvgDisgust = sessionValues["AvgDisgust" + gender] = (sessionValues["AvgDisgust" + gender] + item.Disgust) / 2;
            if (float.IsNaN(item.AvgDisgust)) item.AvgDisgust = 0;

            item.DisgustHigh = sessionValues["DisgustHigh" + gender] = Math.Max(sessionValues["DisgustHigh" + gender], item.Disgust);
            item.DisgustLow = sessionValues["DisgustLow" + gender] = Math.Min(sessionValues["DisgustLow" + gender], item.Disgust);

            item.AvgSurprise = sessionValues["AvgSurprise" + gender] = (sessionValues["AvgSurprise" + gender] + item.Surprise) / 2;
            if (float.IsNaN(item.AvgSurprise)) item.AvgSurprise = 0;

            item.SurpriseHigh = sessionValues["SurpriseHigh" + gender] = Math.Max(sessionValues["SurpriseHigh" + gender], item.Surprise);
            item.SurpriseLow = sessionValues["SurpriseLow" + gender] = Math.Min(sessionValues["SurpriseLow" + gender], item.Surprise);

            item.AvgFear = sessionValues["AvgFear" + gender] = (sessionValues["AvgFear" + gender] + item.Fear) / 2;
            if (float.IsNaN(item.AvgFear)) item.AvgFear = 0;

            item.FearHigh = sessionValues["FearHigh" + gender] = Math.Max(sessionValues["FearHigh" + gender], item.Fear);
            item.FearLow = sessionValues["FearLow" + gender] = Math.Min(sessionValues["FearLow" + gender], item.Fear);

            item.NumOfInstances = 0;
            item.NumberOfWords = numberOfWords;
            item.NumberOfUniqueWords = numberOfUniqueWords;

            item.FacesRecognized = genders.Count();
            item.FacesInMeasurment = genders.Count();

            item.ATAvgHappiness = globalValues["ATAvgHappiness" + gender] = (globalValues["ATAvgHappiness" + gender] + item.Happiness) / 2;
            if (float.IsNaN(item.ATAvgHappiness)) item.ATAvgHappiness = 0;
            item.ATAvgSadness = globalValues["ATAvgSadness" + gender] = (globalValues["ATAvgSadness" + gender] + item.Sadness) / 2;
            if (float.IsNaN(item.ATAvgSadness)) item.ATAvgSadness = 0;
            item.ATAvgAnger = globalValues["ATAvgAnger" + gender] = (globalValues["ATAvgAnger" + gender] + item.Anger) / 2;
            if (float.IsNaN(item.ATAvgAnger)) item.ATAvgAnger = 0;
            item.ATAvgNeutral = globalValues["ATAvgNeutral" + gender] = (globalValues["ATAvgNeutral" + gender] + item.Neutral) / 2;
            if (float.IsNaN(item.ATAvgNeutral)) item.ATAvgNeutral = 0;
            item.ATAvgContempt = globalValues["ATAvgContempt" + gender] = (globalValues["ATAvgContempt" + gender] + item.Contempt) / 2;
            if (float.IsNaN(item.ATAvgContempt)) item.ATAvgContempt = 0;
            item.ATAvgDisgust = globalValues["ATAvgDisgust" + gender] = (globalValues["ATAvgDisgust" + gender] + item.Disgust) / 2;
            if (float.IsNaN(item.ATAvgDisgust)) item.ATAvgDisgust = 0;
            item.ATAvgSurprise = globalValues["ATAvgSurprise" + gender] = (globalValues["ATAvgSurprise" + gender] + item.Surprise) / 2;
            if (float.IsNaN(item.ATAvgSurprise)) item.ATAvgSurprise = 0;
            item.ATAvgFear = globalValues["ATAvgFear" + gender] = (globalValues["ATAvgFear" + gender] + item.Fear) / 2;
            if (float.IsNaN(item.ATAvgFear)) item.ATAvgFear = 0;

            item.Speaker = tbSpeaker.Text;
            item.Gender = gender;
        }

        // When we want to start sending to Power BI
        private void btnStartPBI_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(tbAPIURL.Text))
            {
                WriteLine_PBI("API URL missing...");
                return;
            }

            if (String.IsNullOrEmpty(tbSpeaker.Text))
            {
                WriteLine_PBI("Speaker missing...");
                return;
            }

            sendToPBI = true;

            WriteLine_PBI("[Started]");
        }

        // When we want to end sending to Power BI
        private void btnEndPBI_Click(object sender, RoutedEventArgs e)
        {
            sendToPBI = false;

            WriteLine_PBI("[Ended]");
        }

        // Reset global values
        private void btnResetGlobal_Click(object sender, RoutedEventArgs e)
        {
            foreach (var gKey in globalValues.Keys)
                globalValues[gKey] = 0;
        }


        // Reset session related values
        private void btnResetSession_Click(object sender, RoutedEventArgs e)
        {
            foreach (var sKey in sessionValues.Keys)
                sessionValues[sKey] = 0;
        }

        private void WriteLine_PBI(string format)
        {
            Trace.WriteLine(format);
            Dispatcher.Invoke(() =>
            {
                tbLogsPBI.Text += format + "\n";
                tbLogsPBI.ScrollToEnd();
            });
        }

        #endregion

        // Removing unnecessary words (is, an, the, you etc.) with Standford Parser (Java implementation) -> natural language parser that works out the grammatical structure of sentences
        // Install-Package Stanford.NLP.Parser
        // https://nlp.stanford.edu/software/lex-parser.shtml
        #region STANDFORD PARSER

        private LexicalizedParser lp;

        private void RemoveUnusedWords(ref List<WordsInTime> sentences)
        {
            foreach (var sentence in sentences)
            {
                if (sentence.Language == "English")
                {
                    var tokenizerFactory = PTBTokenizer.factory(new CoreLabelTokenFactory(), "");
                    var sent2Reader = new java.io.StringReader(sentence.Word);
                    var rawWords = tokenizerFactory.getTokenizer(sent2Reader).tokenize();
                    sent2Reader.close();

                    var tree = lp.apply(rawWords);
                    var seznam = tree.taggedLabeledYield();

                    string tempSentence = "";
                    for (int i = 0; i < seznam.size(); i++)
                    {
                        var item = seznam.get(i).ToString();

                        // Remove unnecessary words by tags
                        if ((!item.StartsWith("DT-")) && (!item.StartsWith("TO-")) && (!item.StartsWith("IN-")) && (!item.StartsWith("VB-")) && (!item.StartsWith("VBZ-")) && (!item.StartsWith("VBD-")) && (!item.StartsWith("VBN-")) && (!item.StartsWith("WDT-")) && (!item.StartsWith("PRP-")) && (!item.StartsWith("CC-")) && (!item.StartsWith("CD-")))
                            tempSentence += rawWords.get(i) + " ";
                    }

                    // return modified sentence
                    sentence.Word = tempSentence;
                }
            }
        }

        #endregion

        // Sending data to Azure DB for processing in AML (Azure Machine Learning)
        #region AML

        AzureAMLDCDataContext db;
        private bool sendToAML = false;

        // Sending data to Azure DB for AML via Linq to SQL
        private void SendToAML(List<PBIItem> items)
        {
            foreach (var item in items)
            {
                InputData input = new InputData()
                {
                    ID = Guid.NewGuid(),
                    Timestamp = item.Timestamp,
                    Hour = item.Hour,
                    Minute = item.Minute,
                    Anger = item.Anger,
                    Contempt = item.Contempt,
                    Disgust = item.Disgust,
                    Fear = item.Fear,
                    Happiness = item.Happiness,
                    Neutral = item.Neutral,
                    Sadness = item.Sadness,
                    Surprise = item.Surprise,
                    AvgHappiness = item.AvgHappiness,
                    HappinessHigh = item.HappinessHigh,
                    HappinessLow = item.HappinessLow,
                    AvgSadness = item.AvgSadness,
                    SadnessHigh = item.SadnessHigh,
                    SadnessLow = item.SadnessLow,
                    AvgAnger = item.AvgAnger,
                    AngerHigh = item.AngerHigh,
                    AngerLow = item.AngerLow,
                    AvgNeutral = item.AvgNeutral,
                    NeutralHigh = item.NeutralHigh,
                    NeutralLow = item.NeutralLow,
                    AvgContempt = item.AvgContempt,
                    ContemptHigh = item.ContemptHigh,
                    ContemptLow = item.ContemptLow,
                    AvgDisgust = item.AvgDisgust,
                    DisgustHigh = item.DisgustHigh,
                    DisgustLow = item.DisgustLow,
                    AvgSurprise = item.AvgSurprise,
                    SurpriseHigh = item.SurpriseHigh,
                    SurpriseLow = item.SurpriseLow,
                    AvgFear = item.AvgFear,
                    FearHigh = item.FearHigh,
                    FearLow = item.FearLow,
                    NumOfInstances = item.NumOfInstances,
                    NumberOfWords = item.NumberOfWords,
                    NumberOfUniqueWords = item.NumberOfUniqueWords,
                    FacesRecognized = item.FacesRecognized,
                    Beseda = item.Beseda,
                    FacesInMeasurment = item.FacesInMeasurment,
                    ATAvgHappiness = item.ATAvgHappiness,
                    ATAvgSadness = item.ATAvgSadness,
                    ATAvgAnger = item.ATAvgAnger,
                    Speaker = item.Speaker,
                    ATAvgNeutral = item.ATAvgNeutral,
                    ATAvgContempt = item.ATAvgContempt,
                    ATAvgDisgust = item.ATAvgDisgust,
                    ATAvgSurprise = item.ATAvgSurprise,
                    ATAvgFear = item.ATAvgFear,
                    Language = item.Language,
                    Gender = item.Gender
                };

                db.InputDatas.InsertOnSubmit(input);
            }

            db.SubmitChanges();

            WriteLine_AML(DateTime.Now + " | New items successfully added into Azure DB");
        }

        // When you want to start sending to AML
        private void btnStartAML_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(tbDBConnectionString.Text))
            {
                WriteLine_AML("DB Connection String missing...");
                return;
            }

            if (String.IsNullOrEmpty(tbSpeaker.Text))
            {
                WriteLine_AML("Speaker missing...");
                return;
            }

            db = new AzureAMLDCDataContext(tbDBConnectionString.Text);

            sendToAML = true;

            WriteLine_AML("[Started]");
        }

        // When you want to end sending to AML
        private void btnEndAML_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sendToAML = false;

                db.Dispose();
            }
            catch { }

            WriteLine_AML("[Ended]");
        }

        private void WriteLine_AML(string format)
        {
            Trace.WriteLine(format);
            Dispatcher.Invoke(() =>
            {
                tbLogsAML.Text += format + "\n";
                tbLogsAML.ScrollToEnd();
            });
        }

        #endregion

        // Timer for processing datas in Faces & Words Queues every 5 seconds
        private DispatcherTimer timer;
        private int timerInterval = 5;  // seconds

        // Loading & Saving permanent datas to Configuration file
        private Configuration config;

        // Permanent datas splitted to global values and session related values
        private Dictionary<string, float> globalValues = new Dictionary<string, float>();
        private Dictionary<string, float> sessionValues = new Dictionary<string, float>();

        public MainWindow()
        {
            InitializeComponent();



            // ***** Face Recognition *****

            // Create empty queue for detected faces in time
            facesInTime = new List<FacesInTime>();

            // Initialize Face Recognition
            grabber = new FrameGrabber<LiveCameraResult>(tbLogsFR);
            InitializeFaceRecognition();



            // ***** Speech Recognition *****

            // Create empty queue for detected words in time
            wordsInTime = new List<WordsInTime>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ***** Face Recognition *****
            faceClient = new FaceServiceClient("{enter-face-api-subscription-key}", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");

            // ***** Speech Recognition (Google) *****
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
            //await FromMicrophone();

            // ***** Beri iz configuration fajla *****
            config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);

            try { tbAPIURL.Text = config.AppSettings.Settings["tbAPIURL"].Value; } catch { }
            try { tbSpeaker.Text = config.AppSettings.Settings["tbSpeaker"].Value; } catch { }
            try { tbDBConnectionString.Text = config.AppSettings.Settings["tbDBConnectionString"].Value; } catch { }

            InitializeGenderVariables("Male");
            InitializeGenderVariables("Female");

            // ***** PBI *****
            // we initialize timer
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, timerInterval);
            timer.Start();

            // ***** STANDFORD PARSER *****
            string folder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string pathToStandfordModel = folder + @"\NLP\models\edu\stanford\nlp\models\lexparser\englishPCFG.ser.gz";
            lp = LexicalizedParser.loadModel(pathToStandfordModel);
        }

        // When timer tick
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // we call Processing to PBI functionality
                ProcessingToPBI();
            }
            catch (Exception _ex)
            {
                WriteLine_PBI("ERROR Timer: " + _ex.Message);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // ***** Speech Recognition *****
            if (null != micClient)
            {
                micClient.Dispose();
            }

            // ****** PBI *****
            if (timer != null)
                timer.Stop();

            // ***** Piši v configuration fajl *****
            if (config.AppSettings.Settings["tbAPIURL"] == null)
                config.AppSettings.Settings.Add("tbAPIURL", tbAPIURL.Text);
            else
                config.AppSettings.Settings["tbAPIURL"].Value = tbAPIURL.Text;

            if (config.AppSettings.Settings["tbSpeaker"] == null)
                config.AppSettings.Settings.Add("tbSpeaker", tbSpeaker.Text);
            else
                config.AppSettings.Settings["tbSpeaker"].Value = tbSpeaker.Text;

            if (config.AppSettings.Settings["tbDBConnectionString"] == null)
                config.AppSettings.Settings.Add("tbDBConnectionString", tbDBConnectionString.Text);
            else
                config.AppSettings.Settings["tbDBConnectionString"].Value = tbDBConnectionString.Text;

            SaveGenderVariables("Male");
            SaveGenderVariables("Female");

            config.Save(ConfigurationSaveMode.Minimal);

            base.OnClosed(e);
        }

        private void InitializeGenderVariables(string gender)
        {
            FillSessionValue("AvgHappiness" + gender); FillSessionValue("HappinessHigh" + gender); FillSessionValue("HappinessLow" + gender);
            FillSessionValue("AvgSadness" + gender); FillSessionValue("SadnessHigh" + gender); FillSessionValue("SadnessLow" + gender);
            FillSessionValue("AvgAnger" + gender); FillSessionValue("AngerHigh" + gender); FillSessionValue("AngerLow" + gender);
            FillSessionValue("AvgNeutral" + gender); FillSessionValue("NeutralHigh" + gender); FillSessionValue("NeutralLow" + gender);
            FillSessionValue("AvgContempt" + gender); FillSessionValue("ContemptHigh" + gender); FillSessionValue("ContemptLow" + gender);
            FillSessionValue("AvgDisgust" + gender); FillSessionValue("DisgustHigh" + gender); FillSessionValue("DisgustLow" + gender);
            FillSessionValue("AvgSurprise" + gender); FillSessionValue("SurpriseHigh" + gender); FillSessionValue("SurpriseLow" + gender);
            FillSessionValue("AvgFear" + gender); FillSessionValue("FearHigh" + gender); FillSessionValue("FearLow" + gender);

            FillGlobalValue("ATAvgHappiness" + gender); FillGlobalValue("ATAvgSadness" + gender); FillGlobalValue("ATAvgAnger" + gender); FillGlobalValue("ATAvgNeutral" + gender); FillGlobalValue("ATAvgContempt" + gender); FillGlobalValue("ATAvgDisgust" + gender); FillGlobalValue("ATAvgSurprise" + gender); FillGlobalValue("ATAvgFear" + gender);
        }

        private void SaveGenderVariables(string gender)
        {
            ReadSessionValue("AvgHappiness" + gender); ReadSessionValue("HappinessHigh" + gender); ReadSessionValue("HappinessLow" + gender);
            ReadSessionValue("AvgSadness" + gender); ReadSessionValue("SadnessHigh" + gender); ReadSessionValue("SadnessLow" + gender);
            ReadSessionValue("AvgAnger" + gender); ReadSessionValue("AngerHigh" + gender); ReadSessionValue("AngerLow" + gender);
            ReadSessionValue("AvgNeutral" + gender); ReadSessionValue("NeutralHigh" + gender); ReadSessionValue("NeutralLow" + gender);
            ReadSessionValue("AvgContempt" + gender); ReadSessionValue("ContemptHigh" + gender); ReadSessionValue("ContemptLow" + gender);
            ReadSessionValue("AvgDisgust" + gender); ReadSessionValue("DisgustHigh" + gender); ReadSessionValue("DisgustLow" + gender);
            ReadSessionValue("AvgSurprise" + gender); ReadSessionValue("SurpriseHigh" + gender); ReadSessionValue("SurpriseLow" + gender);
            ReadSessionValue("AvgFear" + gender); ReadSessionValue("FearHigh" + gender); ReadSessionValue("FearLow" + gender);

            ReadGlobalValue("ATAvgHappiness" + gender); ReadGlobalValue("ATAvgSadness" + gender); ReadGlobalValue("ATAvgAnger" + gender); ReadGlobalValue("ATAvgNeutral" + gender); ReadGlobalValue("ATAvgContempt" + gender); ReadGlobalValue("ATAvgDisgust" + gender); ReadGlobalValue("ATAvgSurprise" + gender); ReadGlobalValue("ATAvgFear" + gender);
        }

        private void FillSessionValue(string key)
        {
            try
            {
                sessionValues.Add(key, float.Parse(config.AppSettings.Settings[key].Value));
            }
            catch
            {
                sessionValues.Add(key, 0);
            }
        }

        private void ReadSessionValue(string key)
        {
            if (config.AppSettings.Settings[key] == null)
                config.AppSettings.Settings.Add(key, sessionValues[key].ToString());
            else
                config.AppSettings.Settings[key].Value = sessionValues[key].ToString();
        }

        private void FillGlobalValue(string key)
        {
            try
            {
                globalValues.Add(key, float.Parse(config.AppSettings.Settings[key].Value));
            }
            catch
            {
                globalValues.Add(key, 0);
            }
        }

        private void ReadGlobalValue(string key)
        {
            if (config.AppSettings.Settings[key] == null)
                config.AppSettings.Settings.Add(key, globalValues[key].ToString());
            else
                config.AppSettings.Settings[key].Value = globalValues[key].ToString();
        }
    }
}
