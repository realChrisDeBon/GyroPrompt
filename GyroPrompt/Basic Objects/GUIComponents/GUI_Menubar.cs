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

        public GUI_Menubar(string name_, List<LocalList> menuItemstoadd) 
        {
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.Menubar;

            menuBar = new MenuBar()
            {

            };
            int x = 0;
            foreach (LocalList list in menuItemstoadd)
            {
                MenuItem[] menuItems = new MenuItem[list.numberOfElements];
                for (int y = 0; y < list.numberOfElements; y++)
                {
                    menuItems[y] = new MenuItem(list.items[y].Value, "", () => { });
                }
                var menuBarItem = new MenuBarItem(list.Name, menuItems);
                menuBar.Menus.SetValue(menuBarItem, x);
                x++;
            }


        }
    }
}
