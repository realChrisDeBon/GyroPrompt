using GyroPrompt.Basic_Objects.GUIComponents.Containers;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class GUI_TabContainer : GUI_BaseItem
    {
        public TabView tabcontainer;
        List<GUI_Tab> tabs_ = new List<GUI_Tab>();
        public Parser toplvlparse;
        public GUI_TabContainer(Parser top_, string name_, string[] tabs, int x_ = 0, int y_ = 0, int width_ = 4, int height_ = 2, Color textcolor = Color.White, Color background = Color.DarkGray) 
        { 
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.TabContainer;

            tabcontainer = new TabView()
            {
                X = x_,
                Y = y_,
                Width = width_,
                Height = height_,
                ColorScheme = new ColorScheme()
                {
                    Normal = Terminal.Gui.Attribute.Make(textcolor, background),
                    Focus = Terminal.Gui.Attribute.Make(textcolor, background),
                    HotNormal = Terminal.Gui.Attribute.Make(textcolor, background),
                    HotFocus = Terminal.Gui.Attribute.Make(textcolor, background)
                },
                
            };
            toplvlparse = top_;
            objview = tabcontainer;
            foreach (string s in tabs)
            {
                string[] nameThenText = s.Split(',');
                // nameThenText[0] is name
                // nameThenText[1] is text
                if (nameThenText.Length == 2)
                {
                    GUI_Tab newtab_ = new GUI_Tab(nameThenText[0], toplvlparse);
                    tabcontainer.AddTab(new TabView.Tab(nameThenText[1], newtab_.tabview), false);
                    toplvlparse.GUIObjectsInUse.Add(nameThenText[0], newtab_ );
                    toplvlparse.consoleDirector.viewobjects.Add(newtab_);
                    tabs_.Add(newtab_);
                }
            }
            tabcontainer.SelectedTab = tabcontainer.Tabs.First();
        }
        public override void SetWidth(int x_, coordVal filler)
        {
            try
            {
                switch (filler)
                {
                    case coordVal.Fill:
                        tabcontainer.Width = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        tabcontainer.Width = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        tabcontainer.Width = x_;
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
                        tabcontainer.Height = Dim.Fill();
                        break;
                    case coordVal.Percentage:
                        tabcontainer.Height = Dim.Percent(x_);
                        break;
                    case coordVal.Number:
                        tabcontainer.Height = x_;
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
                tabcontainer.X = Pos.Left(obj);
            }
            else if (fillval == coordValue.RightOf)
            {
                tabcontainer.X = Pos.Right(obj);
            }
        }
        public override void SetXCoord(int x_, coordValue filler)
        {
            try
            {
                switch (filler)
                {
                    case coordValue.Number:
                        tabcontainer.X = x_;
                        break;
                    case coordValue.Center:
                        tabcontainer.X = Pos.Center();
                        break;
                    case coordValue.Percent:
                        tabcontainer.X = Pos.Percent(x_);
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
                        tabcontainer.Y = x_;
                        break;
                    case coordValue.Center:
                        tabcontainer.Y = Pos.Center();
                        break;
                    case coordValue.Percent:
                        tabcontainer.Y = Pos.Percent(x_);
                        break;
                }
            }
            catch
            {

            }
        }

    }

}
