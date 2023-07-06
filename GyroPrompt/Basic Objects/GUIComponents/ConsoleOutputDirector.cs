using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static System.Net.Mime.MediaTypeNames;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class ConsoleOutputDirector
    {
        public List<GUI_Button> GUIButtonsToAdd = new List<GUI_Button>();
        public List<GUI_textfield> GUITextFieldsToAdd = new List<GUI_textfield>();
        public List<GUI_Menubar> GUIMenuBarsToAdd = new List<GUI_Menubar>();
        public List<GUI_Label> GUILabelsToAdd = new List<GUI_Label>();
        public List<GUI_Checkbox> GUICheckboxToAdd = new List<GUI_Checkbox>();
        public bool runningPermision = true;
        public Window mainWindow;
        public SaveDialog saveDialog;

        public void InitializeGUIWindow(string windowTitle = "GUIMode", int x_ = 0, int y_ = 0)
        {
            Terminal.Gui.Application.Init();
            var top = Terminal.Gui.Application.Top;

            // Set the default color scheme
            mainWindow = new Window($"{windowTitle}")
            {
                X = x_,
                Y = y_,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Border = new Border()
                {
                    BorderStyle = BorderStyle.Single,
                    DrawMarginFrame = true,
                    Padding = new Thickness(1),
                    BorderBrush = Color.White
                }
            };
            mainWindow.ColorScheme = new ColorScheme()
            {
                Normal = Terminal.Gui.Attribute.Make(Color.White, Color.Black),
                Focus = Terminal.Gui.Attribute.Make(Color.White, Color.Black),
                HotNormal = Terminal.Gui.Attribute.Make(Color.White, Color.Black),
                HotFocus = Terminal.Gui.Attribute.Make(Color.White, Color.Black)
            };
            // Take every GUI object within GUIItemsToAdd and add it to mainWindow
            foreach (GUI_Menubar item in GUIMenuBarsToAdd)
            {
                top.Add(item.menuBar);
            }
            foreach (GUI_textfield item_ in GUITextFieldsToAdd)
            {
                mainWindow.Add(item_.textView);
            }
            foreach (GUI_Label item_ in GUILabelsToAdd)
            {
                mainWindow.Add(item_.newlabel);
            }
            foreach (GUI_Button item in GUIButtonsToAdd)
            {
                mainWindow.Add(item.newButton);
            }
            foreach (GUI_Checkbox item in GUICheckboxToAdd)
            {
                mainWindow.Add(item.newCheckbox);
            }
            try
            {
                // Execute application
                top.Add(mainWindow);
                Terminal.Gui.Application.Run();
            }
            catch
            {
                // Expect error to be thrown when Application.Shutdown() and Application.RequestStop() execute from parser
            }
        }
        
        public int yesno_msgbox(string title, string msg)
        {
            int result = MessageBox.Query(title, msg, "YES", "NO");
            return result;
        }
        public void ok_msgbox(string title, string msg)
        {
            MessageBox.Query(title, msg, "Ok");
        }

        public string showsaveDialog()
        {
            saveDialog = new SaveDialog("Save File As", "Select a location to save the file");

            Terminal.Gui.Application.Run(saveDialog);
            MessageBox.Query("result", saveDialog.FilePath.ToString(), "OK");
            
            if (!string.IsNullOrEmpty(saveDialog.FilePath.ToString()))
            {
                try
                {
                    return saveDialog.FilePath.ToString();
                }
                catch {
                    return null;
                }
            } else
            {
                return null;
            }
        }
    }
}

