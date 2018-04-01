using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RET
{
    public class Equerre
    {
        private int _id;
        private string _repere;
        private string _remarque;
        private TypeEquerre _typeEquerre;

        public Equerre(int id, string repere, string remarque)
        {
            _id = id;
            _repere = repere;
            _remarque = remarque;
        }

        public int Id { get { return _id; } set { _id = value; } }
        public string Repere { get { return _repere; } set { _repere = value; } }
        public string Remarque { get { return _remarque; } set { _remarque = value; } }
        public TypeEquerre TypeEquerre { get { return _typeEquerre; } set { _typeEquerre = value; } }

        public override string ToString()
        {
            string s = _id.ToString() + " " + _repere + " " + _remarque + " ";
            if (_typeEquerre != null) { s +=_typeEquerre.Repere; }
            return s;
        }

    }
}
