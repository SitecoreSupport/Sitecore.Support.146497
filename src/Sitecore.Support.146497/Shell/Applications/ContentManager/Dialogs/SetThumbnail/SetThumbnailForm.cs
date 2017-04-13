namespace Sitecore.Support.Shell.Applications.ContentManager.Dialogs.SetThumbnail
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.ImageLib;
    using Sitecore.IO;
    using Sitecore.Links;
    using Sitecore.Shell.Applications.ContentManager.Dialogs.SetThumbnail;
    using Sitecore.Shell.Framework;
    using Sitecore.Sites;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Drawing;
    using System.IO;
    using System.Runtime.CompilerServices;

    public class SetThumbnailForm : DialogForm
    {
        protected Border SaveAs;

        private void Clear()
        {
            SheerResponse.Eval("scUpdateThumbnails('/sitecore/images/blank.gif','/sitecore/images/blank.gif','/sitecore/images/blank.gif','/sitecore/images/blank.gif','/sitecore/images/blank.gif','/sitecore/images/blank.gif')");
            SheerResponse.Eval("rubberband.Hide()");
            this.Screenshot = string.Empty;
        }

        protected void Crop()
        {
            if (!string.IsNullOrEmpty(this.Screenshot))
            {
                double zoom = ((double)MainUtil.GetInt(WebUtil.GetFormValue("Zoom"), 100)) / 100.0;
                char[] separator = new char[] { ',' };
                string[] textArray1 = WebUtil.GetFormValue("Cropping").Split(separator);
                int @int = MainUtil.GetInt(textArray1[0], 0);
                int y = MainUtil.GetInt(textArray1[1], 0);
                this.GenerateThumbnails(this.Screenshot, zoom, new Rectangle(@int, y, @int + 0x80, y + 0x80));
                string str = this.GetFileName(this.Screenshot, 0x80) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                string str2 = this.GetFileName(this.Screenshot, 0x30) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                string str3 = this.GetFileName(this.Screenshot, 0x20) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                string str4 = this.GetFileName(this.Screenshot, 0x18) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                string str5 = this.GetFileName(this.Screenshot, 0x10) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                SheerResponse.Eval("scUpdateThumbnails(null,'" + str + "','" + str2 + "','" + str3 + "','" + str4 + "','" + str5 + "')");
            }
        }

        protected void Download()
        {
            if (string.IsNullOrEmpty(this.Screenshot))
            {
                SheerResponse.Alert("Please generate a screenshot first.", new string[0]);
            }
            else
            {
                Files.Download(this.Screenshot);
            }
        }

        private string GenerateThumbnail(string url)
        {
            Assert.ArgumentNotNull(url, "url");
            string filename = TempFolder.GetFilename("thumbnail.png");
            HtmlCapture capture1 = new HtmlCapture
            {
                Url = url,
                FileName = filename
            };
            if (capture1.Capture())
            {
                return filename;
            }
            return null;
        }

        private void GenerateThumbnail(Bitmap source, string filename, int size)
        {
            ResizeOptions options = new ResizeOptions
            {
                AllowStretch = false,
                BackgroundColor = Color.White,
                Format = source.RawFormat,
                Size = new Size(size, size)
            };
            using (Bitmap bitmap = new Resizer().Resize(source, options, source.RawFormat))
            {
                bitmap.Save(FileUtil.MapPath(this.GetFileName(filename, size)));
            }
        }

        private void GenerateThumbnails(string filename, double zoom, Rectangle crop)
        {
            using (Bitmap bitmap = System.Drawing.Image.FromFile(FileUtil.MapPath(filename)) as Bitmap)
            {
                if (bitmap != null)
                {
                    ResizeOptions options = new ResizeOptions
                    {
                        AllowStretch = true,
                        BackgroundColor = Color.White,
                        Format = bitmap.RawFormat,
                        Size = new Size((int)(bitmap.Width * zoom), (int)(bitmap.Height * zoom))
                    };
                    using (Bitmap bitmap2 = new Resizer().Resize(bitmap, options, bitmap.RawFormat))
                    {
                        using (Bitmap bitmap3 = new Bitmap(bitmap2, 0x80, 0x80))
                        {
                            using (Graphics graphics = Graphics.FromImage(bitmap3))
                            {
                                graphics.Clear(Color.White);
                                graphics.DrawImage(bitmap2, 0, 0, crop, GraphicsUnit.Pixel);
                            }
                            bitmap3.Save(FileUtil.MapPath(this.GetFileName(filename, 0x80)));
                            this.GenerateThumbnail(bitmap3, filename, 0x30);
                            this.GenerateThumbnail(bitmap3, filename, 0x20);
                            this.GenerateThumbnail(bitmap3, filename, 0x18);
                            this.GenerateThumbnail(bitmap3, filename, 0x10);
                        }
                    }
                }
            }
        }

        private string GetFileName(string filename, int size)
        {
            string str = FileUtil.NormalizeWebPath(Path.GetDirectoryName(filename));
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            string extension = Path.GetExtension(filename);
            return string.Format("{0}/{1}{2}x{2}{3}", new object[] { str, fileNameWithoutExtension, size, extension });
        }

        private UrlString GetItemUrl()
        {
            Assert.IsNotNull(UIUtil.GetItemFromQueryString(Context.ContentDatabase), typeof(Item));
            string path = this.Presentation.Value;
            Item item = UIUtil.GetItemFromQueryString(Context.ContentDatabase).Database.GetItem(path);
            if (item == null)
            {
                return null;
            }
            SiteContext site = Factory.GetSite(Sitecore.Configuration.Settings.Preview.DefaultSite);
            if (site == null)
            {
                return null;
            }
            return this.GetItemUrl(item, site);
        }

        private UrlString GetItemUrl(Item item, SiteContext site)
        {
            Assert.ArgumentNotNull(item, "item");
            UrlOptions defaultOptions = UrlOptions.DefaultOptions;
            defaultOptions.Site = site;
            string itemUrl = LinkManager.GetItemUrl(item, defaultOptions);
            UrlString str2 = new UrlString(WebUtil.GetServerUrl() + itemUrl)
            {
                ["sc_site"] = site.Name,
                ["sc_mode"] = "normal",
                ["sc_duration"] = "temporary"
            };
            ID id = ID.Parse(this.Device.Value);
            if (id != ItemIDs.DevicesRoot)
            {
                str2["sc_device"] = id.ToString();
            }
            return str2;
        }

        private UrlString GetUrl()
        {
            if (WebUtil.GetFormValue("UrlKind") == "1")
            {
                return this.GetUrlUrl();
            }
            return this.GetItemUrl();
        }

        private UrlString GetUrlUrl()
        {
            string str = this.Url.Value;
            Registry.SetString("/Current_User/ContentEditor/ThumbnailUrl", str);
            if (str.IndexOf("://", StringComparison.InvariantCulture) < 0)
            {
                str = "http://" + str;
            }
            return new UrlString(str);
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Database.GetDatabase(WebUtil.GetQueryString("sc_content", Context.ContentDatabase.Name)));
                Assert.IsNotNull(itemFromQueryString, typeof(Item));
                string queryString = WebUtil.GetQueryString("presentationId");
                this.Presentation.Value = string.IsNullOrEmpty(queryString) ? itemFromQueryString.ID.ToString() : queryString;
                this.Url.Value = Registry.GetString("/Current_User/ContentEditor/ThumbnailUrl", string.Empty);
                this.SaveAs.ToolTip = Translate.Text("Save your screenshot");
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if (string.IsNullOrEmpty(this.Screenshot))
            {
                SheerResponse.Alert("Please generate a screenshot first.", new string[0]);
            }
            else
            {
                SheerResponse.SetDialogValue(this.GetFileName(this.Screenshot, 0x80));
                base.OnOK(sender, args);
            }
        }

        protected void PreviewGenerate()
        {
            UrlString url = this.GetUrl();
            if (url == null)
            {
                this.Clear();
            }
            else
            {
                string filename = this.GenerateThumbnail(url.ToString());
                if (filename == null)
                {
                    this.Clear();
                }
                else
                {
                    double zoom = ((double)MainUtil.GetInt(WebUtil.GetFormValue("Zoom"), 100)) / 100.0;
                    char[] separator = new char[] { ',' };
                    string[] textArray1 = WebUtil.GetFormValue("Cropping").Split(separator);
                    int @int = MainUtil.GetInt(textArray1[0], 0);
                    int y = MainUtil.GetInt(textArray1[1], 0);
                    this.GenerateThumbnails(filename, zoom, new Rectangle(@int, y, @int + 0x80, y + 0x80));
                    this.Screenshot = filename;
                    this.Preview.Src = filename;
                    string str3 = this.GetFileName(filename, 0x80) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                    string str4 = this.GetFileName(filename, 0x30) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                    string str5 = this.GetFileName(filename, 0x20) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                    string str6 = this.GetFileName(filename, 0x18) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                    string str7 = this.GetFileName(filename, 0x10) + "?dt=" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssffff");
                    SheerResponse.Eval("scUpdateThumbnails('" + filename + "','" + str3 + "','" + str4 + "','" + str5 + "','" + str6 + "','" + str7 + "')");
                    SheerResponse.Eval("rubberband.Show()");
                }
            }
        }

        protected TreePicker Device { get; set; }

        protected TreePicker Presentation { get; set; }

        public ThemedImage Preview { get; set; }

        protected string Screenshot
        {
            get { return ((base.ServerProperties["Screenshot"] as string) ?? string.Empty); }
            set { base.ServerProperties["Screenshot"] = value; }
        }


        protected Edit Url { get; set; }
    }
}
