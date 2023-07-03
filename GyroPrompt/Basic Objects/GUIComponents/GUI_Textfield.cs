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
        public GUI_textfield(string objname, int x_ = 0, int y_ = 0, int width_ = 20, int height_ = 20, bool multiline_ = true, string text_ = "Default test", bool isReadOnly = false, Color textcolor = Color.White, Color background = Color.DarkGray) 
        {

            GUIObjName = objname;
            GUIObjectType = GUIObjectType.Textfield;
            textView = new TextView()
            {
                X = x_,
                Y = y_,
                Width = width_,
                Height = height_,
                Text = text_,
                ReadOnly = isReadOnly,
                Multiline = multiline_,
                RightOffset = 1,
                ColorScheme = new ColorScheme()
                {
                    Normal = Terminal.Gui.Attribute.Make(textcolor, background),
                    Focus = Terminal.Gui.Attribute.Make(textcolor, background),
                    HotNormal = Terminal.Gui.Attribute.Make(textcolor, background),
                    HotFocus = Terminal.Gui.Attribute.Make(textcolor, background)
                },
            };
            textView.KeyUp += (e) =>
            {
                LineNumber = textView.CurrentRow; // Enable linenumber tracking
            };
            textView.KeyDown += (e) =>
            {
                LineNumber = textView.CurrentRow; // Enable linenumber tracking
            };
            textView.ContentsChanged += (e) =>
            {
                textfieldtext = textView.Text.ToString();
                LineNumber = textView.CurrentRow; // Enable linenumber tracking
            };


        }
        public string GetText()
        {
            return textView.Text.ToString();
        }
        public void SetText(string text)
        {
            textView.Text = text;
        }
        public override void SetWidth(int x_, coordVal filler)
        {
            try
            {
                switch (filler)
                {
                    case coordVal.Fill:
                        textView.Width = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        textView.Width = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        textView.Width = x_;
                        break;
                }
            }
            catch
            {

            }
        }
        public override void SetHeight(int x_, coordVal filler)
        {
            try
            {
                switch (filler)
                {
                    case coordVal.Fill:
                        textView.Height = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        textView.Height = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        textView.Height = x_;
                        break;
                }
            }
            catch
            {

            }
        }
        public override void SetToLeftOrRight(View obj, coordValue fillval)
        {
            if (fillval == coordValue.LeftOf)
            {
                textView.X = Pos.Left(obj);
            }
            else if (fillval == coordValue.RightOf)
            {
                textView.X = Pos.Right(obj);
            }
        }
        public void SetXCoord(int x_, coordValue filler)
        {
            try
            {
                switch (filler)
                {
                    case coordValue.Number:
                        textView.X = x_;
                        break;
                    case coordValue.Center:
                        textView.X = Pos.Center();
                        break;
                    case coordValue.Percent:
                        textView.X = Pos.Percent(x_);
                        break;
                }
            }
            catch
            {

            }
        }
        public void SetYCoord(int x_, coordValue filler)
        {
            try
            {
                switch (filler)
                {
                    case coordValue.Number:
                        textView.Y = x_;
                        break;
                    case coordValue.Center:
                        textView.Y = Pos.Center();
                        break;
                    case coordValue.Percent:
                        textView.Y = Pos.Percent(x_);
                        break;
                }
            }
            catch
            {

            }
        }
        public void AddScrollbars()
        {
            
        }
    }
}
