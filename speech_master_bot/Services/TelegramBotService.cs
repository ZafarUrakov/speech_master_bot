using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace speech_master_bot.Services
{
    public class TelegramBotService
    {
        private readonly ILogger<TelegramBotService> logger;
        private readonly ITelegramBotClient telegramBotClient;

        public TelegramBotService(
            ITelegramBotClient telegramBotClient,
            ILogger<TelegramBotService> logger)
        {
            this.telegramBotClient = telegramBotClient;
            this.logger = logger;
        }

        public async ValueTask EchoAsync(Update update)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageRecieved(update.Message),
                UpdateType.CallbackQuery => BotOnCallBackQueryRecieved(update.CallbackQuery),
                _ => UnknownUpdateTypeHandler(update)
            };

            try
            {
                await handler;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
            }
        }

        public ValueTask HandleErrorAsync(Exception ex)
        {
            var errorMessage = ex switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error :\n{apiRequestException.ErrorCode}",
                _ => ex.ToString()
            };

            this.logger.LogInformation(errorMessage);

            return ValueTask.CompletedTask;
        }

        private ValueTask UnknownUpdateTypeHandler(Update update)
        {
            this.logger.LogInformation($"Unknown upodate type: {update.Type}");

            return ValueTask.CompletedTask;
        }

        private async ValueTask BotOnCallBackQueryRecieved(CallbackQuery callbackQuery)
        {
            await telegramBotClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"{callbackQuery.Data}");
        }

        private async ValueTask BotOnMessageRecieved(Message message)
        {

            if (message.Text is not null)
            {
                await this.telegramBotClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "here text");
            }
            else
            {
                var voice = message.Voice;

                var fileId = message.Voice.FileId;
                var file1 = await this.telegramBotClient.GetFileAsync(fileId);
                string filePath1 = file1.FilePath;

                var voiceFile = await this.telegramBotClient.GetFileAsync(voice.FileId);

                var filePath = "../../../wwwroot/audio.ogg";

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await this.telegramBotClient.DownloadFileAsync(voiceFile.FilePath, fileStream);
                }

                string mp3FilePath = "../../../wwwroot/audio.wav";

                ConvertOggToWav(filePath, mp3FilePath);

                Console.WriteLine("Конвертация завершена.");

                var config = SpeechConfig.FromSubscription("b0865984f22d42ebb91601daa3eb27a7", "eastus");

                string language = "en-US";
                string topic = "your own topic";

                var audioConfig = AudioConfig.FromWavFileInput(mp3FilePath);

                var speechRecognizer = new SpeechRecognizer(config, language.Replace("_", "-"), audioConfig);

                var connection = Connection.FromRecognizer(speechRecognizer);

                var phraseDetectionConfig = new
                {
                    enrichment = new
                    {
                        pronunciationAssessment = new
                        {
                            referenceText = "",
                            gradingSystem = "HundredMark",
                            granularity = "Word",
                            dimension = "Comprehensive",
                            enableMiscue = "False",
                            enableProsodyAssessment = "True"
                        },
                        contentAssessment = new
                        {
                            topic = topic
                        }
                    }
                };
                connection.SetMessageProperty("speech.context", "phraseDetection", JsonConvert.SerializeObject(phraseDetectionConfig));

                var phraseOutputConfig = new
                {
                    format = "Detailed",
                    detailed = new
                    {
                        options = new[]
                        {
                    "WordTimings",
                    "PronunciationAssessment",
                    "ContentAssessment",
                    "SNR",
                }
                    }
                };

                connection.SetMessageProperty("speech.context", "phraseOutput", JsonConvert.SerializeObject(phraseOutputConfig));

                var done = false;
                var fullRecognizedText = "";

                speechRecognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("Closing on {0}", e);
                    done = true;
                };

                speechRecognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine("Closing on {0}", e);
                    done = true;
                };

                connection.MessageReceived += (s, e) =>
                {
                    if (e.Message.IsTextMessage())
                    {
                        var messageText = e.Message.GetTextMessage();
                        var json = Newtonsoft.Json.Linq.JObject.Parse(messageText);
                        if (json.ContainsKey("NBest"))
                        {
                            var nBest = json["NBest"][0];
                            if (nBest["Display"].ToString().Trim().Length > 1)
                            {
                                var recognizedText = json["DisplayText"];

                                fullRecognizedText += $" {recognizedText}";
                                var accuracyScore = nBest["PronunciationAssessment"]["AccuracyScore"].ToString();
                                var fluencyScore = nBest["PronunciationAssessment"]["FluencyScore"].ToString();
                                var prosodyScore = nBest["PronunciationAssessment"]["ProsodyScore"].ToString();
                                var completenessScore = nBest["PronunciationAssessment"]["CompletenessScore"].ToString();
                                var pronScore = nBest["PronunciationAssessment"]["PronScore"].ToString();

                                this.telegramBotClient.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: $"Accuracy Score {accuracyScore}\n" +
                                    $"Fluency Score: {fluencyScore}\n" +
                                    $"Prosody Score {prosodyScore}\n" +
                                    $"Completeness Score {completenessScore}\n" +
                                    $"PronScore {pronScore}");
                            }
                            else
                            {
                                Console.WriteLine($"Content Assessment Results for: {fullRecognizedText}");
                            }
                        }
                    }
                };

                await speechRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                while (!done)
                {
                    await Task.Delay(1000);
                }

                await speechRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }

        static void ConvertOggToWav(string inputFilePath, string outputFilePath)
        {
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            ffMpeg.ConvertMedia(inputFilePath, outputFilePath, "wav");
        }
    }
}
