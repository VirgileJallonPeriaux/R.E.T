using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RET
{
    public class Navire
    {
        private int _id;
        private string _nom;

        public Navire(int id, string nom)
        {
            _id = id;
            _nom = nom;
        }

        public int Id { get { return _id; } set { _id = value; } }
        public string Nom { get { return _nom; } set { _nom = value; } }
        public override string ToString()
        {
            return _id.ToString() + " " + _nom;
        }


    }
}
