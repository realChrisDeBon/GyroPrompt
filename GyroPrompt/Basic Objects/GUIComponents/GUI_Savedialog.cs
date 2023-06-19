using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Basic_Objects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents
{

    public class GUI_Savedialog : GUI_BaseItem
    {
        public SaveDialog saveDialog;
        public List<string> validFileExtension = new List<string>();
        public GUI_Savedialog(string title, string message, LocalList fileTypes = default)
        {
            GUIObjectType = GUIObjectType.Dialog;
            GUIObjName = title;
            saveDialog.Title = title;
            saveDialog.Message = message;
            try
            {
                if (fileTypes.arrayType == ArrayType.String) {
                    foreach (LocalVariable type in fileTypes.items)
                    {
                        validFileExtension.Add(type.Value);
                    }
                    saveDialog.AllowedFileTypes = validFileExtension.ToArray();
                } else
                {
                    // Wrong list type
                }
            }
            catch
            {
                // likely did not format list correctly
            }
        }

        public string ShowSaveDialog()
        {
            string path = "";
            Application.Run(saveDialog);
            if (!string.IsNullOrEmpty(saveDialog.FilePath.ToString()))
            {
                try
                {
                    path = saveDialog.FilePath.ToString();
                }
                catch { }
            }
            return path;
        }
    }

}   
    

