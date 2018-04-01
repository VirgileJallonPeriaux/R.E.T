using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RET
{
    public class Transport
    {
        private int _id;
        private string _classe;
        private string _libelle;

        public Transport(int id, string classe, string libelle)
        {
            _id = id;
            _classe = classe;
            _libelle = libelle;
        }

        public int Id { get { return _id; } set { _id = value; } }
        public string Classe { get { return _classe; } set { _classe = value; } }
        public string Libelle { get { return _libelle; } set { _libelle = value; } }
        public override string ToString()
        {
            return _id.ToString() + " " + _classe + " " + _libelle;
        }

    }
}
