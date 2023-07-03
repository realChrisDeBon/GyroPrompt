using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using GyroPrompt.Basic_Objects.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class GUI_Label : GUI_BaseItem
    {
        public Label newlabel;

        public GUI_Label (string name_, string text_ = "Label", int x_ = 0, int y_ = 0, int width_ = 4, int height_ = 2, Color textcolor = Color.White, Color background = Color.DarkGray)
        {
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.Label;
            newlabel = new Label()
            {
                X = x_,
                Y = y_,
                Width = width_,
                Height = height_,
                Text = text_,
                ColorScheme = new ColorScheme()
                {
                    Normal = Terminal.Gui.Attribute.Make(textcolor, background),
                    Focus = Terminal.Gui.Attribute.Make(textcolor, background),
                    HotNormal = Terminal.Gui.Attribute.Make(textcolor, background),
                    HotFocus = Terminal.Gui.Attribute.Make(textcolor, background)
                },
            };

            newlabel.DrawContent += (e) =>
            {
                
            };
        }
        public override void SetWidth(int x_, coordVal filler)
        {
            try
            {
                switch (filler)
                {
                    case coordVal.Fill:
                        newlabel.Width = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        newlabel.Width = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        newlabel.Width = x_;
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
                        newlabel.Height = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        newlabel.Height = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        newlabel.Height = x_;
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
                newlabel.X = Pos.Left(obj);
            } else if (fillval == coordValue.RightOf)
            {
                newlabel.X = Pos.Right(obj);
            }
        }
        public void SetText(string text)
        {
            newlabel.Text = text;
            newlabel.SetNeedsDisplay();
        }
        public string GetText()
        {
            return newlabel.Text.ToString();
        }
        public void SetXCoord(int x_, coordValue filler)
        {
            try
            {
                switch (filler)
                {
                    case coordValue.Number:
                        newlabel.X = x_;
                        break;
                    case coordValue.Center:
                        newlabel.X = Pos.Center();
                        break;
                    case coordValue.Percent:
                        newlabel.X = Pos.Percent(x_);
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
                        newlabel.Y = x_;
                        break;
                    case coordValue.Center:
                        newlabel.Y = Pos.Center();
                        break;
                    case coordValue.Percent:
                        newlabel.Y = Pos.Percent(x_);
                        break;
                }
            }
            catch
            {

            }
        }
    }
}
