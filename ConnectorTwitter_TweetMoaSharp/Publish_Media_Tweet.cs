using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TweetSharp;

namespace Twitter
{
    public class Publish_Media_Tweet : CodeActivity
    {
        [Category("Input Authentication")]
        [RequiredArgument]
        public InArgument<String> ConsumerKey { get; set; }

        [Category("Input Authentication")]
        [RequiredArgument]
        public InArgument<String> ConsumerSecret { get; set; }

        [Category("Input Authentication")]
        [RequiredArgument]
        public InArgument<String> AccessToken { get; set; }

        [Category("Input Authentication")]
        [RequiredArgument]
        public InArgument<String> AccessSecret { get; set; }

        [Category("Tweet Details")]
        [RequiredArgument]
        public InArgument<String> Message { get; set; }

        [Category("Tweet Details")]
        public InArgument<String> MediaPath { get; set; }

        [Category("Output")]
        public OutArgument<long> TweetId {get; set;}

        public static TwitterService service;

        protected override void Execute(CodeActivityContext context)
        {
            service = new TwitterService(ConsumerKey.Get(context),
                ConsumerSecret.Get(context),
                AccessToken.Get(context),
                AccessSecret.Get(context));
            service.TraceEnabled = true;

            using (var stream = new FileStream(MediaPath.Get(context), FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                TwitterChunkedMedia uploadedMedia = InitialiseMediaUpload(service, stream);

                UploadMediaChunks(service, stream, uploadedMedia);

                FinializeMediaAndWaitForProcessing(service, uploadedMedia);

                // Now send a tweet with the media attached
                var twitterStatus = service.SendTweet(new SendTweetOptions()
                {
                    Status = Message.Get(context),
                    MediaIds = new string[] { uploadedMedia.MediaId.ToString() }
                });

                //AssertResultWas(service, HttpStatusCode.OK);
                //Assert.IsNotNull(twitterStatus);

                // Capture Posted Tweet Id
                TweetId.Set(context, twitterStatus.Id);
            }

            


        }

        private static TwitterChunkedMedia InitialiseMediaUpload(TwitterService service, FileStream stream)
        {
            var uploadedMedia = service.UploadMediaInit(new UploadMediaInitOptions
            {
                MediaType = "video/mp4",
                TotalBytes = stream.Length,
                MediaCategory = TwitterMediaCategory.Video
            });

            //AssertResultWas(service, HttpStatusCode.Accepted);
            //Assert.IsNotNull(uploadedMedia);
            //Assert.AreNotEqual(0, uploadedMedia.MediaId);
            return uploadedMedia;
        }


        private static void UploadMediaChunks(TwitterService service, FileStream stream, TwitterChunkedMedia uploadedMedia)
        {
            long chunkSize = 1024 * 512;
            long index = 0;
            byte[] buffer = new byte[chunkSize];

            while (stream.Position < stream.Length)
            {
                int thisChunkSize = (int)Math.Min(stream.Length - stream.Position, chunkSize);
                if (thisChunkSize != chunkSize)
                    buffer = new byte[thisChunkSize];

                stream.Read(buffer, 0, thisChunkSize);
                var ms = new System.IO.MemoryStream(buffer);

                service.UploadMediaAppend(new UploadMediaAppendOptions
                {
                    MediaId = uploadedMedia.MediaId,
                    SegmentIndex = index,
                    Media = new MediaFile()
                    {
                        //FileName = "test_video.mp4",
                        Content = ms
                    }
                });
                //AssertResultWas(service, HttpStatusCode.NoContent);

                index++;
            }
        }

        private static void FinializeMediaAndWaitForProcessing(TwitterService service, TwitterChunkedMedia uploadedMedia)
        {
            var result = service.UploadMediaFinalize(new UploadMediaFinalizeOptions()
            {
                MediaId = uploadedMedia.MediaId
            });

            var done = false;
            while (!done)
            {
                //AssertResultWas(service, HttpStatusCode.OK);
                if (result.ProcessingInfo.Error != null)
                {
                    throw new InvalidOperationException(result.ProcessingInfo.Error.Code + " - " + result.ProcessingInfo.Error.Message);
                }

                var state = (result.ProcessingInfo?.State ?? TwitterMediaProcessingState.Succeeded);
                done = state == TwitterMediaProcessingState.Succeeded || state == TwitterMediaProcessingState.Failed;
                if (!done)
                {
                    System.Threading.Thread.Sleep(Convert.ToInt32((result?.ProcessingInfo?.CheckAfterSeconds ?? 5) * 1000));
                    result = service.UploadMediaCheckStatus(new UploadMediaCheckStatusOptions() { MediaId = uploadedMedia.MediaId });
                }
            }
        }



    }
}
