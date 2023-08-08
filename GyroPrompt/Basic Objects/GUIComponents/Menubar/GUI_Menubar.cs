using GyroPrompt.Basic_Objects.Collections;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class GUI_Menubar : GUI_BaseItem
    {
        public MenuBar menuBar;
        public List<TaskList> onClick;
        private Parser topparser;

        public GUI_Menubar(Parser toplvlParser, string name_, List<string> topitems, List<string> subitems) 
        {
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.Menubar;
            topparser = toplvlParser;


            menuBar = new MenuBar()
            {

            };


            foreach (string topname in topitems)
            {
                List<string> validvals = new List<string>();
                foreach (string sub_ in subitems)
                {
                    bool isvalid = false;
                    string[] checker = sub_.Split(',');
                    if (checker.Length >= 2)
                    {
                        if (checker[0].Equals(topname, StringComparison.OrdinalIgnoreCase))
                        {
                            validvals.Add(sub_);
                        }
                    }
                    else
                    {

                    }
                }

                MenuItem[] menuItems = new MenuItem[validvals.Count];
                for (int y = 0; y < validvals.Count; y++)
                {
                    string[] textAndFunction = subitems[y].Split(',');
                    bool hasMinimumElement = (textAndFunction.Length >= 2);
                    if (hasMinimumElement == false)
                    {
                        continue;
                    }
                    bool isAssignedToTop = (textAndFunction[0] == topname);
                    if ((isAssignedToTop == true) && (hasMinimumElement == true))
                    {
                        string expectedText = textAndFunction[1];
                        string expectedCommand = textAndFunction[2];
                        if (textAndFunction.Length > 2)
                        {
                            menuItems[y] = new MenuItem(expectedText, "", () => { try { topparser.parse(expectedCommand); } catch { } });
                        }
                        else
                        {
                            // Throw error

                        }
                    }
                }
                var menuBarItem = new MenuBarItem(topname, menuItems);
                menuBar.Menus = menuBar.Menus.Concat(new MenuBarItem[] { menuBarItem }).ToArray();
            }




        }
    }
}
