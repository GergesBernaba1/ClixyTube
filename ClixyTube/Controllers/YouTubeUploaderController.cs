﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Auth.AspNetCore3;

namespace ClixyTube.Controllers
{
    [ApiController]
    [Route("[controller]/[Action]")]
    public class YouTubeUploaderController : ControllerBase
    {
        private readonly ILogger<YouTubeUploaderController> _logger;

        public YouTubeUploaderController(ILogger<YouTubeUploaderController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Obsolete]
        public async Task<IActionResult> UploadVideo(IFormFile videoFile)
        {
            string title = "Test";
            string description = "Test desc";
            string[] tags = { "video", "test" };
            string categoryId = "22";
            try
            {

                // Define parameters for the video
                var video = new Video();
                video.Snippet = new VideoSnippet();
                video.Snippet.Title = title;
                video.Snippet.Description = description;
                video.Snippet.Tags = tags;
                video.Snippet.CategoryId = categoryId; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
                video.Status = new VideoStatus();
                video.Status.PrivacyStatus = "public"; // "private" or "public" or unlisted

                // Authenticate and create the YouTube API service
                //UserCredential credential;
                //using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                //{
                //    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                //        GoogleClientSecrets.Load(stream).Secrets,
                //        new[] { YouTubeService.Scope.YoutubeUpload },
                //        "user",
                //        CancellationToken.None,
                //        new FileDataStore("YouTubeUploader")
                //    );
                //}
                UserCredential credential;
                using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    var secrets = GoogleClientSecrets.Load(stream).Secrets;
                    var initializer = new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = secrets,
                        Scopes = new[] { YouTubeService.Scope.YoutubeUpload },
                        DataStore = new FileDataStore("YouTubeUploader"),
                        // Use the following properties to set the authorization server URL, token server URL, and redirect URI
                        // AuthorizationUrl = new Uri("https://accounts.google.com/o/oauth2/auth"),
                        // TokenUrl = new Uri("https://oauth2.googleapis.com/token"),
                        // RedirectUri = "https://localhost:44373/oauth2/callback/google"
                    };
                    var flow = new GoogleAuthorizationCodeFlow(initializer);
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        new ClientSecrets
                        {
                            //ClientId = secrets.ClientId,
                            //ClientSecret = secrets.ClientSecret
                            ClientId = "283865851701-ik9h69h441cpmalq5km6pbt1f1jbav0s.apps.googleusercontent.com",
                            ClientSecret = "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"
                        },
                        new[] { YouTubeService.Scope.YoutubeUpload },
                        "user",
                        CancellationToken.None
                    );
                }

                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "ClixyTube"
                });

                // Create a video insert request
                var insertRequest = youtubeService.Videos.Insert(video, "snippet,status", videoFile.OpenReadStream(), videoFile.ContentType);
                insertRequest.ProgressChanged += InsertRequest_ProgressChanged;
                insertRequest.ResponseReceived += InsertRequest_ResponseReceived;

                // Execute the upload
                await insertRequest.UploadAsync();

                return Ok("Upload complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video to YouTube.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error uploading video to YouTube.");
            }
        }

        private void InsertRequest_ResponseReceived(Video video)
        {
            // Video was successfully uploaded
            _logger.LogInformation("Video id '{0}' was successfully uploaded.", video.Id);
        }

        private void InsertRequest_ProgressChanged(IUploadProgress progress)
        {
            // Report upload progress
            _logger.LogInformation("Upload status: {0} ({1}/{2} bytes)", progress.Status, progress.BytesSent);
        }

    }
}
