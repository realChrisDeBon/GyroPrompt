using GyroPrompt.Basic_Objects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class GUI_Checkbox : GUI_BaseItem
    {
        public CheckBox newCheckbox;
        private Parser topLevelParser;
        // Bool variables that will be set to match the checkbox on event checkbox toggled
        List<string> linkedBoolVariables = new List<string>();
        public bool hasLinkedBools = false;

        public GUI_Checkbox(Parser topparse, string name_, string text_ = "Checkbox", int x_ = 0, int y_ = 0, int width_ = 4, int height_ = 2, bool isChecked_ = false, bool hasLinkedBools_ = false, List<string> linkedBool_ = default, Color textcolor = Color.White, Color background = Color.Black)
        {
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.Checkbox;
            topLevelParser = topparse;
            linkedBoolVariables = linkedBool_;
            hasLinkedBools = hasLinkedBools_;
            newCheckbox = new CheckBox()
            {
                X = x_,
                Y = y_,
                Width = width_,
                Height = height_,
                Text = text_,
                Checked = isChecked_,
                ColorScheme = new ColorScheme()
                {
                    Normal = Terminal.Gui.Attribute.Make(textcolor, background),
                    Focus = Terminal.Gui.Attribute.Make(textcolor, background),
                    HotNormal = Terminal.Gui.Attribute.Make(textcolor, background),
                    HotFocus = Terminal.Gui.Attribute.Make(textcolor, background)
                },
            };

            newCheckbox.Toggled += (bool status) =>
            {
                if (hasLinkedBools == true)
                {
                    foreach (string boolVar in linkedBoolVariables)
                    {
                        LocalVariable locvar = topLevelParser.local_variables.Find(z => z.Name == boolVar);
                        if (locvar != null)
                        {
                            if (status == true)
                            {
                                locvar.Value = "False";
                            } else if (status == false)
                            {
                                locvar.Value = "True";
                            }

                        }
                    }
                }
            };

            objview = newCheckbox;
        }

        public void addLinkedBool(string boolName)
        {
            if (hasLinkedBools == false) { hasLinkedBools = true; }
            linkedBoolVariables.Add(boolName);
        }
        public override void SetWidth(int x_, coordVal filler)
        {
            try
            {
                switch (filler)
                {
                    case coordVal.Fill:
                        newCheckbox.Width = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        newCheckbox.Width = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        newCheckbox.Width = x_;
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
                        newCheckbox.Height = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        newCheckbox.Height = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        newCheckbox.Height = x_;
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
                newCheckbox.X = Pos.Left(obj);
            }
            else if (fillval == coordValue.RightOf)
            {
                newCheckbox.X = Pos.Right(obj);
            }
        }
        public override void SetText(string text)
        {
            newCheckbox.Text = text;
        }
        public override string GetText()
        {
            return newCheckbox.Text.ToString();
        }
        public override void SetXCoord(int x_, coordValue filler)
        {
            try
            {
                switch (filler)
                {
                    case coordValue.Number:
                        newCheckbox.X = x_;
                        break;
                    case coordValue.Center:
                        newCheckbox.X = Pos.Center();
                        break;
                    case coordValue.Percent:
                        newCheckbox.X = Pos.Percent(x_);
                        break;
                }
            }
            catch
            {

            }
        }
        public override void SetYCoord(int x_, coordValue filler)
        {
            try
            {
                switch (filler)
                {
                    case coordValue.Number:
                        newCheckbox.Y = x_;
                        break;
                    case coordValue.Center:
                        newCheckbox.Y = Pos.Center();
                        break;
                    case coordValue.Percent:
                        newCheckbox.Y = Pos.Percent(x_);
                        break;
                }
            }
            catch
            {

            }
        }




    }
}