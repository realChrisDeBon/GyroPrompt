using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class GUI_textfield : GUI_BaseItem
    {
        public TextView textView;
        public string textfieldtext { get; set; }
        public int LineNumber { get; set; }
        
        public GUI_textfield(string objname, int x_ = 0, int y_ = 0, bool isReadOnly = false) 
        {

            GUIObjName = objname;
            GUIObjectType = GUIObjectType.Textfield;
            textView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = 20,
                Height = 20,
                Text = "Default text",
                ReadOnly = false
            };

            textView.ContentsChanged += (e) =>
            {
                textfieldtext = textView.Text.ToString();
                LineNumber = textView.CurrentRow;
            };
        }
        public void SetText(string text)
        {
            textView.Text = text;
        }

        public override void SetWidth(int x_, fillValue filler)
        {
            try
            {
                switch (filler)
                {
                    case fillValue.Fill:
                        textView.Width = Dim.Fill();
                        break;
                    case fillValue.Percentage:
                        textView.Width = Dim.Percent(x_);
                        break;
                    case fillValue.Number:
                        textView.Width = x_;
                        break;
                }
            }
            catch
            {

            }
        }
        public override void SetHeight(int x_, fillValue filler)
        {
            try
            {
                switch (filler)
                {
                    case fillValue.Fill:
                        textView.Height = Dim.Fill();
                        break;
                    case fillValue.Percentage:
                        textView.Height = Dim.Percent(x_);
                        break;
                    case fillValue.Number:
                        textView.Height = x_;
                        break;
                }
            }
            catch
            {

            }
        }
        public void SetXCoord(int x_)
        {
            try
            {
                textView.X = x_;
            }
            catch
            {

            }
        }
        public void SetYCoord(int x_)
        {
            try
            {
                textView.Y = x_;
            }
            catch
            {

            }
        }
        public void SetDefault()
        {
            
        }
    }
}
