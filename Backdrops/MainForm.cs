using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Backdrops
{
    public partial class MainForm : Form
    {
        const string BASE_API_URL = "http://www.backdrops.io/walls/api.php?";
        const string CATEGORIES_URL = "page=1";
        const string CATEGORY_CONTENTS_URL = "wallpaper_cat_id=";
        const string SOCIAL_URL = "task=social_wallpaper&sort=";
        const string EXPLORE_URL = "task=recent_wallpaper";
        const string BASE_IMAGE_URL = "http://www.backdrops.io/walls/upload/";

        const int PAGE_SIZE = 30;

        DownloadManager DownloadManager = new DownloadManager();

        List<Wallpaper> ExploreWallpapers;
        List<Wallpaper> SocialWallpapers;

        public MainForm()
        {
            InitializeComponent();
        }

        #region "Invoke Control Delegates"
        private delegate void ComboBoxClearDelegate(ComboBox ComboBox);
        private void ComboBoxClear(ComboBox ComboBox)
        {
            if (ComboBox.InvokeRequired)
            {
                var d = new ComboBoxClearDelegate(ComboBoxClear);
                this.Invoke(d, ComboBox);
            }
            else
            {
                ComboBox.Items.Clear();
            }
        }

        private delegate void ComboBoxAddRangeDelegate(ComboBox ComboBox, object[] Objects);
        private void ComboBoxAddRange(ComboBox ComboBox, object[] Objects)
        {
            if (ComboBox.InvokeRequired)
            {
                var d = new ComboBoxAddRangeDelegate(ComboBoxAddRange);
                this.Invoke(d, ComboBox, Objects);
            }
            else
            {
                ComboBox.Items.AddRange(Objects);
            }
        }

        private delegate void ComboBoxSelectedIndexDelegate(ComboBox ComboBox, int Index);
        private void ComboBoxSelectedIndex(ComboBox ComboBox, int Index)
        {
            if (ComboBox.InvokeRequired)
            {
                var d = new ComboBoxSelectedIndexDelegate(ComboBoxSelectedIndex);
                this.Invoke(d, ComboBox, Index);
            }
            else
            {
                ComboBox.SelectedIndex = Index;
            }
        }

        private delegate void PanelClearDelegate(Panel Panel);
        private void PanelClear(Panel Panel)
        {
            if (Panel.InvokeRequired)
            {
                var d = new PanelClearDelegate(PanelClear);
                this.Invoke(d, Panel);
            }
            else
            {
                Panel.Controls.Clear();
            }
        }

        private delegate void PanelAddControlDelegate(Panel Panel, Control Control);
        private void PanelAddControl(Panel Panel, Control Control)
        {
            if (Panel.InvokeRequired)
            {
                var d = new PanelAddControlDelegate(PanelAddControl);
                this.Invoke(d, Panel, Control);
            }
            else
            {
                Panel.Controls.Add(Control);
            }
        }

        private delegate Panel.ControlCollection PanelGetControlsDelegate(Panel Panel);
        private Panel.ControlCollection PanelGetControls(Panel Panel)
        {
            if (Panel.InvokeRequired)
            {
                var d = new PanelGetControlsDelegate(PanelGetControls);
                return (Panel.ControlCollection)this.Invoke(d, Panel);
            }
            else
            {
                return Panel.Controls;
            }
        }

        private delegate void DisposeControlDelegate(Control Control);
        private void DisposeControl(Control Control)
        {
            if (Control.InvokeRequired)
            {
                var d = new DisposeControlDelegate(DisposeControl);
                this.Invoke(d, Control);
            }
            else
            {
                Control.Dispose();
            }
        }

        private delegate void SetControlWidthDelegate(Control Control, int Width);
        private void SetControlWidth(Control Control, int Width)
        {
            if (Control.InvokeRequired)
            {
                var d = new SetControlWidthDelegate(SetControlWidth);
                this.Invoke(d, Control, Width);
            }
            else
            {
                Control.Width = Width;
            }
        }
        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {
            DownloadManager.CreateRequest(DownloadManager.DownloadType.API, LoadCategories, $"{BASE_API_URL}{CATEGORIES_URL}");
            DownloadManager.CreateRequest(DownloadManager.DownloadType.API, LoadExplore, $"{BASE_API_URL}{EXPLORE_URL}");
            cmbSocialSort.Items.Clear();
            cmbSocialSort.Items.Add("Most recent");
            cmbSocialSort.Items.Add("Most downloaded");
            cmbSocialSort.SelectedIndex = 0;
        }

        private void LoadCategories(string JSON)
        {
                var parsedJson = JObject.Parse(JSON);
                var categoryList = new List<Category>();
                var categories = parsedJson.GetValue("entertainment");
                categoryList.AddRange(categories.Select(p => new Category
                {
                    CategoryID = (int)p["cid"],
                    CategoryName = (string)p["category_name"],
                    CategoryIconUrl = (string)p["category_icon"]
                }));

                ComboBoxClear(cmbCategory);
                ComboBoxAddRange(cmbCategory, categoryList.ToArray());
                if (cmbCategory.Items.Count == 0)
                {
                    MessageBox.Show("Could not load categories.");
                }
                else
                {
                    ComboBoxSelectedIndex(cmbCategory, 0);
                }
        }

        private void LoadExplore(string JSON)
        {
            var parsedJson = JObject.Parse(JSON);
            ExploreWallpapers = new List<Wallpaper>();
            var categories = parsedJson.GetValue("entertainment");
            ExploreWallpapers.AddRange(categories.Select(p => new Wallpaper
            {
                ID = (int)p["id"],
                CategoryID = (int)p["cid"],
                CategoryName = (string)p["category_name"],
                ImageURL = (string)p["wallpaper_image"],
                ThumbURL = (string)p["wallpaper_thumb"],
                Title = (string)p["title"],
                Description = (string)p["description"],
                Tags = (string)p["tag"],
                Size = (string)p["size"],
                User = (string)p["user"],
                CopyrightName = (string)p["copyright_name"],
                CopyrightURL = (string)p["copyright_link"],
                Width = (int)p["width"],
                Height = (int)p["height"],
                DownloadCount = (int)p["download_count"],
                Rating = (float)p["wallpaper_rate_avg"]
            }));

            if (ExploreWallpapers.Count <= PAGE_SIZE)
            {
                foreach (Control control in PanelGetControls(pnlExploreContent))
                {
                    DisposeControl(control);
                }
                PanelClear(pnlExploreContent);

                foreach (var item in ExploreWallpapers)
                {
                    var control = new WallpaperControl(item, DownloadManager);
                    PanelAddControl(pnlExploreContent, control);
                }
            }
            else
            {
                LoadExplorePage(1);
            }
        }

        private void LoadExplorePage(int PageNumber)
        {
            var PageCount = (int)Math.Ceiling(ExploreWallpapers.Count / (float)PAGE_SIZE);
            if (PageNumber <= PageCount)
            {
                lblExplorePageCount.Text = $"{PageNumber}/{PageCount}";
                btnExploreNext.Left = lblExplorePageCount.Left + TextRenderer.MeasureText(lblExplorePageCount.Text, lblExplorePageCount.Font).Width + pnlExploreTopRight.Padding.Left;
                SetControlWidth(pnlExploreTopRight, btnExploreNext.Left + btnExploreNext.Width + pnlExploreTopRight.Padding.Horizontal);

                foreach (Control control in PanelGetControls(pnlExploreContent))
                {
                    DisposeControl(control);
                }
                PanelClear(pnlExploreContent);

                var minimumIndex = PAGE_SIZE * (PageNumber - 1);
                // Includes one past index so that the loop is a fraction of a microsecond faster.
                var maximumIndex = PageNumber == PageCount ? ExploreWallpapers.Count : PAGE_SIZE * PageNumber;

                for (int i = minimumIndex; i < maximumIndex; i++)
                {
                    var control = new WallpaperControl(ExploreWallpapers[i], DownloadManager);
                    PanelAddControl(pnlExploreContent, control);
                }

                if (PageNumber == 1)
                {
                    btnExplorePrevious.Enabled = false;
                }
                else
                {
                    btnExplorePrevious.Enabled = true;
                }

                if (PageNumber == PageCount)
                {
                    btnExploreNext.Enabled = false;
                }
                else
                {
                    btnExploreNext.Enabled = true;
                }

                lblExplorePageCount.Tag = PageNumber;
            }
        }

        private void cmbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCategory.SelectedItem != null && cmbCategory.SelectedItem is Category)
            {
                var categoryID = (cmbCategory.SelectedItem as Category).CategoryID;
                DownloadManager.CreateRequest(DownloadManager.DownloadType.API, LoadCategory, $"{BASE_API_URL}{CATEGORY_CONTENTS_URL}{categoryID}");
            }
        }

        private void LoadCategory(string JSON)
        {
            var parsedJson = JObject.Parse(JSON);
            var wallpaperList = new List<Wallpaper>();
            var categories = parsedJson.GetValue("entertainment");
            wallpaperList.AddRange(categories.Select(p => new Wallpaper
            {
                ID = (int)p["id"],
                CategoryID = (int)p["cid"],
                CategoryName = (string)p["category_name"],
                ImageURL = (string)p["wallpaper_image"],
                ThumbURL = (string)p["wallpaper_thumb"],
                Title = (string)p["title"],
                Description = (string)p["description"],
                Tags = (string)p["tag"],
                Size = (string)p["size"],
                User = (string)p["user"],
                CopyrightName = (string)p["copyright_name"],
                CopyrightURL = (string)p["copyright_link"],
                Width = (int)p["width"],
                Height = (int)p["height"],
                DownloadCount = (int)p["download_count"],
                Rating = (float)p["wallpaper_rate_avg"]
            }));

            foreach (Control control in PanelGetControls(pnlCategoriesContent))
            {
                DisposeControl(control);
            }
            PanelClear(pnlCategoriesContent);

            foreach (var item in wallpaperList)
            {
                var control = new WallpaperControl(item, DownloadManager);
                PanelAddControl(pnlCategoriesContent, control);
            }
        }

        private void LoadSocialPage(int PageNumber)
        {
            var PageCount = (int)Math.Ceiling(SocialWallpapers.Count / (float)PAGE_SIZE);
            if (PageNumber <= PageCount)
            {
                lblSocialPageCount.Text = $"{PageNumber}/{PageCount}";
                btnSocialNext.Left = lblSocialPageCount.Left + TextRenderer.MeasureText(lblSocialPageCount.Text, lblSocialPageCount.Font).Width + pnlSocialTopRight.Padding.Left;
                SetControlWidth(pnlSocialTopRight, btnSocialNext.Left + btnSocialNext.Width + pnlSocialTopRight.Padding.Horizontal);

                foreach (Control control in PanelGetControls(pnlSocialContent))
                {
                    DisposeControl(control);
                }
                PanelClear(pnlSocialContent);

                var minimumIndex = PAGE_SIZE * (PageNumber - 1);
                // Includes one past index so that the loop is a fraction of a microsecond faster.
                var maximumIndex = PageNumber == PageCount ? SocialWallpapers.Count : PAGE_SIZE * PageNumber;

                for (int i = minimumIndex; i < maximumIndex; i++)
                {
                    var control = new WallpaperControl(SocialWallpapers[i], DownloadManager);
                    PanelAddControl(pnlSocialContent, control);
                }

                if (PageNumber == 1)
                {
                    btnSocialPrevious.Enabled = false;
                }
                else
                {
                    btnSocialPrevious.Enabled = true;
                }

                if (PageNumber == PageCount)
                {
                    btnSocialNext.Enabled = false;
                }
                else
                {
                    btnSocialNext.Enabled = true;
                }

                lblSocialPageCount.Tag = PageNumber;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DownloadManager.Stop();
        }

        private void cmbSocialSort_SelectedIndexChanged(object sender, EventArgs e)
        {
            DownloadManager.CreateRequest(DownloadManager.DownloadType.API, LoadSocial, $"{BASE_API_URL}{SOCIAL_URL}{(cmbSocialSort.SelectedText == "Most recent" ? 0 : 1)}");
        }

        private void LoadSocial(string JSON)
        {
            var parsedJson = JObject.Parse(JSON);
            SocialWallpapers = new List<Wallpaper>();
            var categories = parsedJson.GetValue("entertainment");
            SocialWallpapers.AddRange(categories.Select(p => new Wallpaper
            {
                ID = (int)p["id"],
                CategoryID = (int)p["cid"],
                CategoryName = (string)p["category_name"],
                ImageURL = (string)p["wallpaper_image"],
                ThumbURL = (string)p["wallpaper_thumb"],
                Title = (string)p["title"],
                Description = (string)p["description"],
                Tags = (string)p["tag"],
                Size = (string)p["size"],
                User = (string)p["user"],
                CopyrightName = (string)p["copyright_name"],
                CopyrightURL = (string)p["copyright_link"],
                Width = (int)p["width"],
                Height = (int)p["height"],
                DownloadCount = (int)p["download_count"],
                Rating = (float)p["wallpaper_rate_avg"]
            }));

            if (SocialWallpapers.Count <= PAGE_SIZE)
            {
                foreach (Control control in PanelGetControls(pnlSocialContent))
                {
                    DisposeControl(control);
                }
                PanelClear(pnlSocialContent);

                foreach (var item in SocialWallpapers)
                {
                    var control = new WallpaperControl(item, DownloadManager);
                    PanelAddControl(pnlSocialContent, control);
                }
            }
            else
            {
                LoadSocialPage(1);
            }
        }

        private void btnExplorePrevious_Click(object sender, EventArgs e)
        {
            var oldPageNumber = (int)lblExplorePageCount.Tag;
            LoadExplorePage(oldPageNumber - 1);
        }

        private void btnExploreNext_Click(object sender, EventArgs e)
        {
            var oldPageNumber = (int)lblExplorePageCount.Tag;
            LoadExplorePage(oldPageNumber + 1);
        }

        private void btnSocialPrevious_Click(object sender, EventArgs e)
        {
            var oldPageNumber = (int)lblSocialPageCount.Tag;
            LoadSocialPage(oldPageNumber - 1);
        }

        private void btnSocialNext_Click(object sender, EventArgs e)
        {
            var oldPageNumber = (int)lblSocialPageCount.Tag;
            LoadSocialPage(oldPageNumber + 1);
        }
    }
}
