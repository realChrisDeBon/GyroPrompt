using Terminal.Gui;
using GyroPrompt.Basic_Objects.Collections;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class GUI_Button : GUI_BaseItem
    {
        public Button newButton;
        public TaskList onClick;
        private Parser topLevelParser;

        public GUI_Button(Parser topparse, string name_, TaskList commandsOnClick, string text_ = "Button", int x_ = 0, int y_ = 0, int width_ = 4, int height_ = 2, Color textcolor = Color.White, Color background = Color.DarkGray)
        {
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.Button;
            topLevelParser = topparse;

            newButton = new Button()
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
            onClick = commandsOnClick;

            newButton.Clicked += () =>
            {
                btnClicked();
            };

            objview = newButton;
        }
        public void btnClicked()
        {
            topLevelParser.executeTask(onClick.taskList, onClick.taskType, onClick.scriptDelay);
        }
        public override void SetWidth(int x_, coordVal filler)
        {
            try
            {
                switch (filler)
                {
                    case coordVal.Fill:
                        newButton.Width = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        newButton.Width = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        newButton.Width = x_;
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
                        newButton.Height = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        newButton.Height = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        newButton.Height = x_;
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
                newButton.X = Pos.Left(obj);
            }
            else if (fillval == coordValue.RightOf)
            {
                newButton.X = Pos.Right(obj);
            }
        }
        public override void SetText (string text)
        {
            newButton.Text = text;
        }
        public override string GetText()
        {
            return newButton.Text.ToString();
        }
        public override void SetXCoord(int x_, coordValue filler)
        {
            try
            {
                switch (filler)
                {
                    case coordValue.Number:
                        newButton.X = x_;
                        break;
                    case coordValue.Center:
                        newButton.X = Pos.Center();
                        break;
                    case coordValue.Percent:
                        newButton.X = Pos.Percent(x_);
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
                        newButton.Y = x_;
                        break;
                    case coordValue.Center:
                        newButton.Y = Pos.Center();
                        break;
                    case coordValue.Percent:
                        newButton.Y = Pos.Percent(x_);
                        break;
                }
            }
            catch
            {

            }
        }
        public void SetToDefault()
        {
            newButton.IsDefault = true;
        }
    }
}