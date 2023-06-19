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
    public class GUI_Button : GUI_BaseItem
    {
        public Button newButton;
        public TaskList onClick;

        public GUI_Button(string name_, TaskList commandsOnClick, int x_ = 0, int y_ = 0, int width_ = 4, int height_ = 2)
        {
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.Button;

            newButton = new Button($"{name_}")
            {
                X = x_,
                Y = y_,
                Width = width_,
                Height = height_,
            };
            onClick = commandsOnClick;

            newButton.Clicked += () =>
            {
                executeTask(onClick.taskList, onClick.taskType, onClick.scriptDelay);
            };
        }


        public override void SetWidth(int x_, fillValue filler)
        {
            try
            {
                switch (filler)
                {
                    case fillValue.Fill:
                        newButton.Width = Dim.Fill();
                        break;
                    case fillValue.Percentage:
                        newButton.Width = Dim.Percent(x_);
                        break;
                    case fillValue.Number:
                        newButton.Width = x_;
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
                        newButton.Height = Dim.Fill();
                        break;
                    case fillValue.Percentage:
                        newButton.Height = Dim.Percent(x_);
                        break;
                    case fillValue.Number:
                        newButton.Height = x_;
                        break;
                }
            }
            catch
            {

            }
        }
        public void SetText (string text)
        {
            newButton.Text = text;
        }
        public string GetText()
        {
            return newButton.Text.ToString();
        }
        public void SetXCoord(int x_)
        {
            try
            {
                newButton.X = x_;
            }
            catch
            {

            }
        }
        public void SetYCoord(int x_)
        {
            try
            {
                newButton.Y = x_;
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