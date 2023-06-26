using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using ADOX;
using System.Data.Common;

namespace GyroPrompt.Basic_Objects.Data_Management
{
    public class DatabaseManager
    {
        public void CreateNewServer()
        {

            CreateDatabase("database.mdb");

        }

        public void CreateDatabase(string databasePath)
        {


        }
    }
}
