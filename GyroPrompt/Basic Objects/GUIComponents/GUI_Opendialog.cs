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
    public class GUI_Opendialog : GUI_BaseItem
    {
        public OpenDialog openDialog;
        public List<string> validFileExtension = new List<string>();
        public GUI_Opendialog(string title, string message, LocalList fileTypes = default)
        {
            GUIObjectType = GUIObjectType.Dialog;
            GUIObjName = title;
            openDialog.Title = title;
            openDialog.Message = message;
            try
            {
                if (fileTypes.arrayType == ArrayType.String)
                {
                    foreach (LocalVariable type in fileTypes.items)
                    {
                        validFileExtension.Add(type.Value);
                    }
                    openDialog.AllowedFileTypes = validFileExtension.ToArray();
                }
                else
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
            Application.Run(openDialog);
            if (!string.IsNullOrEmpty(openDialog.FilePath.ToString()))
            {
                try
                {
                    path = openDialog.FilePath.ToString();
                }
                catch { }
            }
            return path;
        }
    }
}