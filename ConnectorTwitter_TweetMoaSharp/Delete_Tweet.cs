using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TweetSharp;

namespace Twitter
{
    public class Delete_Tweet : CodeActivity
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
        public InArgument<long> TweetID { get; set; }

        public static TwitterService service;

        protected override void Execute(CodeActivityContext context)
        {
            service = new TwitterService(ConsumerKey.Get(context),
                ConsumerSecret.Get(context),
                AccessToken.Get(context),
                AccessSecret.Get(context));

            service.DeleteTweet(new DeleteTweetOptions { Id = TweetID.Get(context) });

        }

    }
}
