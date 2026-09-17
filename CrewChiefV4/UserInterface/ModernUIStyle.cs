using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CrewChiefV4.UserInterface
{
    public static class ModernUIStyle
    {
        public static void Apply(Form form)
        {
            Color bgColor = Color.FromArgb(13, 17, 23);
            Color cardColor = Color.FromArgb(33, 38, 45);
            Color fgColor = Color.FromArgb(230, 237, 243);
            Color accentColor = Color.FromArgb(16, 185, 129); // emerald-500
            
            form.BackColor = bgColor;
            form.ForeColor = fgColor;
            form.Padding = new Padding(10);
            
            Queue<Control> q = new Queue<Control>();
            foreach (Control c in form.Controls) q.Enqueue(c);
            
            while (q.Count > 0)
            {
                Control c = q.Dequeue();
                
                if (c is Button b)
                {
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderSize = 0;
                    b.BackColor = cardColor;
                    b.ForeColor = fgColor;
                    b.Font = new Font(b.Font.FontFamily, 9f, FontStyle.Bold);
                    b.Cursor = Cursors.Hand;
                    b.Margin = new Padding(5);
                }
                else if (c is Panel p)
                {
                    p.BackColor = bgColor;
                }
                else if (c is GroupBox g)
                {
                    g.BackColor = cardColor;
                    g.ForeColor = fgColor;
                    g.FlatStyle = FlatStyle.Flat;
                }
                else if (c is Label l)
                {
                    l.ForeColor = fgColor;
                    l.BackColor = Color.Transparent;
                }
                else if (c is TextBox t)
                {
                    t.BackColor = bgColor;
                    t.ForeColor = accentColor;
                    t.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is ComboBox cb)
                {
                    cb.BackColor = bgColor;
                    cb.ForeColor = fgColor;
                    cb.FlatStyle = FlatStyle.Flat;
                }
                else if (c is ListBox lb)
                {
                    lb.BackColor = bgColor;
                    lb.ForeColor = fgColor;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                }
                
                foreach (Control child in c.Controls)
                {
                    q.Enqueue(child);
                }
            }
            
            // Adjust layout spacing
            if (form is MainWindow mw)
            {
                if (mw.tableLayoutPanelMain != null) {
                    mw.tableLayoutPanelMain.Dock = DockStyle.Fill;
                    mw.tableLayoutPanelMain.Padding = new Padding(10);
                }
            }
        }
    }
}
