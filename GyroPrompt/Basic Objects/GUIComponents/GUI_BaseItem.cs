using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public enum GUIObjectType
    {
        [Description("button")]
        Button,
        [Description("textfield")]
        Textfield,
        [Description("label")]
        Label,
        [Description("cheeckbox")]
        Checkbox,
        [Description("radiobutton")]
        Radiobutton,
        [Description("menubar")]
        Menubar,
        [Description("statusbar")]
        Statusbar,
        [Description("dialog")]
        Dialog
    }
    // fillValue will help user fine tune the size of objects like text fields
    public enum coordVal
    {
        Number,
        Percentage,
        Fill
    }
    // coordValue will help user fine tune X, Y placement
    public enum coordValue
    {
        Center,
        Bottom,
        Top,
        Right,
        Left,
        RightOf,
        LeftOf,
        Number,
        Percent
    }
    public class GUI_BaseItem : Parser
    {
        public View objview { get; set; }
        public string GUIObjName { get; set; }
        public GUIObjectType GUIObjectType { get; set;}
        public virtual void SetWidth(int xx, coordVal fillval)
        {

        }
        public virtual void SetHeight(int xx, coordVal fillval){

        }
        public virtual void SetToLeftOrRight(View obj, coordValue fillval)
        {

        }
        public virtual void SetXCoord(int x_, coordValue filler)
        {

        }
        public virtual void SetYCoord(int x_, coordValue filler)
        {

        }
        public virtual string GetText()
        {
            return "";
        }
        public virtual void SetText(string text)
        {

        }
    }
}