using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Basic_Objects.Variables;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static System.Net.Mime.MediaTypeNames;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class ConsoleOutputDirector
    {
        public readonly Dictionary<GUIObjectType, bool> objectCanRepos = new Dictionary<GUIObjectType, bool>()
        {
                            { GUIObjectType.Button, true},
                            { GUIObjectType.Textfield, true},
                            { GUIObjectType.Checkbox, true},
                            { GUIObjectType.Label, true},
                            { GUIObjectType.TabContainer, true }
        };
        public readonly Dictionary<GUIObjectType, bool> objectHasTextAccess = new Dictionary<GUIObjectType, bool>()
        {
                            { GUIObjectType.Button, true},
                            { GUIObjectType.Textfield, true},
                            { GUIObjectType.Checkbox, true},
                            { GUIObjectType.Label, true},
        };
        public readonly Dictionary<GUIObjectType, bool> objectIsContainer = new Dictionary<GUIObjectType, bool>()
        {
                            {GUIObjectType.Tab , true}
        };

         public List<GUI_Button> GUIButtonsToAdd = new List<GUI_Button>();
         public List<GUI_textfield> GUITextFieldsToAdd = new List<GUI_textfield>();
         public List<GUI_Menubar> GUIMenuBarsToAdd = new List<GUI_Menubar>();
         public List<GUI_Label> GUILabelsToAdd = new List<GUI_Label>();
         public List<GUI_Checkbox> GUICheckboxToAdd = new List<GUI_Checkbox>();


        public ArrayList viewobjects = new ArrayList();
        public bool runningPermision = true;
        public const string main = "main";
        public Window mainWindow;
        public SaveDialog saveDialog;
        public OpenDialog openDialog;
        public Parser topLevelParser;
        private IDictionary<Key, TaskList> keyFunctionList = new Dictionary<Key, TaskList>();

        public void InitializeGUIWindow(Parser topparser, string windowTitle = "GUIMode", int x_ = 0, int y_ = 0)
        {
            topLevelParser = topparser;
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
                    BorderBrush = Terminal.Gui.Color.White
                }
            };
            mainWindow.ColorScheme = new ColorScheme()
            {
                Normal = Terminal.Gui.Attribute.Make(Terminal.Gui.Color.White, Terminal.Gui.Color.Black),
                Focus = Terminal.Gui.Attribute.Make(Terminal.Gui.Color.White, Terminal.Gui.Color.Black),
                HotNormal = Terminal.Gui.Attribute.Make(Terminal.Gui.Color.White, Terminal.Gui.Color.Black),
                HotFocus = Terminal.Gui.Attribute.Make(Terminal.Gui.Color.White, Terminal.Gui.Color.Black)
            };

            mainWindow.KeyUp += (e) =>
            {
                if (keyFunctionList.ContainsKey(e.KeyEvent.Key))
                {
                    topLevelParser.executeTask(keyFunctionList[e.KeyEvent.Key].taskList, keyFunctionList[e.KeyEvent.Key].taskType, keyFunctionList[e.KeyEvent.Key].scriptDelay);
                }
            };


            foreach (GUI_BaseItem obj in viewobjects)
            {
                if (obj.container == main)
                {
                    mainWindow.Add(obj.objview);
                }
            }
            // Take every GUI object within GUIItemsToAdd and add it to mainWindow
            foreach (GUI_Menubar item in GUIMenuBarsToAdd)
            {
                top.Add(item.menuBar);
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
        
        public void addKeyPressFunction(TaskList taskList_, Key keyPressed_)
        {
            keyFunctionList.Add(keyPressed_, taskList_);
        }
        public void yesno_msgbox(string title, string msg, string varname)
        {
            int result = MessageBox.Query(title, msg, "YES", "NO");
            LocalVariable tempvar_ = topLevelParser.local_variables.Find(x => x.Name == varname);
            if (tempvar_ == null)
            {
                Console.WriteLine($"Could not access variable {varname}.");
                
                return;
            }
            if (result == 0)
            {
                tempvar_.Value = "True";
            }
            else if (result == 1)
            {
                tempvar_.Value = "False";
            }
        }
        public void ok_msgbox(string title, string msg)
        {
            MessageBox.Query(title, msg, "Ok");
        }

        public void error_msgbox(string title, string msg)
        {
            MessageBox.ErrorQuery(title, msg, "Continue");
        }

        public void showsaveDialog(string title, string msg, string varname, LocalList filetypes = default)
        {
            // Parse list variable values
            List<string> filetypes_parsed = new List<string>();
            foreach(LocalVariable strvariable in filetypes.items)
            {
                filetypes_parsed.Add(strvariable.Value);
            }
            // Create dialog
            saveDialog = new SaveDialog(title, msg, filetypes_parsed);
            // Execute dialog
            Terminal.Gui.Application.Run(saveDialog);
            
            if (!string.IsNullOrEmpty(saveDialog.FilePath.ToString()))
            {
                try
                {
                    LocalVariable localvar = topLevelParser.local_variables.Find(x => x.Name ==  varname);
                    if (localvar == null)
                    {
                        Console.WriteLine($"Could not access variable {varname}.");
                        return;
                    }
                    localvar.Value = saveDialog.FilePath.ToString();
                }
                catch {
                    return;
                }
            } else
            {
                return;
            }
        }

        public void showopenDialog(string title_, string msg, string varname, LocalList filetypes = default)
        {
            // Parse list variable values
            List<string> filetypes_parsed = new List<string>();
            foreach (LocalVariable strvariable in filetypes.items)
            {
                filetypes_parsed.Add(strvariable.Value);
            }
            // Create dialog
            openDialog = new OpenDialog(title_, msg, filetypes_parsed);
           
            // Execute dialog
            Terminal.Gui.Application.Run(openDialog);

            if (!string.IsNullOrEmpty(openDialog.FilePath.ToString()))
            {
                try
                {
                    LocalVariable localvar = topLevelParser.local_variables.Find(x => x.Name == varname);
                    if (localvar == null)
                    {
                        Console.WriteLine($"Could not access variable {varname}.");
                        return;
                    }
                    localvar.Value = openDialog.FilePath.ToString();
                }
                catch
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }
}

