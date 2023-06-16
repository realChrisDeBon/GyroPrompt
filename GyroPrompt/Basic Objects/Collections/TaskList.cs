using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GyroPrompt.Basic_Objects.Collections
{
    public enum TaskType
    {
        BackgroundTask,
        InlineTask
    }

    public class TaskList
    {
        public string taskName { get; set; }
        public int scriptDelay { get; set; }
        public TaskType taskType { get; set; }
        public List<string> taskList = new List<string>();
        public int taskCount
        {
            get { return taskList.Count; }
            set { }
        }
        public TaskList(string taskName_, TaskType taskType_, int scriptDelay_ = 500)
        {
            taskName = taskName_;
            taskType = taskType_;
            scriptDelay = scriptDelay_;
        }

        public void AppendCommand(string command)
        {
            taskList.Add(command);
        }

        public void InsertCommandToPosition(string command, int position)
        {
            if (position <= taskList.Count)
            {
                taskList.Insert(position, command);
            }
            else
            {
                Console.WriteLine($"Index {position} out of bounds, {taskList.Count} elements in task list.");
            }
        }
        public void RemoveCommand(int index)
        {
            taskList.RemoveAt(index);
        }
        public void Clear()
        {
            taskList.Clear();
        }
        public void PrintContents()
        {
            Console.WriteLine($"{taskName} contents:");
            int x = 0;
            foreach (string s in taskList)
            {
                Console.WriteLine($"{x}:\t{s}");
                x++;
            }
        }
    }
}
