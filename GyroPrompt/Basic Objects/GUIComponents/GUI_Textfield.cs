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
                ReadOnly = isReadOnly,
                RightOffset = 1,
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
        
        public void AddScrollbars()
        {
            var _scrollBar = new ScrollBarView(textView, false, true);

            _scrollBar.ChangedPosition += () => {
                textView.TopRow = _scrollBar.Position;
                if (textView.TopRow != _scrollBar.Position)
                {
                    _scrollBar.Position = textView.TopRow;
                }
                textView.SetNeedsDisplay();
                if (textView.HasFocus == true)
                {
                    textView.SetFocus();
                }
            };

            _scrollBar.OtherScrollBarView.ChangedPosition += () => {
                textView.LeftColumn = _scrollBar.OtherScrollBarView.Position;
                if (textView.LeftColumn != _scrollBar.OtherScrollBarView.Position)
                {
                    _scrollBar.OtherScrollBarView.Position = textView.LeftColumn;
                }
                textView.SetNeedsDisplay();
            };


            textView.DrawContent += (e) => {

                _scrollBar.Size = textView.Frame.Width;
                _scrollBar.Position = textView.TopRow;
                _scrollBar.OtherScrollBarView.Size = textView.Maxlength;
                _scrollBar.OtherScrollBarView.Position = textView.LeftColumn;
                _scrollBar.Refresh();
            };
        }
    }
}
