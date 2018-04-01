using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RET
{
    static class PersistanceParametres
    {

        static private string _cheminDossierSauvegardePdf;
        static private string _cheminFichierTxtConnexionBdd;
        static private string _cheminFichierExtractionC212P;
        static private string _cheminImageErreur;
        static private DateTime _dateMiseAJour;

        static PersistanceParametres()
        {

        }


        static public string CheminDossierSauvegardePdf { get { return _cheminDossierSauvegardePdf; } set { _cheminDossierSauvegardePdf = value; } }
        static public string CheminFichierTxtConnexionBdd { get { return _cheminFichierTxtConnexionBdd; } set { _cheminFichierTxtConnexionBdd = value; } }
        static public string CheminFichierExtractionC212P { get { return _cheminFichierExtractionC212P; } set { _cheminFichierExtractionC212P = value; } }
        static public string CheminImageErreur { get { return _cheminImageErreur; } set { _cheminImageErreur = value; } }
        static public DateTime DateMiseAJour { get { return _dateMiseAJour; } set { _dateMiseAJour = value; } }

    }
}
