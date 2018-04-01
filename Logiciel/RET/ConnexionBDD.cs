using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Windows.Forms;

namespace RET
{
    static class ConnexionBDD
    {

        static private MySqlConnection _conn;
        static private string _user =       "root";
        static private string _password =   "root";
        static private string _database =   "ret2";
        static private string _host = "localhost"; 
        static private string _port =       "3307";
        static private string _connexionString = "";
        static ConnexionBDD()
        {
            try
            {
                _connexionString = "user=" + _user + ";password=" + _password + ";database=" + _database + ";host=" + _host + ";port=" + _port;
                _conn = new MySqlConnection(_connexionString);
                _conn.Open();
                _conn.Close();
            }
            catch
            {
                _conn = null;
            }
        }

        static public MySqlConnection Connexion 
        {
            get { return _conn; } 
        }

        static public MySqlConnection ReloadConnexion()
        {
            try
            {
                _connexionString = "user=" + _user + ";password=" + _password + ";database=" + _database + ";host=" + _host + ";port=" + _port;
                _conn = new MySqlConnection(_connexionString);
                _conn.Open();
                _conn.Close();
            }
            catch
            {
                _conn = null;
            }
            return _conn;
        }

        static public string User { get { return _user; } set { _user = value; } }
        static public string Password { get { return _password; } set { _password = value; } }
        static public string Database { get { return _database; } set { _database = value; } }
        static public string Host { get { return _host; } set { _host = value; } }
        static public string Port { get { return _port; } set { _port = value; } }
        static public string ConnexionString { get; set; }

    }
}
