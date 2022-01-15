using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Buffers.Binary;
using GyroPrompt;
using System.Runtime.ExceptionServices;
using GyroPrompt.Functions;
using System.Drawing;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using BlockIoLib;
using System.Configuration;

namespace GyroPrompt.NetworkObjects
{
    public class GyroCloud
    {

        public SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["MainServer"].ConnectionString);

        BlockIo DogeNetwork = new BlockIo(ConfigurationManager.AppSettings["DogeMainnet"], "03231993");

        public struct Credentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public int AvailableData { get; set; }
            public int UsedData { get; set; }
            public int DogeBalance { get; set; }

            public bool isValid { get; set; }

            public void Reset()
            {
                Username = "";
                Password = "";
                AvailableData = 0;
                UsedData = 0;
            }

            public int CalculateDifference()
            {
                int x = (AvailableData - UsedData);
                return x;
            }

        }

        public Credentials credentials = new Credentials();

        public bool Open()
        {
            bool success = true;
            try
            {
                conn.Open();
            } catch
            {
                success = false;
            }
            return success;
        }

        public void CreateUser(string username, string password)
        {
            try
            {
                //conn.Open();

                SqlCommand checkUser = new SqlCommand("SELECT COUNT(*) from UserCredentialTable where Username like @username", conn);
                checkUser.Parameters.AddWithValue("@username", username);
                int userCount = (int)checkUser.ExecuteScalar();
                if (userCount == 0)
                {
                    SqlCommand newUser = new SqlCommand("INSERT INTO UserCredentialTable (Username, Password, DogePublic, DogePrivate) VALUES (@username, @password, @dpublic, @dprivate)", conn);
                    newUser.Parameters.AddWithValue("@username", username);
                    newUser.Parameters.AddWithValue("@password", password);

                    // below parameters will be replaced with a newly generated DogeCoin wallet
                    string dogeWallet = username + "_WALLET";
                    var test = DogeNetwork.GetNewAddress(new { label = dogeWallet });
                    if (test.Status == "success")
                    {
                        string privateKey = test.Data.address;
                        newUser.Parameters.AddWithValue("@dpublic", dogeWallet);
                        newUser.Parameters.AddWithValue("@dprivate", privateKey);
                        newUser.ExecuteScalar();
                       
                            bool worked = true;
                            try
                            {
                                File.WriteAllText($"{username} DogeCoin Address.txt", $"DogeCoin address for {username}: {privateKey}");
                            }
                            catch
                            {
                                GyroCloudError("Error occured creating file!");
                                worked = false;
                            }
                            if (worked == true) { Console.WriteLine("Created a text file with DogeCoin address.\nYou may need this to fund your account!"); }
                        
                    } else
                    {
                        GyroCloudError("Unable to generate a DogeCoin wallet to assign to new account. New account creation cancelled.");
                        
                    }
                } else
                {
                    GyroCloudError($"Username {username} already in use!");
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(@"\nFailed entry.\n{0}\n", e);
                GyroCloudError("Encountered unexpected error!");
            }
        }

        public void UploadFile(string filepath)
        {

            byte[] fileData = File.ReadAllBytes(filepath);
            long _fileSize = new System.IO.FileInfo(filepath).Length;

            FileInfo fileinfo = new FileInfo(filepath);
            string ext = fileinfo.Extension;
            long size = fileinfo.Length;
            int filesize = Convert.ToInt32(size);
            string filename = fileinfo.Name;

            StringBuilder strbuild = new StringBuilder(); // remove file extension 
            strbuild.Append(filename);
            strbuild.Remove(filename.Length - ext.Length, ext.Length);
            string _filename = strbuild.ToString();

            ValidCredential(filesize);
            int x = credentials.CalculateDifference();

            if (credentials.isValid == true && x > filesize)
            {
                bool proceed = true;
                try
                {
                    //conn.Open();
                    SqlCommand uploadFile = new SqlCommand("INSERT INTO ActiveFiles (FileID, FileOwner, FileName, FileSize, FileData, FileType) VALUES (@FileID, @FileOwner, @FileName, @FileSize, @FileData, @FileType)", conn);
                    uploadFile.Parameters.AddWithValue("@FileID", $"{credentials.Username}_{_filename}");
                    uploadFile.Parameters.AddWithValue("@FileOwner", $"{credentials.Username}");
                    uploadFile.Parameters.AddWithValue("@FileName", filename);
                    uploadFile.Parameters.AddWithValue("@FileSize", filesize);
                    uploadFile.Parameters.AddWithValue("@FileData", fileData);
                    uploadFile.Parameters.AddWithValue("@FileType", ext);
                    uploadFile.ExecuteScalar();

                }
                catch (Exception e)
                {
                    proceed = false;
                    //GyroCloudError(e.ToString());
                }
                if (proceed == true)
                {
                    try
                    {
                        // update used data 
                        int y = (credentials.UsedData + filesize);
                        SqlCommand updateUsedData = new SqlCommand("UPDATE UserCredentialTable SET UsedData = @newvalue WHERE Username like @username AND Password like @password", conn);
                        updateUsedData.Parameters.AddWithValue("@newvalue", y);
                        updateUsedData.Parameters.AddWithValue("@username", credentials.Username);
                        updateUsedData.Parameters.AddWithValue("@password", credentials.Password);
                        updateUsedData.ExecuteScalar();
                    }
                    catch
                    {

                    }
                }

            }
            else
            {
                if (x <= filesize && credentials.isValid == true)
                {
                    GyroCloudError("Your account does not have enough space.");
                }
                else
                {
                    GyroCloudError("Could not upload file. Please check your login credentials.");
                }
            }
        }

        public void UploadScript(string filepath)
        {
            string[] fileText = File.ReadAllLines(filepath);
            StringBuilder _strbuild = new StringBuilder();
            foreach (string str in fileText)
            {
                _strbuild.Append($"{str}\n");
            }
            string fileData = _strbuild.ToString();

            long _fileSize = new System.IO.FileInfo(filepath).Length;

            FileInfo fileinfo = new FileInfo(filepath);
            string ext = fileinfo.Extension;
            long size = fileinfo.Length;
            int filesize = Convert.ToInt32(size);
            string filename = fileinfo.Name;

            StringBuilder strbuild = new StringBuilder(); // remove file extension 
            strbuild.Append(filename);
            strbuild.Remove(filename.Length - ext.Length, ext.Length);
            string _filename = strbuild.ToString();

            ValidCredential(filesize);
            int x = credentials.CalculateDifference();

            if (credentials.isValid == true && x > filesize)
            {
                bool proceed = true;
                try
                {
                    //conn.Open();
                    SqlCommand uploadFile = new SqlCommand("INSERT INTO ActiveScript (FileID, FileOwner, FileName, FileSize, ScriptData) VALUES (@FileID, @FileOwner, @FileName, @FileSize, @ScriptData)", conn);
                    uploadFile.Parameters.AddWithValue("@FileID", $"{credentials.Username}_{_filename}");
                    uploadFile.Parameters.AddWithValue("@FileOwner", $"{credentials.Username}");
                    uploadFile.Parameters.AddWithValue("@FileName", filename);
                    uploadFile.Parameters.AddWithValue("@FileSize", filesize);
                    uploadFile.Parameters.AddWithValue("@ScriptData", fileData);
                    uploadFile.ExecuteScalar();

                }
                catch (Exception e)
                {
                    proceed = false;
                    GyroCloudError(e.ToString());
                }
                if (proceed == true)
                {
                    try
                    {
                        // update used data 
                        int y = (credentials.UsedData + filesize);
                        SqlCommand updateUsedData = new SqlCommand("UPDATE UserCredentialTable SET UsedData = @newvalue WHERE Username like @username AND Password like @password", conn);
                        updateUsedData.Parameters.AddWithValue("@newvalue", y);
                        updateUsedData.Parameters.AddWithValue("@username", credentials.Username);
                        updateUsedData.Parameters.AddWithValue("@password", credentials.Password);
                        updateUsedData.ExecuteScalar();
                    }
                    catch
                    {

                    }
                }

            }
            else
            {
                if (x <= filesize && credentials.isValid == true)
                {
                    GyroCloudError("Your account does not have enough space.");
                }
                else
                {
                    GyroCloudError("Could not upload script. Please check your login credentials.");
                }
            }
        }

        public void DownloadFile(string fileID)
        {

            //SELECT * FROM yourtable WHERE yourvarcharfield LIKE '%yoursearchstring%'
            //SqlConnection conn;
            //const string connectionString = "server = LAPTOP-0BVLE8CQ; database = AllUsers; USER ID = usera; PASSWORD = pass123";
            //conn = new SqlConnection(connectionString);

            try
            {
                //conn.Open();
                SqlCommand downloadFile = new SqlCommand("SELECT * FROM ActiveFiles WHERE FileID LIKE @value", conn);
                downloadFile.Parameters.AddWithValue("@value", fileID);
                long binaryData;
                int buff = 100;
                long startIndx = 0;
                long retval;
                using (SqlDataReader reader = downloadFile.ExecuteReader())
                {
                    byte[] outbyte = new byte[buff];
                    while (reader.Read())
                    {
                        FileStream filestream = new FileStream(Directory.GetCurrentDirectory() + "\\" + reader.GetString(2), FileMode.OpenOrCreate, FileAccess.Write);
                        BinaryWriter binarywrite = new BinaryWriter(filestream);

                        binaryData = reader.GetBytes(4, startIndx, outbyte, 0, buff);


                        while (binaryData == buff)
                        {
                            binarywrite.Write(outbyte);
                            binarywrite.Flush();

                            // Reposition the start index to the end of the last buffer and fill the buffer.
                            startIndx += buff;
                            binaryData = reader.GetBytes(4, startIndx, outbyte, 0, buff);
                        }

                        if (binaryData > 0)
                        {
                            binarywrite.Write(outbyte, 0, (int)binaryData);
                        }

                        binarywrite.Close();
                        filestream.Close();

                    }
                }
            }
            catch (Exception e)
            {
                GyroCloudError(e.ToString());
            }
        }

        public string RunScript(string file_name, ref string name)
        {
            SqlCommand grabScript = new SqlCommand("SELECT * FROM ActiveScript WHERE FileID LIKE @name", conn);
            grabScript.Parameters.AddWithValue("@name", file_name);
            string script_text = "";
            try
            {
                using (SqlDataReader reader = grabScript.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        script_text = Convert.ToString(reader["ScriptData"]);
                        name = Convert.ToString(reader["FileName"]);
                    }
                }
            } catch
            {
                GyroCloudError($"Could not grab script {name}");
            }
            return (script_text);
        }

        public void ValidCredential(int _filesize)
        {
            credentials.isValid = false;
            //SqlConnection conn;
            //const string connectionString = "server = LAPTOP-0BVLE8CQ; database = AllUsers; USER ID = usera; PASSWORD = pass123";
            //conn = new SqlConnection(connectionString);
            //conn.Open();
            Console.Write("\t│");
            Console.Write("\n\t└Username: ");
            var _username = Console.ReadLine();
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write("\t├\n");
            Console.Write("\t└Password: ");
            var _password = Console.ReadLine();
            //string __Username = Convert.ToString(_username);
            //string __Password = Convert.ToString(_password);

            SqlCommand CredentialUser = new SqlCommand("SELECT * FROM UserCredentialTable WHERE Username like @username AND Password like @password", conn);
            CredentialUser.Parameters.AddWithValue("@username", _username.ToString());
            CredentialUser.Parameters.AddWithValue("@password", _password.ToString());

            CredentialUser.ExecuteScalar();
            
            using (SqlDataReader reader = CredentialUser.ExecuteReader()) 
            {
                if (reader.Read())
                {
                    credentials.Username = _username;
                    credentials.Password = _password;
                    credentials.AvailableData = Convert.ToInt32(reader["AvailableData"]);
                    credentials.UsedData = Convert.ToInt32(reader["UsedData"]);
                    credentials.isValid = true;
                } else
                {
                    GyroCloudError("Invalid login credentials.");
                }
            }
            // Just remember for some reason to use this style because casting to an int drove me 
            // fucking insane and wouldn't work because HUR DUR Input string not formatted right
            // it worked for checkUser when creating a new login but does not work the same here when
            // we check for Username AND Password which was really fucking weird and did not make sense

            //conn.Close();
        }

        public void DogeCoinTransfer_UserToUser(string _username, string _password, int quantity, string _recipient_username)
        {
            SqlCommand CredentialUser = new SqlCommand("SELECT * FROM UserCredentialTable WHERE Username like @username AND Password like @password", conn);
            CredentialUser.Parameters.AddWithValue("@username", _username);
            CredentialUser.Parameters.AddWithValue("@password", _password);

            CredentialUser.ExecuteScalar();

            using (SqlDataReader reader = CredentialUser.ExecuteReader())
            {
                if (reader.Read())
                {

                    ///<summary>
                    /// Here in the code should grab the DogeCoin balance of the user, ensure is is >= the quantity (amount we're sending) then
                    /// the code should initiate a DogeNetwork transaction using labels. 
                    /// </summary>

                    credentials.Username = _username;
                    credentials.Password = _password;
                    credentials.AvailableData = Convert.ToInt32(reader["AvailableData"]);
                    credentials.UsedData = Convert.ToInt32(reader["UsedData"]);
                    credentials.isValid = true;

                    string wallet_label = _username + "_WALLET";
                    string recipient_label = _recipient_username + "_WALLET";
                    var bal = DogeNetwork.GetAddressBalance(new { label = wallet_label });
                    if (bal.Status == "success")
                    {
                       decimal _amount = Convert.ToDecimal(bal.Data.available_balance);
                       decimal __qty = Convert.ToDecimal(quantity);
                        decimal _a = .0m;
                        decimal _qty = Decimal.Add(__qty, _a);
                        Console.Write("Current balance: ");
                        Console.WriteLine(_amount.ToString());
                        if (_amount >= _qty)
                        {
                            try
                            {
                                var tran = DogeNetwork.WithdrawFromLabels(new {from_labels = wallet_label, to_labels = recipient_label, amounts = _qty.ToString()});

                                if (tran.Status == "success")
                                {
                                    Console.WriteLine($"Sent {quantity} DOGE to user {_recipient_username}.");
                                }
                                else
                                {
                                    GyroCloudError($"Error sending {_qty} DOGE to user {_recipient_username}, please double check to make sure recipient name typed correctly, and that you have enough to cover the network fee.");
                                }

                            } catch (Exception e)
                            {
                                GyroCloudError($"..Error sending {_qty} DOGE to user {_recipient_username}, please double check to make sure recipient name typed correctly, and that you have enough to cover the network fee.\n{e}");
                            }
                        } else
                        {
                            GyroCloudError($"Insufficient DogeCoin balance {_amount}.");
                        }

                    } else
                    {
                        GyroCloudError($"Unable to find DogeCoin wallet associated with {_username}.");
                    }


                }
                else
                {
                    GyroCloudError("Invalid login credentials.");
                }
            }
        }

        public void LoadAccount (string _username, string _password)
        {
            SqlCommand CredentialUser = new SqlCommand("SELECT * FROM UserCredentialTable WHERE Username like @username AND Password like @password", conn);
            CredentialUser.Parameters.AddWithValue("@username", _username);
            CredentialUser.Parameters.AddWithValue("@password", _password);
            
            CredentialUser.ExecuteScalar();
            int qty = 400;
            using (SqlDataReader reader = CredentialUser.ExecuteReader())
            {
                if (reader.Read())
                {

                    ///<summary>
                    /// Here in the code should grab the DogeCoin balance of the user, ensure is is >= the quantity (amount we're sending) then
                    /// the code should initiate a DogeNetwork transaction using labels. 
                    /// </summary>

                    credentials.Username = _username;
                    credentials.Password = _password;
                    credentials.AvailableData = Convert.ToInt32(reader["AvailableData"]);
                    credentials.UsedData = Convert.ToInt32(reader["UsedData"]);
                    credentials.isValid = true;

                    string wallet_label = _username + "_WALLET";
                    var bal = DogeNetwork.GetAddressBalance(new { label = wallet_label });
                    if (bal.Status == "success")
                    {
                        int DogeBalance = Convert.ToInt32(bal.Data.balance);
                        if (DogeBalance >= qty)
                        {
                            try
                            {
                                var tran = DogeNetwork.WithdrawFromLabels(new { amounts = qty, from_labels = wallet_label, to_labels = "GyroSoftLabs" });
                                if (tran.Status == "success")
                                {
                                    AvailableData_1gigabyte(_username);
                                    Console.WriteLine($"Account loaded with 1 gb of data for 30 days.");
                                }
                                else
                                {
                                    GyroCloudError($"Error sending {qty} DOGE to load account.");
                                }
                            }
                            catch
                            {
                                GyroCloudError($"Error sending {qty} DOGE to load account.");
                            }
                        }
                        else
                        {
                            GyroCloudError($"Insufficient DogeCoin balance {qty}.");
                        }

                    }
                    else
                    {
                        GyroCloudError($"Error accessing DogeCoin wallet.");
                    }


                }
                else
                {
                    GyroCloudError("Invalid login credentials.");
                }
            }
        }

        public void AvailableData_1gigabyte (string _username)
        {
            
            DateTime _timestamp = new DateTime();
            _timestamp = DateTime.Now.AddDays(30);
            string str_timestamp = _timestamp.ToString();
            string[] _today_ = str_timestamp.Split(' ');
            string date = _today_[0];

            long gb = 1073741824;
            int one_gigabyte = Convert.ToInt32(gb);
            SqlCommand UpdateAvailableData = new SqlCommand("UPDATE UserCredentialTable SET AvailableData = @data, Timestamp = @timestamp WHERE Username like @username", conn);
            UpdateAvailableData.Parameters.AddWithValue("@username", _username);
            UpdateAvailableData.Parameters.AddWithValue("@data", one_gigabyte);
            UpdateAvailableData.Parameters.AddWithValue("@timestamp", date);

            UpdateAvailableData.ExecuteScalar();
        }

        public void GyroCloudError(string message)
        {
            Console.WriteLine($"\nGyroCloud ‼ {message}\n");
        }
    }
}
