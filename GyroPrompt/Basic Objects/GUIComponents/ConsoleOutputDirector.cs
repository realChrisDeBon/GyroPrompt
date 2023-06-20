using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class ConsoleOutputDirector
    {
        public List<GUI_Button> GUIButtonsToAdd = new List<GUI_Button>();
        public List<GUI_textfield> GUITextFieldsToAdd = new List<GUI_textfield>();
        public bool runningPermision = true;
        public Window mainWindow;
        public void InitializeGUIWindow(string windowTitle = "GUIMode", int x_ = 0, int y_ = 0)
        {
                Application.Init();
                var top = Application.Top;

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
            foreach (GUI_Button item in GUIButtonsToAdd)
            {
                mainWindow.Add(item.newButton);
            }
            foreach (GUI_textfield item_ in GUITextFieldsToAdd)
            {
                mainWindow.Add(item_.textView);
            }
            try
            {
                // Execute application
                top.Add(mainWindow);
                Application.Run();
            }
            catch
            {
                // Expect error to be thrown when Application.Shutdown() and Application.RequestStop() execute from parser
            }
        }

    }
}
