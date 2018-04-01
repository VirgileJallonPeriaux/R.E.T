using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RET
{
    public class Propriete
    {
        private int _id;
        private int _hauteur;
        private decimal _charge;
        private Transport _transport;

        public Propriete(int id, int hauteur, decimal charge)
        {
            _id = id;
            _hauteur = hauteur;
            _charge = charge;
        }

        public int Id { get { return _id; } set { _id = value; } }
        public int Hauteur { get { return _hauteur; } set { _hauteur = value; } }
        public decimal Charge { get { return _charge; } set { _charge = value; } }
        public Transport Transport { get { return _transport; } set { _transport = value; } }
        public override string ToString()
        {
            string s = _id.ToString() + " " + _hauteur.ToString() + " " + _charge.ToString() + " ";
            if (_transport != null) { s += _transport.Libelle; }
            return  s;
        }

    }
}
