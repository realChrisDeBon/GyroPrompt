using System.Collections;
using Terminal.Gui;

namespace GyroPrompt.Basic_Objects.GUIComponents.Containers
{
    public class GUI_Tab : GUI_BaseItem
    {
        public ArrayList porteditems = new ArrayList();

        public View tabview;
        public Parser toplvl;
        public GUI_Tab(string name_, Parser topparser_)
        {
            GUIObjName = name_;
            GUIObjectType = GUIObjectType.Tab;
            toplvl = topparser_;
            tabview = new View()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            objview = tabview;
        }

        public View getView()
        {
            return tabview;
        }
        public override void PortItem(GUI_BaseItem item)
        {
            item.container = GUIObjName;
            porteditems.Add(item);
            tabview.Add(item.objview);
        }
    }
}