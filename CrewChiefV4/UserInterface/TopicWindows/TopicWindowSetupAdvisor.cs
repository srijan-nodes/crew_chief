using System;
using System.Drawing;
using System.Windows.Forms;
using CrewChiefV4.HeadlessSimulation;

namespace CrewChiefV4.UserInterface.TopicWindows
{
    public partial class TopicWindowSetupAdvisor : Form
    {
        private static TopicWindowSetupAdvisor _instance;
        private Label lblDiagnosis;
        private Label lblRecommendation;

        public TopicWindowSetupAdvisor()
        {
            lock (_lock)
            {
                _instance = this;
            }
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblDiagnosis = new System.Windows.Forms.Label();
            this.lblRecommendation = new System.Windows.Forms.Label();
            this.SuspendLayout();
            
            // lblDiagnosis
            this.lblDiagnosis.AutoSize = true;
            this.lblDiagnosis.Location = new System.Drawing.Point(12, 20);
            this.lblDiagnosis.Name = "lblDiagnosis";
            this.lblDiagnosis.Size = new System.Drawing.Size(200, 13);
            this.lblDiagnosis.TabIndex = 0;
            this.lblDiagnosis.Text = "Waiting for session...";
            
            // lblRecommendation
            this.lblRecommendation.AutoSize = true;
            this.lblRecommendation.Location = new System.Drawing.Point(12, 60);
            this.lblRecommendation.Name = "lblRecommendation";
            this.lblRecommendation.Size = new System.Drawing.Size(200, 13);
            this.lblRecommendation.TabIndex = 1;
            this.lblRecommendation.Text = "";
            
            // TopicWindowSetupAdvisor
            this.ClientSize = new System.Drawing.Size(400, 120);
            this.Controls.Add(this.lblRecommendation);
            this.Controls.Add(this.lblDiagnosis);
            this.Name = "TopicWindowSetupAdvisor";
            this.Text = "Setup Advisor";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            lock (_lock)
            {
                _instance = null;
            }
            base.OnFormClosed(e);
        }

        private static readonly object _lock = new object();
        public static void UpdateDiagnosis(GripLossAnalyzer.GripDiagnosis diag)
        {
            if (diag == null) return;
            lock (_lock)
            {
                if (_instance == null || _instance.IsDisposed || !_instance.Visible) return;
                var localInstance = _instance;
                localInstance.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        if (localInstance.IsDisposed) return;
                        localInstance.lblDiagnosis.Text = string.IsNullOrEmpty(diag.Summary) ? "Looks good" : diag.Summary;
                    }
                    catch (ObjectDisposedException) { }
                }));
            }
        }

        public static void UpdateRecommendation(string text)
        {
            lock (_lock)
            {
                if (_instance == null || _instance.IsDisposed || !_instance.Visible) return;
                
                var localInstance = _instance;
                localInstance.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        if (localInstance.IsDisposed) return;
                        localInstance.lblRecommendation.Text = text;
                    }
                    catch (ObjectDisposedException) { }
                }));
            }
        }
    }
}
