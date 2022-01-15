using System;
using System.Collections.Generic;
using System.Text;

namespace GyroPrompt.Functions
{
    public class CSV_Spreadsheet
    {
        public struct Column_structure
        {
            public int count;
            public string data;
            public string designation;
        }

        int total_entries = 0;
        public string Spreadsheet_Name { get; set; }
        public List<Column_structure> Columns = new List<Column_structure>();

        public CSV_Spreadsheet(string name, string header)
        {
            Spreadsheet_Name = name;

            /// <summary>
            /// Create two columns that reside in the 0 index. One is designated as the header, and the
            /// other is designated as the variable holder. 
            /// </summary>

            Column_structure _variables = new Column_structure();
            _variables.count = -1;
            _variables.data = "";
            _variables.designation = "Variables";
            Columns.Add(_variables);

            Column_structure _header = new Column_structure();
            _header.count = -1;
            _header.data = header;
            _header.designation = "Header";
            Columns.Add(_header);

        }

        public void AddColumn(string data)
        {
            Column_structure new_column = new Column_structure();
            new_column.data = data;
            new_column.count = total_entries;
            new_column.designation = "Column";
            total_entries++;
            Columns.Add(new_column);
        }

        public void DeleteColumn(int column_number)
        {

        }

        public void ModifyCell(int x, int y, string new_data)
        {
            if (total_entries <= y)
            {
                bool was_found = false;
                string _rebuilt = "";

                foreach (Column_structure col in Columns)
                {
                    if (col.count == y)
                    {
                        string[] data = col.data.Split(',');
                        if (data.Length <= x)
                        {
                            data[x] = new_data;
                            was_found = true;
                            StringBuilder strn = new StringBuilder();
                            foreach (string str in data)
                            {
                                strn.Append(str + ",");
                            }
                            strn.Remove(strn.Length, 1);
                            _rebuilt = strn.ToString();
                        }
                        else
                        {
                            // ERROR, specified columns does not extend out to X rows
                            Console.WriteLine($"ERROR, less rows than {x}.");
                        }
                    }
                }
                int _index = Columns.FindLastIndex(c => c.count == y);
                if ((_index != -1) && (was_found == true))
                {
                    Column_structure _new = new Column_structure();
                    _new.data = _rebuilt;
                    _new.count = y;
                    _new.designation = "Column";
                    Columns[_index] = _new;
                }
            }
            else
            {
                // ERROR on COUNT, Y should be less than total entries
            }
                }

        public void PrintSpreadsheet()
        {
            Console.Write($"{Spreadsheet_Name}\tColumns: {total_entries}\n");
            int x = 0;
            foreach(Column_structure _column in Columns)
            {
                if (_column.designation == "Column")
                {
                    Console.Write($"{x}:\t {_column.data}\n");
                    x++;
                } else
                {
                    Console.Write($"\t{_column.data}\n");
                }
            }
        }

    }
}
  