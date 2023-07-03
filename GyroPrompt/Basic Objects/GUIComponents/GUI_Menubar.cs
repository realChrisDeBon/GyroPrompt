using GyroPrompt.Basic_Objects.Collections;
using GyroPrompt.Basic_Objects.Variables;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents
{
    public class GUI_Menubar : GUI_BaseItem
    {
        public MenuBar menuBar;
        public List<TaskList> onClick;
        private Parser topLevelParser;

        public GUI_Menubar(Parser toplvlParser, string name_, List<LocalList> menuItemstoadd, List<TaskList> taskItems) 
        {
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.Menubar;
            topLevelParser = toplvlParser;
            onClick = taskItems;

            menuBar = new MenuBar()
            {

            };
            int x = 0;
            foreach (LocalList list in menuItemstoadd)
            {
                MenuItem[] menuItems = new MenuItem[list.numberOfElements];
                for (int y = 0; y < list.numberOfElements; y++)
                {
                    string menuText = list.items[y].Value.ToString().TrimEnd();
                    menuItems[y] = new MenuItem(menuText, "", () => { TaskList exec = onClick.Find(z => z.taskName == menuText); bool itemFound = exec != null; if (itemFound == true) { try { topLevelParser.executeTask(exec.taskList, exec.taskType, exec.scriptDelay); } catch { } } });
                }
                var menuBarItem = new MenuBarItem(list.Name, menuItems);
                menuBar.Menus = menuBar.Menus.Concat(new MenuBarItem[] { menuBarItem }).ToArray();
                x++;
            }


        }
    }
}
