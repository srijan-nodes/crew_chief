import os
import re

base_dir = r"c:\Users\dsrij\codes\crew_chief\CrewChiefV4\CrewChiefV4"
menu_path = os.path.join(base_dir, "UserInterface", "MainWindowMenu.cs")

with open(menu_path, 'r', encoding='utf-8') as f:
    menu_code = f.read()

# Create menu item
setup_item = """            var topicSetupAdvisorToolStripMenuItem = new ToolStripMenuItem("Setup Advisor") { CheckOnClick = true };"""
modern_ui = """            var modernUIToolStripMenuItem = new ToolStripMenuItem("Modern UI (Beta)");
            modernUIToolStripMenuItem.Click += (s, e) => { new CrewChiefV4.UserInterface.ModernUIPreview().Show(); };
            
            var topicSetupAdvisorToolStripMenuItem = new ToolStripMenuItem("Setup Advisor") { CheckOnClick = true };"""
menu_code = menu_code.replace(setup_item, modern_ui)

# Add to MainMenuStrip Items array
items_array = """                topicWindowsToolStripMenuItem,
                problemsMenu.GetMenuItem(),
                helpToolStripMenuItem
            });"""
items_array_new = """                topicWindowsToolStripMenuItem,
                modernUIToolStripMenuItem,
                problemsMenu.GetMenuItem(),
                helpToolStripMenuItem
            });"""
menu_code = menu_code.replace(items_array, items_array_new)

with open(menu_path, 'w', encoding='utf-8') as f:
    f.write(menu_code)
