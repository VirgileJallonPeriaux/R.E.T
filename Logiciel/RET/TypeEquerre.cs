using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RET
{
    public class TypeEquerre
    {
        private int _id;
        private string _repere;
        private string _numeroPlan;
        private bool _semblable;
        private int _reglageHauteur;
        private string _cheminImage;
        private List<Propriete> _proprietes;

        public TypeEquerre(int id, string repere, string numeroPlan, bool semblable, int reglageHauteur, string cheminImage)
        {
            _id = id;
            _repere = repere;
            _numeroPlan = numeroPlan;
            _semblable = semblable;
            _reglageHauteur = reglageHauteur;
            _cheminImage = cheminImage;
            _proprietes = new List<Propriete>();
        }


        public int Id { get { return _id; } set { _id = value; } }
        public string Repere { get { return _repere; } set { _repere = value; } }
        public string NumeroPlan { get { return _numeroPlan; } set { _numeroPlan = value; } }
        public bool Semblable { get { return _semblable; } set { _semblable = value; } }
        public int ReglageHauteur { get { return _reglageHauteur; } set { _reglageHauteur = value; } }
        public string CheminImage { get { return _cheminImage; } set { _cheminImage = value; } }
        public List<Propriete> Proprietes { get { return _proprietes; } set { _proprietes = value; } }
        public override string ToString()
        {
            return _id.ToString() + " " + _repere + " " + _numeroPlan + " " + _semblable.ToString() + " " + _reglageHauteur.ToString() + " " + _cheminImage;
        }

    }
}
