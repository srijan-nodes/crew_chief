namespace CrewChiefV4.UserInterface.TopicWindows
{
    partial class TopicWindowFuel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TopicWindowFuel));
            this.labelFuelLevel = new System.Windows.Forms.Label();
            this.textBoxFuelLevel = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // labelFuelLevel
            // 
            this.labelFuelLevel.AutoSize = true;
            this.labelFuelLevel.Location = new System.Drawing.Point(12, 15);
            this.labelFuelLevel.Name = "labelFuelLevel";
            this.labelFuelLevel.Size = new System.Drawing.Size(81, 20);
            this.labelFuelLevel.TabIndex = 0;
            this.labelFuelLevel.Text = "Fuel Level";
            // 
            // textBoxFuelLevel
            // 
            this.textBoxFuelLevel.Location = new System.Drawing.Point(95, 12);
            this.textBoxFuelLevel.Name = "textBoxFuelLevel";
            this.textBoxFuelLevel.ReadOnly = true;
            this.textBoxFuelLevel.Size = new System.Drawing.Size(100, 26);
            this.textBoxFuelLevel.TabIndex = 1;
            // 
            // TopicWindowFuel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 61);
            this.Controls.Add(this.textBoxFuelLevel);
            this.Controls.Add(this.labelFuelLevel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TopicWindowFuel";
            this.Text = "Fuel";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelFuelLevel;
        private System.Windows.Forms.TextBox textBoxFuelLevel;
    }
}
