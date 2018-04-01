using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

namespace RET
{
    public class Utilisateur
    {
        private int _id;
        private string _trigramme;
        private int _rang;
        private string _nom;
        private string _prenom;
        private bool _main;


        public Utilisateur(int id, string trigramme, int rang, string nom, string prenom, bool main)
        {
            _id = id;
            _trigramme = trigramme;
            _rang = rang;
            _nom = nom;
            _prenom = prenom;
            _main = main;
        }

        public int Id { get { return _id; } set { _id = value; } }
        public string Trigramme { get { return _trigramme; } set { _trigramme = value; } }
        public int Rang { get { return _rang; } set { _rang = value; } }
        public string Nom { get { return _nom; } set { _nom = value; } }
        public string Prenom { get { return _prenom; } set { _prenom = value; } }
        public bool Main { get { return _main; } set { _main = value; } }
        public override string ToString()
        {
            return _id.ToString() + " " + _trigramme + " " + _rang.ToString() + " " + _nom + " " + _prenom+" "+_main.ToString();
        }

    }
}
