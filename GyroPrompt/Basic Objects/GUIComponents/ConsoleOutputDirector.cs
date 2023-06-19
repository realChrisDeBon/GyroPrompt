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
        Window mainWindow;
        public void InitializeGUIWindow(string windowTitle = "GUIMode", int x_ = 0, int y_ = 0)
        {
                Application.Init();

                // Set the default color scheme
                mainWindow = new Window($"{windowTitle}")
                {
                    X = x_,
                    Y = y_,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
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
              

                // Execute application
                var top = Application.Top;
                top.Add(mainWindow);
                Application.Run();

            
        }

        public void Terminate()
        {
            Application.Current.EndInit(); // This is not working for some raisin and I need to figure out why.
        }
    }
}
