using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.ComponentModel;

namespace Backdrops
{
    public class DownloadManager
    {
        private class Request
        {
            public RequestCallback Callback;
            public string URL;
            public string Path;

            public Request(RequestCallback Callback, string URL, string Path)
            {
                this.Callback = Callback;
                this.URL = URL;
                this.Path = Path;
            }
        }

        public enum DownloadType
        {
            API,
            Wallpaper,
            Thumbnail
        }

        Thread RequestMonitor;

        bool APIClientBusy;
        bool WallpaperClientBusy;
        bool[] ThumbnailClientsBusy;

        Request CurrentAPIRequest;
        Request CurrentWallpaperRequest;
        Request[] CurrentThumbnailRequest;

        Queue<Request> APIRequests;
        Queue<Request> WallpaperRequests;
        Queue<Request> ThumbnailRequests;

        WebClient APIClient;
        WebClient WallpaperClient;
        WebClient[] ThumbnailClients;

        /// <summary>
        /// Initialises a new download manager.
        /// </summary>
        /// <param name="ThumbnailClientCount">The number of concurrent thumbnail downloads, defaults to 4.</param>
        public DownloadManager(int ThumbnailClientCount = 4)
        {
            CurrentThumbnailRequest = new Request[ThumbnailClientCount];

            APIRequests = new Queue<Request>();
            WallpaperRequests = new Queue<Request>();
            ThumbnailRequests = new Queue<Request>();

            APIClient = new WebClient();
            WallpaperClient = new WebClient();
            ThumbnailClients = new WebClient[ThumbnailClientCount];
            for (int i = 0; i < ThumbnailClientCount; i++)
            {
                ThumbnailClients[i] = new WebClient();
            }

            APIClient.DownloadStringCompleted += APIRequestCompleted;
            WallpaperClient.DownloadFileCompleted += WallpaperRequestCompleted;
            for (int i = 0; i < ThumbnailClientCount; i++)
            {
                var index = i;
                ThumbnailClients[i].DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { ThumbnailRequestCompleted(index); };
            }

            APIClientBusy = false;
            WallpaperClientBusy = false;
            ThumbnailClientsBusy = new bool[ThumbnailClientCount];

            RequestMonitor = new Thread(MonitorRequests);
            RequestMonitor.Start();
        }

        public delegate void RequestCallback(string Result);
        public void CreateRequest(DownloadType Type, RequestCallback Callback, string URL, string Path = "")
        {
            switch (Type)
            {
                case DownloadType.API:
                    APIRequests.Enqueue(new Request(Callback, URL, Path));
                    break;
                case DownloadType.Wallpaper:
                    WallpaperRequests.Enqueue(new Request(Callback, URL, Path));
                    break;
                case DownloadType.Thumbnail:
                    ThumbnailRequests.Enqueue(new Request(Callback, URL, Path));
                    break;
            }
        }

        public void Stop()
        {
            RequestMonitor.Abort();
        }

        private void APIRequestCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            CurrentAPIRequest.Callback?.Invoke(e.Result);
            APIClientBusy = false;
        }

        private void WallpaperRequestCompleted(object sender, AsyncCompletedEventArgs e)
        {
            CurrentWallpaperRequest.Callback?.Invoke(CurrentWallpaperRequest.Path);
            WallpaperClientBusy = false;
        }

        private void ThumbnailRequestCompleted(int ID)
        {
            CurrentThumbnailRequest[ID].Callback?.Invoke(CurrentThumbnailRequest[ID].Path);
            ThumbnailClientsBusy[ID] = false;
        }

        private void MonitorRequests()
        {
            while (true)
            {
                if (APIRequests.Count > 0 && !APIClientBusy)
                {
                    APIClientBusy = true;
                    CurrentAPIRequest = APIRequests.Dequeue();
                    APIClient.DownloadStringAsync(new Uri(CurrentAPIRequest.URL));
                }

                if (WallpaperRequests.Count > 0 && !WallpaperClientBusy)
                {
                    WallpaperClientBusy = true;
                    CurrentWallpaperRequest = WallpaperRequests.Dequeue();
                    WallpaperClient.DownloadFileAsync(new Uri(CurrentWallpaperRequest.URL), CurrentWallpaperRequest.Path);
                }

                for (int i = 0; i < ThumbnailClients.Length; i++)
                {
                    if (ThumbnailRequests.Count > 0 && !ThumbnailClientsBusy[i])
                    {
                        ThumbnailClientsBusy[i] = true;
                        CurrentThumbnailRequest[i] = ThumbnailRequests.Dequeue();
                        ThumbnailClients[i].DownloadFileAsync(new Uri(CurrentThumbnailRequest[i].URL), CurrentThumbnailRequest[i].Path);
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
