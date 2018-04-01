using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RET
{
    public class Bloc
    {
        private int _id;
        private Navire _navire;
        private string _repere;
        private DateTime _dateDebutPm;
        private DateTime _dateFinPm;
        private DateTime _dateFinBord;
        private bool _dateDebutPmVerrouillee;
        private bool _dateFinPmVerrouillee;
        private string _remarque;
        private bool _stadeEtudePm;
        private bool _stadeEtudeBord;

        public Bloc(int id, Navire navire, string repere, DateTime dateDebutPm, DateTime dateFinPm, DateTime dateFinBord, bool dateDebutPmVerrouillee, bool dateFinPmVerrouillee, string remarque, bool stadeEtudePm, bool stadeEtudeBord) // bool dateFinBordVerrouillee,
        {
            _id = id;
            _navire = navire;
            _repere = repere;
            _dateDebutPm = dateDebutPm;
            _dateFinPm = dateFinPm;
            _dateFinBord = dateFinBord;
            _dateDebutPmVerrouillee = dateDebutPmVerrouillee;
            _dateFinPmVerrouillee = dateFinPmVerrouillee;
            _remarque = remarque;
            _stadeEtudePm = stadeEtudePm;
            _stadeEtudeBord = stadeEtudeBord;
        }

        public int Id { get { return _id; } set { _id = value; } }
        public Navire Navire { get { return _navire; } set { _navire = value; } }
        public string Repere { get { return _repere; } set { _repere = value; } }
        public DateTime DateDebutPm { get { return _dateDebutPm; } set { _dateDebutPm = value; } }
        public DateTime DateFinPm { get { return _dateFinPm; } set { _dateFinPm = value; } }
        public DateTime DateFinBord { get { return _dateFinBord; } set { _dateFinBord = value; } }
        public bool DateDebutPmVerrouillee { get { return _dateDebutPmVerrouillee; } set { _dateDebutPmVerrouillee = value; } }
        public bool DateFinPmVerrouillee { get { return _dateFinPmVerrouillee; } set { _dateFinPmVerrouillee = value; } }
        public string Remarque { get { return _remarque; } set { _remarque = value; } }
        public bool StadeEtudePm { get { return _stadeEtudePm; } set { _stadeEtudePm = value; } }
        public bool StadeEtudeBord { get { return _stadeEtudeBord; } set { _stadeEtudeBord = value; } }
        public override string ToString()
        {
            return _id.ToString() + " " + Navire.Nom + " " + _repere + " " + _dateDebutPm.ToString() + " " + _dateFinPm.ToString() + " " + _dateFinBord.ToString() + " " + _dateDebutPmVerrouillee.ToString() + " " + _dateFinPmVerrouillee.ToString() + " " + _remarque + " " + _stadeEtudePm.ToString() + " " + _stadeEtudeBord.ToString();
        }

    }
}
