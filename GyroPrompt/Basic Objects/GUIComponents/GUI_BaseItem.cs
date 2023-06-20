using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public enum GUIObjectType
    {
        Button,
        Textfield,
        Menubar,
        Statusbar,
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
        public string GUIObjName { get; set; }
        public GUIObjectType GUIObjectType { get; set;}
        public virtual void SetWidth(int xx, coordVal fillval)
        {

        }
        public virtual void SetHeight(int xx, coordVal fillval){

        }
    }
}