﻿using Android.App;
using Android.App.Job;
using Android.Util;
using Astropix.DataRepository;
using Astropix.Factories;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;

namespace Astropix.Services
{
    [Service(Exported = true, Permission = "android.permission.BIND_JOB_SERVICE")]
    internal class WorkerService : JobService
    {
        private readonly string urlplussecret = "https://api.nasa.gov/planetary/apod?api_key=h7fNJpWLRMhp0DLCOn2FmrXNuEVhx3yzLpolkEs9";
        private ImageOfTheDay imageOfTheDay;

        public override bool OnStartJob(JobParameters @params)
        {
            Log.Info("Astropix", "Its time to download data");
            try
            {
                ThreadPool.QueueUserWorkItem(async m =>
                {
                    //FIX ME: I crash when an HTTP Error occurs. (-:
                    var httpClient = new HttpClient();
                    var result = await httpClient.GetStringAsync(urlplussecret);
                    var post = JsonConvert.DeserializeObject<ImageOfTheDay>(result);
                    imageOfTheDay = post;
                    using (var dbhelper = new DBHelper())
                    {
                        //If this query returns false, then Insert the new registry to the database.
                        //and download the image from the url and set it as wallpaper.

                        //If returns true, it means that the registry already exists, and it won't do anything, to avoid
                        //inserting the same item twice or even more.
                        if (!dbhelper.SelectQueryImageOfTheDay(post.Hdurl))
                        {
                            dbhelper.InsertIntoTableImageOfTheDay(imageOfTheDay);
                            if (post.Media_Type == "image")
                            {
                                ImageComposer.SetDownloadedImageAsBackground(post.Url);
                            }
                        }
                    }
                });
            }
            catch
            {
                return false; //Failed download, needs rescheduling.
            }

            return true;
        }

        public override bool OnStopJob(JobParameters @params)
        {
            Log.Info("Astropix", "OnStopJob Called");
            return true;
        }
    }
}