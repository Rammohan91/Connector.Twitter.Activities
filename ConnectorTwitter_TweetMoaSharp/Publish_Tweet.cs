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
    public class Publish_Tweet : CodeActivity
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
        public InArgument<String> ImagePath { get; set; }

        [Category("Output")]
        public OutArgument<long> TweetId { get; set; }

        public static TwitterService service;

        protected override void Execute(CodeActivityContext context)
        {
            try
            {

                service = new TwitterService(ConsumerKey.Get(context),
                    ConsumerSecret.Get(context),
                    AccessToken.Get(context),
                    AccessSecret.Get(context));

                TwitterStatus status;
                if (ImagePath.Get(context) == null)
                {
                    // Send Tweet without Image
                    status = service.SendTweet(new SendTweetOptions { Status = Message.Get(context) });
                }
                else
                {
                    // Send Tweet with Image
                    MediaFile mf = new MediaFile();
                    Stream stream = new FileStream(ImagePath.Get(context), FileMode.Open);
                    mf.Content = stream;

                    var media = service.UploadMedia(new UploadMediaOptions { Media = mf });
                    var id = media.Media_Id;

                    List<string> ls = new List<string>();
                    ls.Add(id.ToString());

                    status = service.SendTweet(new SendTweetOptions
                    {
                        Status = Message.Get(context),
                        MediaIds = ls,
                    });

                }

                // Capture Posted Tweet Id
                TweetId.Set(context, status.Id);
            }
            catch (Exception e)
            {
                TweetId.Set(context, e.Message);
            }
        }
    }
}
