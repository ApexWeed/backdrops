using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Backdrops
{
    public partial class WallpaperControl : UserControl
    {
        const string BASE_THUMB_URL = "http://www.backdrops.io/walls/upload/";
        const string CACHE_DIR = "cache";
        const string WALLPAPER_DIR = "walls";

        Wallpaper Wallpaper;
        DownloadManager DownloadManager;
        Image Thumbnail;
        bool Saving;

        public WallpaperControl(Wallpaper Wallpaper, DownloadManager DownloadManager)
        {
            InitializeComponent();
            this.Wallpaper = Wallpaper;
            this.DownloadManager = DownloadManager;
            lblTitle.Text = Wallpaper.Title;
            lblUser.Text = Wallpaper.User;
            lblTitle.Left = (this.Width / 2) - (TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font).Width / 2);
            lblUser.Left = (this.Width / 2) - (TextRenderer.MeasureText(lblUser.Text, lblUser.Font).Width / 2);
            LoadThumbnail(Wallpaper.ThumbURL);
            Saving = false;

            if (File.Exists(Path.Combine(WALLPAPER_DIR, Wallpaper.ImageURL)))
            {
                btnSave.Text = "Saved";
                btnSave.Enabled = false;
            }
        }

        private void LoadThumbnail(string url)
        {
            // Use cached version if it exists.
            if (File.Exists(Path.Combine(CACHE_DIR, url)))
            {
                SetImage(Path.Combine(CACHE_DIR, url));
            }
            else
            {
                if (!Directory.Exists(CACHE_DIR))
                {
                    Directory.CreateDirectory(CACHE_DIR);
                }

                DownloadManager.CreateRequest(DownloadManager.DownloadType.Thumbnail, SetImage, $"{BASE_THUMB_URL}{url}", Path.Combine(CACHE_DIR, url));
            }
        }

        private void SetImage(string Path)
        {
            Thumbnail = Image.FromFile(Path);
            picThumb.Image = Thumbnail;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                if (Thumbnail != null)
                {
                    Thumbnail.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!Saving && !File.Exists(Path.Combine(WALLPAPER_DIR, Wallpaper.ImageURL)))
            {
                Saving = true;
                DownloadManager.CreateRequest(DownloadManager.DownloadType.Wallpaper, WallpaperSaved, $"{BASE_THUMB_URL}{Wallpaper.ImageURL}", Path.Combine(WALLPAPER_DIR, Wallpaper.ImageURL));
            }
        }

        private void WallpaperSaved(string path)
        {
            btnSave.Text = "Saved";
            btnSave.Enabled = false;
            Saving = false;
        }

        private void SetWallpaper(string path)
        {
            WallpaperSetter.Set(Path.GetFullPath(path), WallpaperSetter.WallpaperStyle.Fill);
            Saving = false;
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            if (File.Exists(Path.Combine(WALLPAPER_DIR, Wallpaper.ImageURL)))
            {
                SetWallpaper(Path.Combine(WALLPAPER_DIR, Wallpaper.ImageURL));
            }
            else if (!Saving)
            {
                if (!Directory.Exists(WALLPAPER_DIR))
                {
                    Directory.CreateDirectory(WALLPAPER_DIR);
                }
                DownloadManager.CreateRequest(DownloadManager.DownloadType.Wallpaper, SetWallpaper, $"{BASE_THUMB_URL}{Wallpaper.ImageURL}", Path.Combine(WALLPAPER_DIR, Wallpaper.ImageURL));
            }
        }
    }
}
