using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace CrewChiefV4.UserInterface
{
    public class ModernUIPreview : Form
    {
        private WebView2 webView;

        public ModernUIPreview()
        {
            this.Text = "Crew Chief V4 - Modern UI Preview";
            this.Width = 1024;
            this.Height = 768;
            this.StartPosition = FormStartPosition.CenterScreen;

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            string artifactPath = @"C:\Users\dsrij\.gemini\antigravity\brain\b9a137b5-5fb3-4cdf-bd54-3d63b64e9196\modern_ui_preview.html";
            if (File.Exists(artifactPath))
            {
                webView.CoreWebView2.NavigateToString(File.ReadAllText(artifactPath));
            }
            else
            {
                webView.CoreWebView2.NavigateToString("<h1>Modern UI Preview not found at " + artifactPath + "</h1>");
            }
        }
    }
}
