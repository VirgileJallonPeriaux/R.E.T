using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Collections;

namespace RET
{
    public class BDDQuery
    {
        // todo : quand une requête retourne un seul resultat : utiliser "limit 0,1" dans la requête et remplacer executeReader par executeScalar

        private MySqlCommand _requete;
        private MySqlConnection _connexion;

        public BDDQuery()
        {
            _connexion = ConnexionBDD.Connexion;
            _requete = new MySqlCommand();
        }
        
        // ############################################################ \\
        // ########################## Select ########################## \\
        // ############################################################ \\
        /// <summary>
        /// Retourne l'utilisateur ayant le trigramme passé en paramètres. Si cet utilisateur n'existe pas, retourne un nouvel utilisateur
        /// </summary>
        /// <param name="trigramme">Trigramme de l'utilisateur</param>
        /// <returns></returns>
        public Utilisateur getUtilisateur(string trigramme)
        {
            Utilisateur utilisateur;
            _connexion.Open();
            _requete = new MySqlCommand("select * from Utilisateur where trigramme=@trigramme", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@trigramme", trigramme));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            if(dr.HasRows)  // Si l'utilisateur recherché existe, on l'instancie
            {
                utilisateur = new Utilisateur(Convert.ToInt32(dr["id"]), (string)dr["trigramme"], Convert.ToInt32(dr["rang"]), (string)dr["nom"], (string)dr["prenom"], Convert.ToBoolean(dr["main"]));
            }
            else            // Sinon, on en crée un nouveau
            {
                _connexion.Close();
                utilisateur = newUtilisateur(trigramme);
                _connexion.Open();
            }
            dr.Close();
            _connexion.Close();

            return utilisateur;
        }

        /// <summary>
        /// Retourne l'utilisateur qui a actuellement la main sur la réservation. Si aucun utilisateur a la main, retourne un nouvel utilisateur
        /// </summary>
        /// <returns></returns>
        public Utilisateur getUtilisateurEnTrainDeReserver()
        {
            Utilisateur utilisateur;
            _connexion.Open();
            _requete = new MySqlCommand("select * from Utilisateur where main=@main", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@main", true));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            if (dr.HasRows)
            {
                utilisateur = new Utilisateur(Convert.ToInt32(dr["id"]), (string)dr["trigramme"], Convert.ToInt32(dr["rang"]), (string)dr["nom"], (string)dr["prenom"], Convert.ToBoolean(dr["main"]));
            }
            else      
            {
                utilisateur = new Utilisateur(0, "", 0, "", "", false);
            }
            dr.Close();
            _connexion.Close();

            return utilisateur;
        }

        /// <summary>
        /// Retourne le navire ayant pour nom {nomNavire}
        /// </summary>
        /// <param name="nomNavire">Nom du navire [Ex : "G34"]</param>
        /// <returns></returns>
        public Navire getNavire(string nomNavire)
        {
            _connexion.Open();
            _requete = new MySqlCommand("select * from Navire where nom=@nom", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@nom", nomNavire));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            Navire navire = new Navire((int)dr["id"], (string)dr["nom"]);
            dr.Close();
            _connexion.Close();

            return navire;
        }

        /// <summary>
        /// Retourne le bloc du navire {navire} ayant pour repère {repere}
        /// </summary>
        /// <param name="navire">Navire possédant le bloc recherché</param>
        /// <param name="repere">Repère du bloc [Ex : 0104]</param>
        /// <returns></returns>
        public Bloc getBloc(Navire navire, string repere)
        {
            _connexion.Open();
            _requete = new MySqlCommand("select * from Bloc where idNavire=@idNavire and repere=@repere", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idNavire", navire.Id));
            _requete.Parameters.Add(new MySqlParameter("@repere", repere));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            Bloc bloc = new Bloc((int)dr["id"], navire, repere, (DateTime)dr["dateDebutPm"], (DateTime)dr["dateFinPm"], (DateTime)dr["dateFinBord"], Convert.ToBoolean(dr["dateDebutPmVerrouillee"]),  Convert.ToBoolean(dr["dateFinPmVerrouillee"]), (string)dr["remarque"], Convert.ToBoolean(dr["stadeEtudePm"]), Convert.ToBoolean(dr["stadeEtudeBord"]));
            dr.Close();
            _connexion.Close();
            return bloc;
        }

        /// <summary>
        /// Retourne TRUE si tous les PDFs ne sont pas à jour, sinon retourne FALSE
        /// </summary>
        /// <returns></returns>
        public bool miseAJourPdfIncomplete()
        {
            _connexion.Open();
            _requete = new MySqlCommand("select count(*) as 'nombre' from EtatMiseAJourPdf where pm=0 or bord=0", _connexion); 
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            int nombre = Convert.ToInt32(dr["nombre"]);
            dr.Close();
            _connexion.Close();
            bool incomplete = nombre > 0 ? true : false;

            return incomplete;
        }

        /// <summary>
        /// Retourne le nombre total de tins de type {libelleTin} possédés par STX à la date la plus récente
        /// </summary>
        /// <param name="libelleTin">Libelle du type de tin</param>
        /// <returns></returns>
        public int getNombreTinsMaximumDuType(string libelleTin)
        {
            _connexion.Open();
            _requete = new MySqlCommand("select nombre from HistoriqueOutillage "+
                                        "inner join Outillage on Outillage.id = HistoriqueOutillage.idOutillage "+
                                        "where libelle = @libelle "+
                                        "order by dateMiseAJour desc limit 0, 1", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@libelle", libelleTin));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            int nombre = Convert.ToInt32(dr["nombre"]);
            dr.Close();
            _connexion.Close();

            return nombre;
        }

        /// <summary>
        /// Retourne TRUE si une réservation de tins de type {typeTin} existe déjà pour le chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc">Bloc visé par la réservation</param>
        /// <param name="pm">TRUE si le chantier est PM, FALSE si le chantier est BORD</param>
        /// <param name="typeTin">Libelle du type de tin</param>
        /// <returns></returns>
        public bool reservationTinDejaExistante(Bloc bloc, bool pm, string typeTin)
        {
            string stringRequete = "select count(*) as nombre from reserveroutillage " +
                                     "inner join outillage on outillage.id = reserveroutillage.idoutillage " +
                                     "where idbloc = @idBloc and pm = @pm and libelle=@libelle";
            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            _requete.Parameters.Add(new MySqlParameter("@libelle", typeTin));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            bool dejaExistante = Convert.ToBoolean(Convert.ToInt32(dr["nombre"]));
            dr.Close();
            _connexion.Close();

            return dejaExistante;
        }

        /// <summary>
        /// Retourne le nombre d'équerres ayant pour type {typeEquerre}
        /// </summary>
        /// <param name="typeEquerre">Repère du type d'équerre [Ex : E400]</param>
        /// <returns></returns>
        public int getNombreEquerresDuType(string repereTypeEquerre)
        {
            string stringRequete = "select count(*) as nombre from equerre " +
                                     "inner join typeequerre on typeequerre.id = equerre.idtypeequerre " +
                                     "where typeequerre.repere=@repere";
            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@repere", repereTypeEquerre));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            int nombreEquerres = Convert.ToInt32(dr["nombre"]);
            dr.Close();
            _connexion.Close();

            return nombreEquerres;
        }

        /// <summary>
        /// Retourne le nombre d'utilisateurs ayant la main simultanément
        /// </summary>
        /// <returns></returns>
        public int getUtilisateursAyantLaMain()
        {
            _connexion.Open();
            _requete = new MySqlCommand("select count(*) as nombre from Utilisateur where main=1", _connexion);
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            int nombre = Convert.ToInt32(dr["nombre"]);
            dr.Close();
            _connexion.Close();
            return nombre;
        }

        /// <summary>
        /// Retourne la liste des repères de toutes les équerres de type {repereTypeEquerre}
        /// </summary>
        /// <param name="repereTypeEquerre">Repere du type d'équerre [Ex : "E400"]</param>
        /// <returns></returns>
        public List<string> getReperesEquerresDuType(string repereTypeEquerre)
        {
            List<string> listeReperes = new List<string>();
            string stringRequete = "select Equerre.repere from equerre "+
                                    "inner join typeEquerre on typeequerre.id = Equerre.idTypeEquerre "+
                                    "where typeEquerre.repere=@repere";
            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@repere", repereTypeEquerre));
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeReperes.Add((string)dr["repere"]);
            }
            dr.Close();
            _connexion.Close();

            return listeReperes;
        }

        /// <summary>
        /// Retourne la liste des repères de toutes les équerres
        /// </summary>
        /// <returns></returns>
        public List<string> getRepereAllEquerres()
        {
            List<string> listeReperes = new List<string>();
            _connexion.Open();
            _requete = new MySqlCommand("select repere from Equerre", _connexion);
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeReperes.Add((string)dr["repere"]);
            }
            dr.Close();
            _connexion.Close();

            return listeReperes;
        }

        /// <summary>
        /// Retourne l'id de la propriété répondant la mieux au besoin (hauteur, charge) 
        /// </summary>
        /// <param name="repereTypeEquerre">Repere du type d'équerre</param>
        /// <param name="hauteur">Hauteur reherchée</param>
        /// <param name="charge">Charge recherchée</param>
        /// <returns></returns>
        public int getIdPropriete(string repereTypeEquerre, int hauteur, int charge)
        {
            int id = 0;
            _connexion.Open();
            _requete = new MySqlCommand("select Propriete.id from Propriete " +
                                        "inner join TypeEquerre on Typeequerre.id = Propriete.idTypeEquerre " +
                                        "where TypeEquerre.repere = @repere and hauteur between @hauteurMin and @hauteurMax and charge >= @charge order by hauteur desc", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@repere", repereTypeEquerre));
            _requete.Parameters.Add(new MySqlParameter("@hauteurMin", hauteur-800));
            _requete.Parameters.Add(new MySqlParameter("@hauteurMax", hauteur-100));
            _requete.Parameters.Add(new MySqlParameter("@charge", charge));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            id = Convert.ToInt32(dr["id"]);
            dr.Close();
            _connexion.Close();

            return id;
        }

        /// <summary>
        /// Retourne l'équerre ayant pour repère {repere}
        /// </summary>
        /// <param name="repere">Repere de l'équerre recherchée</param>
        /// <returns></returns>
        public Equerre getEquerre(string repere)
        {
            _connexion.Open();
            _requete = new MySqlCommand("select * from Equerre where repere=@repere", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@repere", repere));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            Equerre equerre = new Equerre((int)dr["id"], (string)dr["repere"], (string)dr["remarque"]);
            _connexion.Close();

            return equerre;
        }

        /// <summary>
        /// Retourne une ArrayList représentant le taux d'occupation des équerres de type {typeEquerre}
        /// </summary>
        /// <param name="typeEquerre">Repère du type d'équerre</param>
        /// <returns></returns>
        public ArrayList getStatsTauxOccupation(string typeEquerre)
        {
            ArrayList tauxOccupation = new ArrayList();     // [0] : 2018       [1] : 25(%)     [2] : 2019 ...
            List<DateTime> datesDebut = new List<DateTime>();
            List<DateTime> datesFin = new List<DateTime>();
            int nombreEquerresDuType = getNombreEquerresDuType(typeEquerre);
            string stringRequete = "select dateDebut,dateFin from pret "+
                                    "inner join Equerre on Equerre.id = Pret.idEquerre "+
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                    "where TypeEquerre.repere = @typeEquerre and year(dateFin) <= year(current_timestamp()) + 1 and year(dateDebut) >= year(current_timestamp()) - 3 "+
                                    "union all "+
                                    "select DATE_ADD(dateDebutPm, interval - 1 WEEK) as 'dateDebut', DATE_ADD(dateFinPm, interval 1 WEEK) as 'dateFin' from ReserverEquerre "+
                                    "inner join equerre on Equerre.id = ReserverEquerre.idEquerre "+
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                    "inner join Bloc on Bloc.id = ReserverEquerre.idBloc "+
                                    "where TypeEquerre.repere = @typeEquerre and year(dateFinPm) <= year(current_timestamp()) + 1 and year(dateDebutPm) >= year(current_timestamp()) - 3 "+
                                    "and pm = 1 " +
                                    "union all "+
                                    "select DATE_ADD(dateFinPm, interval - 2 WEEK) as 'dateDebut', DATE_ADD(dateFinBord, interval 1 WEEK) as 'dateFin' from ReserverEquerre "+
                                    "inner join equerre on Equerre.id = ReserverEquerre.idEquerre "+
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                    "inner join Bloc on Bloc.id = ReserverEquerre.idBloc "+
                                    "where TypeEquerre.repere = @typeEquerre and year(dateFinBord) <= year(current_timestamp()) + 1 and year(dateFinPm) >= year(current_timestamp()) - 3 "+
                                    "and year(dateFinBord) <> 1 and pm = 0";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@typeEquerre", typeEquerre));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                datesDebut.Add( Convert.ToDateTime(dr["dateDebut"]) );
                datesFin.Add(Convert.ToDateTime(dr["dateFin"]));
            }
            dr.Close();
            _connexion.Close();

            List<int> annees = new List<int>();
            List<int> nombreResas = new List<int>();
            List<double> sommeJours = new List<double>();

            // Pour chaque duo de dates récupérées (dateDebut / dateFin)
            for(int k=0; k<datesDebut.Count; k++)
            {
                // Si l'année de dateDebut n'est pas contenue dans la liste annees, on l'ajoute à celle-ci
                if (!annees.Contains(datesDebut[k].Year))
                {
                    annees.Add(datesDebut[k].Year);
                    // et on valorise son nombre de réservations ainsi que sa somme de jours réservés à 0 (zéro)
                    nombreResas.Add(0);
                    sommeJours.Add(0);
                }
                // Même chose avec la date de fin
                if (!annees.Contains(datesFin[k].Year))
                {
                    annees.Add(datesFin[k].Year);
                    nombreResas.Add(0);
                    sommeJours.Add(0);
                }

                // Si la date de début et la date de fin ont la même année
                if (datesDebut[k].Year == datesFin[k].Year)
                {
                    // On incrémente la somme des jours réservés de l'année en question par le nombre de jours séparant les deux dates
                    sommeJours[annees.IndexOf(datesDebut[k].Year)] += (datesFin[k] - datesDebut[k]).TotalDays;
                    // L'année voit son nombre de réservations augmenter de 1
                    nombreResas[annees.IndexOf(datesDebut[k].Year)]++;
                }
                else
                {
                    // Si la date de début et la date de fin n'ont pas la même année
                    // Ex nombre de jours réservés :
                    // Du 2018-05-10 au 2025-10-27 = 2018-05-01 au 2018-12-31 + 2025-01-01 au 2025-10-27 + (2025-2018-1) * 365

                    // Somme des jours séparant la date de début et le dernier jour de l'année de début
                    sommeJours[annees.IndexOf(datesDebut[k].Year)] += (new DateTime(datesDebut[k].Year, 12, 31) - datesDebut[k]).TotalDays;
                    nombreResas[annees.IndexOf(datesDebut[k].Year)]++;

                    // Somme des jours séparants le premier jour de l'année de fin et la date de fin
                    sommeJours[annees.IndexOf(datesFin[k].Year)] += (datesFin[k] - new DateTime(datesFin[k].Year, 01, 01)).TotalDays;
                    nombreResas[annees.IndexOf(datesFin[k].Year)]++;

                    // Somme des jours compris entre les années d'écart des dates de fin et de début
                    for(int m=datesDebut[k].Year+1; m<datesFin[k].Year; m++)
                    {
                        if (!annees.Contains(m))
                        {
                            annees.Add(m);
                            nombreResas.Add(0);
                        }
                        sommeJours[annees.IndexOf(m)] += 365;
                        nombreResas[annees.IndexOf(m)]++;
                    }

                }
            }

            // On ajoute au taux d'occupation les années et les pourcentages d'occupations associés
            for(int k=0; k<annees.Count; k++)
            {
                tauxOccupation.Add(annees[k]);
                tauxOccupation.Add(Math.Round((sommeJours[k] / (365 * nombreEquerresDuType)) * 100, 1));
            }

            return tauxOccupation;
        }

        /// <summary>
        /// Retourne une ArrayList représentant le nombre de réservations par mois des équerres de type {typeEquerre} 
        /// </summary>
        /// <param name="typeEquerre"></param>
        /// <returns></returns>
        public ArrayList getStatsNombreReservationsParMois(string typeEquerre)
        {
            ArrayList nombreReservations = new ArrayList();     // [0] : 2018       [1] à [12] : nombre     [13] : 2019
            List<DateTime> datesDebut = new List<DateTime>();
            List<DateTime> datesFin = new List<DateTime>();

            string stringRequete = "select DATE_ADD(dateDebutPm, interval - 1 WEEK) as 'dateDebut' ,DATE_ADD(dateFinPm, interval 1 WEEK) as 'dateFin' from ReserverEquerre "+
                                    "inner join equerre on Equerre.id = ReserverEquerre.idEquerre "+
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                    "inner join Bloc on Bloc.id = ReserverEquerre.idBloc "+
                                    "where TypeEquerre.repere = @typeEquerre and year(dateFinPm) <= year(current_timestamp()) + 1 and year(dateDebutPm) >= year(current_timestamp()) - 3 " +
                                    "and pm = 1 "+
                                    "union all "+
                                    "select DATE_ADD(dateFinPm, interval - 2 WEEK) as 'dateDebut', DATE_ADD(dateFinBord, interval 1 WEEK) as 'dateFin' from ReserverEquerre "+
                                    "inner join equerre on Equerre.id = ReserverEquerre.idEquerre "+
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                    "inner join Bloc on Bloc.id = ReserverEquerre.idBloc "+
                                    "where TypeEquerre.repere = @typeEquerre and year(dateFinBord) <= year(current_timestamp()) + 1 and year(dateFinPm) >= year(current_timestamp()) - 3 " +
                                    "and year(dateFinBord) <> 1 and pm = 0";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@typeEquerre", typeEquerre));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                datesDebut.Add(Convert.ToDateTime(dr["dateDebut"]));
                datesFin.Add(Convert.ToDateTime(dr["dateFin"]));
            }
            dr.Close();
            _connexion.Close();

            List<int> annees = new List<int>();         // [0] : 2018   [1] : 2017   (N'est pas triée) 
            int[,] nombreResasMois = new int[5,12];     // table de 5*12 (5 = années & 12 = mois)
            for(int k=0; k<5; k++)
            {
                for(int m=0; m<12; m++)
                {
                    nombreResasMois[k, m] = 0;          // Valorisation de toutes les valeurs de la table à zéro
                }
            }

            for (int k = 0; k < datesDebut.Count; k++)
            {
                // Pour chaque occurence renvoyée par la requête

                // On ajoute les années des variables de travail à la liste années
                if (!annees.Contains(datesDebut[k].Year))
                {
                    annees.Add(datesDebut[k].Year);
                }
                if (!annees.Contains(datesFin[k].Year))
                {
                    annees.Add(datesFin[k].Year);
                }

                // Si la réservation se fait dans un seul et même mois (Ex : DD = 2017-02-17     DF = 2017-02-24)
                if (datesDebut[k].Year == datesFin[k].Year && datesDebut[k].Month == datesFin[k].Month)
                {
                    // On incrémente le nombre de réservations pour le mois de l'année
                    nombreResasMois[annees.IndexOf(datesDebut[k].Year),datesDebut[k].Month-1]++;
                }
                else
                {
                    // Si la réservation s'étend sur au moins 2 années différentes

                    // On incrémente le nombre de réservations pour le mois de l'année de début
                    nombreResasMois[annees.IndexOf(datesDebut[k].Year), datesDebut[k].Month - 1]++;
                    // On incrémente le nombre de réservations pour le mois de l'année de fin
                    nombreResasMois[annees.IndexOf(datesFin[k].Year), datesFin[k].Month - 1]++;


                    if (datesDebut[k].Year != datesFin[k].Year)
                    {
                        // Ex : DD = 2017-02-17     DF = 2020-07-15
                        // On incrémente le nombre de réservations par mois de 2017-03 à 2017-12
                        for (int n = datesDebut[k].Month  ; n < 12; n++)
                        {
                            nombreResasMois[annees.IndexOf(datesDebut[k].Year), n]++;
                        }
                        // On incrémente le nombre de réservations par mois de 2020-01 à 2020-06
                        for (int p = 0; p < datesFin[k].Month-1; p++)
                        {
                            nombreResasMois[annees.IndexOf(datesFin[k].Year), p]++;
                        }
                    }
                    else
                    {
                        // Ex : DD = 2017-02-17     DF = 2017-09-23)
                        // On incrémente le nombre de réservations par mois de 2017-03 à 2017-08
                        for (int v = datesDebut[k].Month ; v < datesFin[k].Month-1; v++)
                        {
                            nombreResasMois[annees.IndexOf(datesDebut[k].Year), v]++;
                        }
                    }

                    // Pour toutes les années comprises entre celle de DD et DF
                    for (int m = datesDebut[k].Year + 1; m < datesFin[k].Year; m++)
                    {
                        if (!annees.Contains(m))
                        {
                            annees.Add(m);
                        }
                        // On incrémente le nombre de réservations de tous les mois de l'année
                        for (int numeroMois = 0; numeroMois<12; numeroMois++)
                        {
                            nombreResasMois[annees.IndexOf(m), numeroMois]++;
                        }
                    }
                }
            }

            // On complète de manière à avoir toutes les années même si il n'y a aucune réservation pour l'année
            int anneeActuelle = DateTime.Now.Year;
            for(int k=anneeActuelle-3; k<anneeActuelle+2; k++) { if (!annees.Contains(k)) { annees.Add(k); } }

            // On insère les valeurs obtenues dans l'ArrayList nombreReservations selon le schéma établi (cf : voir première ligne de la fonction)
            for (int k = 0; k < 5; k++)
            {
                nombreReservations.Add(annees[k]);
                for (int m = 0; m < 12; m++)
                {
                    nombreReservations.Add(nombreResasMois[annees.IndexOf(annees[k]),m]);
                }
            }

            return nombreReservations;
        }

        /// <summary>
        /// Retourne une ArrayList représentant le nombre de travaux par année des équerres de type {typeEquerre}
        /// </summary>
        /// <param name="typeEquerre"></param>
        /// <returns></returns>
        public ArrayList getStatsNombreTravaux(string typeEquerre)
        {
            ArrayList nombreTravaux = new ArrayList();     // [0] : 2018       [1] : Nombre     [2] : 2019

            string stringRequete =  "select year(datedebut) as 'annee', count(*) as 'nombre' from TravauxEquerre "+
                                    "inner join Equerre on Equerre.id = TravauxEquerre.idEquerre "+
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                    "where TypeEquerre.repere = @typeEquerre and year(dateDebut) <= year(current_timestamp()) + 1 and year(dateDebut) >= year(current_timestamp()) - 3 " +
                                    "group by year(dateDebut)";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@typeEquerre", typeEquerre));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                nombreTravaux.Add(Convert.ToInt32(dr["annee"]));
                nombreTravaux.Add(Convert.ToInt32(dr["nombre"]));
            }
            dr.Close();
            _connexion.Close();


            return nombreTravaux;
        }

        /// <summary>
        /// Retourne une ArrayList représentant le pourcentage des blocs d'un navire ayant eu au moins une réservation d'équerre de type {typeEquerre}
        /// </summary>
        /// <param name="typeEquerre"></param>
        /// <returns></returns>
        public ArrayList getStatsBlocsAyantEuTypeEquerre(string typeEquerre)
        {
            ArrayList blocsAyantEyTypeEquerre = new ArrayList();     // [0] : G34       [1] : 25 (%)        [2] : K34

            string stringRequete = "select nom, (nombreTotalResas/nombreBlocsNavire)*100 as 'pourcentage' from ( "+
                                   "select nom as 'nomNavire', count(*) as 'nombreBlocsNavire' from Bloc "+
                                   "inner join navire on navire.id = bloc.idNavire "+
                                   "group by idNavire) as table1 " +
                                   "inner join ( "+
                                   "select nom, count(*) as 'nombreTotalResas' from ReserverEquerre "+
                                   "inner join bloc on bloc.id = ReserverEquerre.idBloc "+
                                   "inner join Equerre on Equerre.id = ReserverEquerre.idEquerre "+
                                   "inner join navire on navire.id = bloc.idNavire "+
                                   "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                   "where TypeEquerre.repere = @typeEquerre "+
                                   "group by idNavire) as table2 "+
                                   "on table1.nomNavire = table2.nom";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@typeEquerre", typeEquerre));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                blocsAyantEyTypeEquerre.Add(dr["nom"]);
                blocsAyantEyTypeEquerre.Add(Convert.ToDouble(dr["pourcentage"]));
            }
            dr.Close();
            _connexion.Close();

            return blocsAyantEyTypeEquerre;
        }

        /// <summary>
        /// Retourne une ArrayList représentant le nombre de prêts par années d'équerres du type {typeEquerre}
        /// </summary>
        /// <param name="typeEquerre"></param>
        /// <returns></returns>
        public ArrayList getStatsNombrePrets(string typeEquerre)
        {
            ArrayList nombrePrets = new ArrayList();     // [0] : 2018       [1] : Nombre       [2] : 2019

            string stringRequete = "select year(datedebut) as 'annee',count(*) as 'nombre' from pret "+
                                   "inner join Equerre on Equerre.id = Pret.idEquerre "+
                                   "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                   "where TypeEquerre.repere = @typeEquerre and idBloc is null "+
                                   "and year(dateFin) <= year(current_timestamp()) + 1 and year(dateDebut) >= year(current_timestamp()) - 3 "+
                                   "group by year(dateDebut)";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@typeEquerre", typeEquerre));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                nombrePrets.Add(Convert.ToInt32(dr["annee"]));
                nombrePrets.Add(Convert.ToInt32(dr["nombre"]));
            }
            dr.Close();
            _connexion.Close();


            return nombrePrets;

        }

        /// <summary>
        /// Retourne une ArrayList représentant le nombre de prêts,associés à un bloc, par années d'équerres du type {typeEquerre}
        /// </summary>
        /// <param name="typeEquerre"></param>
        /// <returns></returns>
        public ArrayList getStatsNombrePretsBloc(string typeEquerre)
        {
            ArrayList nombrePrets = new ArrayList();     // [0] : Année       [1] : Nombre      [2] : 2019

            string stringRequete =  "select year(datedebut) as 'annee',count(*) as 'nombre' from pret "+
                                    "inner join Equerre on Equerre.id = Pret.idEquerre "+
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                    "where TypeEquerre.repere =@typeEquerre  and idBloc is not  null "+
                                    "and year(dateFin) <= year(current_timestamp()) + 1 and year(dateDebut) >= year(current_timestamp()) - 3 "+
                                    "group by year(dateDebut)";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@typeEquerre", typeEquerre));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                nombrePrets.Add(Convert.ToInt32(dr["annee"]));
                nombrePrets.Add(Convert.ToInt32(dr["nombre"]));
            }
            dr.Close();
            _connexion.Close();

            return nombrePrets;
        }

        /// <summary>
        /// Retourne l'id du type de tin ayant pour repere {typeTin}
        /// </summary>
        /// <param name="typeTin">Repere du type de tin recherché</param>
        /// <returns></returns>
        public int getIdTypeTin(string typeTin)
        {
            _connexion.Open();
            _requete = new MySqlCommand("select id from Outillage where libelle=@libelle", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@libelle", typeTin));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            int id = Convert.ToInt32(dr["id"]);
            dr.Close();
            _connexion.Close();

            return id;
        }

        /// <summary>
        /// Retourne la liste de tous les outillages
        /// </summary>
        /// <returns></returns>
        public List<string> getListeOutillages()
        {
            List<string> listeLibelles = new List<string>();
            _connexion.Open();
            _requete = new MySqlCommand("select libelle from Outillage", _connexion);
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeLibelles.Add((string)dr["libelle"]);
            }
            dr.Close();
            _connexion.Close();
            return listeLibelles;
        }

        /// <summary>
        /// Retourne une Liste représentant le type et le nombre de tins réservés sur le chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        public List<string> getTinsReserves(Bloc bloc, bool pm)
        {
            List<string> listeData = new List<string>();
            string stringRequete = "select libelle,nombre from reserveroutillage " +
                        "inner join outillage on outillage.id = reserveroutillage.idoutillage "+
                        "inner join bloc on bloc.id = reserveroutillage.idBloc " +
                        "where bloc.id = @idBloc and reserveroutillage.pm = @pm";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeData.Add(dr["libelle"].ToString());
                listeData.Add(dr["nombre"].ToString());
            }
            dr.Close();
            _connexion.Close();

            return listeData;
        }

        /// <summary>
        /// Retourne l'équerre ayant pour id {id}
        /// </summary>
        /// <param name="id">Id de l'équerre</param>
        /// <returns></returns>
        public Equerre getEquerre(int id)
        {
            _connexion.Open();
            _requete = new MySqlCommand("select * from Equerre where id=@id", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@id", id));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            Equerre equerre = new Equerre((int)dr["id"], (string)dr["repere"], (string)dr["remarque"]);
            _connexion.Close();
            return equerre;
        }

        /// <summary>
        /// Retourne le type d'équerre de l'équerre ayant pour repere {repereEquerre}
        /// </summary>
        /// <param name="repereEquerre"></param>
        /// <returns></returns>
        public TypeEquerre getTypeEquerre(string repereEquerre)
        {
            string stringRequete = "select TypeEquerre.id, TypeEquerre.repere, TypeEquerre.numeroPlan, TypeEquerre.semblable, " +
                "TypeEquerre.reglageHauteur, TypeEquerre.cheminImage from TypeEquerre " +
                "inner join Equerre on Equerre.idTypeEquerre = TypeEquerre.id " +
                "where Equerre.repere=@repere";
            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@repere", repereEquerre));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            TypeEquerre typeEquerre = new TypeEquerre(Convert.ToInt32(dr["id"]), (string)dr["repere"], (string)dr["numeroPlan"], Convert.ToBoolean(dr["semblable"]), Convert.ToInt32(dr["reglageHauteur"]), (string)dr["cheminImage"]);
            dr.Close();
            _connexion.Close();

            typeEquerre.Proprietes = getProprietes(typeEquerre);

            return typeEquerre;
        }

        /// <summary>
        /// Retourne le type d'équerre ayant pour repere {repereTypeEquerre}
        /// </summary>
        /// <param name="repereTypeEquerre"></param>
        /// <returns></returns>
        public TypeEquerre getTypeEquerreViaRepere(string repereTypeEquerre)
        {
            _connexion.Open();
            _requete = new MySqlCommand("select * from typeequerre where repere=@repere", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@repere", repereTypeEquerre));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            TypeEquerre typeEquerre = new TypeEquerre(Convert.ToInt32(dr["id"]), (string)dr["repere"], (string)dr["numeroPlan"], Convert.ToBoolean(dr["semblable"]), Convert.ToInt32(dr["reglageHauteur"]), (string)dr["cheminImage"]);
            dr.Close();
            _connexion.Close();

            typeEquerre.Proprietes = getProprietes(typeEquerre);

            return typeEquerre;
        }

        /// <summary>
        /// Retourne le repère de type d'équerre de l'équerre ayant pour id {idEquerre}
        /// </summary>
        /// <param name="idEquerre"></param>
        /// <returns></returns>
        public string getRepereTypeEquerre(int idEquerre)
        {
            string stringRequete = "select TypeEquerre.repere from TypeEquerre " +
                "inner join Equerre on Equerre.idTypeEquerre = TypeEquerre.id " +
                "where Equerre.id=@id";
            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@id", idEquerre));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            string repere = dr["repere"].ToString();
            dr.Close();
            _connexion.Close();

            return repere;
        }

        /// <summary>
        /// Retourne le type d'équerre de l'équerre ayant pour id {idEquerre}
        /// </summary>
        /// <param name="idEquerre"></param>
        /// <returns></returns>
        public TypeEquerre getTypeEquerre(int idEquerre)
        {
            string stringRequete = "select TypeEquerre.id, TypeEquerre.repere, TypeEquerre.numeroPlan, TypeEquerre.semblable, " +
                "TypeEquerre.reglageHauteur, TypeEquerre.cheminImage from TypeEquerre " +
                "inner join Equerre on Equerre.idTypeEquerre = TypeEquerre.id " +
                "where Equerre.id=@id";
            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@id", idEquerre));
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            TypeEquerre typeEquerre = new TypeEquerre(Convert.ToInt32(dr["id"]), (string)dr["repere"], (string)dr["numeroPlan"], Convert.ToBoolean(dr["semblable"]), Convert.ToInt32(dr["reglageHauteur"]), (string)dr["cheminImage"]);
            dr.Close();
            _connexion.Close();

            typeEquerre.Proprietes = getProprietes(typeEquerre);

            return typeEquerre;
        }

        /// <summary>
        /// Retourne la liste des libellés de tous les moyens de transport
        /// </summary>
        /// <param name="classeTransport"></param>
        /// <returns></returns>
        public List<string> getTransports(string classeTransport)
        {
            List<string> listeTransports = new List<string>();
            _connexion.Open();
            _requete = new MySqlCommand("select libelle from transport where classe=@classe", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@classe",classeTransport));
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeTransports.Add((string)dr["libelle"]);
            }
            dr.Close();
            _connexion.Close();

            return listeTransports;
        }

        /// <summary>
        /// Retourne une ArrayList représentant toutes les équerres prêtées au chantier {pm} du bloc {bloc} ainsi que la hauteur associée au prêt, la charge etc...
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        public ArrayList getEquerresPreteesBlocPourPdf(Bloc bloc, bool pm)
        {
            ArrayList arrayList = new ArrayList();  // [0] : repèreEquerre      [1] : Hauteur       [2] : Charge        [3] : Classe        [4] : Transport(S)      [5] : dateFin
            List<string> listeClasses = new List<string>();

            string classe = "";
            string stringRequete = "select Equerre.repere, hauteur, charge, classe, datefin from Pret " +
                                    "inner join Bloc on Bloc.id = Pret.idBloc " +
                                    "inner join Equerre on Equerre.id = Pret.idEquerre " +
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre " +
                                    "inner join Propriete on Propriete.idTypeEquerre = TypeEquerre.id " +
                                    "inner join Deplacer on Deplacer.idPropriete = Propriete.id " +
                                    "inner join Transport on Transport.id = Deplacer.idTransport " +
                                    "where idBloc = @idBloc and pm = @pm";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                arrayList.Add((string)dr["repere"]);
                arrayList.Add(dr["hauteur"].ToString());
                arrayList.Add(dr["charge"].ToString());
                classe = (string)dr["classe"];
                arrayList.Add(classe);
                listeClasses.Add(classe);
                arrayList.Add(dr["datefin"].ToString());
            }
            dr.Close();

            // Insertion des moyens de transport dans l'ArrayList
            for (int k = 0; k < listeClasses.Count; k++)
            {
                _connexion.Close();
                string[] transports = getTransports(listeClasses[k]).ToArray();
                arrayList.Insert((k*6)+4, transports);
                _connexion.Open();
            }

            _connexion.Close();

            return arrayList;


        }

        /// <summary>
        /// Retourne une ArrayList représentant toutes les équerres réservées sur le chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        public ArrayList getEquerresReserveesBlocPourPdf(Bloc bloc, bool pm)
        {
            ArrayList arrayList = new ArrayList();      // [0] : repèreEquerre      [1] : Hauteur       [2] : Charge        [3] : Classe        [4] : Transport(S)   
            List<string> listeClasses = new List<string>();

            string classe = "";
            string stringRequete = "select Equerre.repere, hauteur, charge, classe from ReserverEquerre "+
                                    "inner join Equerre on Equerre.id = ReserverEquerre.idEquerre "+
                                    "inner join TypeEquerre on TypeEquerre.id = Equerre.idTypeEquerre "+
                                    "inner join Propriete on Propriete.id = ReserverEquerre.idPropriete "+
                                    "inner join Deplacer on Deplacer.idPropriete = Propriete.id "+
                                    "inner join Transport on Transport.id = Deplacer.idTransport "+
                                    "where idbloc = @idBloc and pm = @pm";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                arrayList.Add((string)dr["repere"]);
                arrayList.Add(dr["hauteur"].ToString());
                arrayList.Add(dr["charge"].ToString());
                classe = (string)dr["classe"];
                arrayList.Add(classe);
                listeClasses.Add(classe);
            }
            dr.Close();

            // Insertion des moyens de transport dans l'ArrayList
            for (int k=0; k<listeClasses.Count; k++)
            {
                _connexion.Close();
                string[] transports = getTransports(listeClasses[k]).ToArray();
                arrayList.Insert(((k + 1) * 5) - 1, transports);
                _connexion.Open();
            }

            _connexion.Close();

            return arrayList;
        }

        /// <summary>
        /// Retourne la liste des propriétés des équerres du type {typeEquerre}
        /// </summary>
        /// <param name="typeEquerre"></param>
        /// <returns></returns>
        public List<Propriete> getProprietes(TypeEquerre typeEquerre)
        {
            List<Propriete> listeProprietes = new List<Propriete>();
            List<int> listeIdTransports = new List<int>();
            List<Transport> listeTransports = getTransports(typeEquerre);

            string stringRequete = "select Propriete.id, Propriete.hauteur, Propriete.charge, Deplacer.idTransport from Propriete " +
                "inner join Deplacer on Deplacer.idPropriete = Propriete.id " +
                "inner join Transport on Transport.id = Deplacer.idTransport " +
                "where idTypeEquerre=@idTypeEquerre";


            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idTypeEquerre", typeEquerre.Id));
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeProprietes.Add(new Propriete((int)dr["id"], (int)dr["hauteur"], Convert.ToDecimal(dr["charge"])));
                listeIdTransports.Add((int)dr["idTransport"]);
            }
            dr.Close();
            _connexion.Close();

            for (int k = 0; k < listeIdTransports.Count; k++)
            {
                for (int m = 0; m < listeTransports.Count; m++)
                {
                    if (listeTransports[m].Id == listeIdTransports[k]) { listeProprietes[k].Transport = listeTransports[m]; }
                }
            }


            return listeProprietes;
        }

        /// <summary>
        /// Retourne la liste des transports pouvant être utilisés pour déplacer des équerres de type {typeEquerre}
        /// </summary>
        /// <param name="typeEquerre"></param>
        /// <returns></returns>
        public List<Transport> getTransports(TypeEquerre typeEquerre)
        {
            List<Transport> listeTransports = new List<Transport>();

            string stringRequete = "select Transport.id, Transport.classe, Transport.libelle from Transport " +
                "inner join Deplacer on Deplacer.idTransport = Transport.id " +
                "inner join Propriete on Propriete.id = Deplacer.idPropriete " +
                "inner join TypeEquerre on TypeEquerre.id = Propriete.idTypeEquerre " +
                "where TypeEquerre.id=@idTypeEquerre " +
                "group by Transport.id";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idTypeEquerre", typeEquerre.Id));
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeTransports.Add(new Transport((int)dr["id"], (string)dr["classe"], (string)dr["libelle"]));
            }
            dr.Close();
            _connexion.Close();

            return listeTransports;
        }

        /// <summary>
        /// Retourne la liste des repères de tous les types d'équerres
        /// </summary>
        /// <returns></returns>
        public List<string> getAllReperesTypesEquerres()
        {
            List<string> listeReperes = new List<string>();

            _connexion.Open();
            _requete = new MySqlCommand("select repere from typeequerre order by repere", _connexion);
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeReperes.Add((string)dr["repere"]);
            }
            dr.Close();
            _connexion.Close();

            return listeReperes;
        }

        /// <summary>
        /// Valorise les données membres de la classe statique PersistanceParametres
        /// </summary>
        public void getPersistanceParametres()
        {
            _connexion.Open();
            _requete = new MySqlCommand("select * from PersistanceParametres", _connexion);
            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            PersistanceParametres.CheminDossierSauvegardePdf = (string)dr["cheminDossierSauvegardePdf"];
            PersistanceParametres.CheminFichierTxtConnexionBdd = (string)dr["cheminFichierTxtConnexionBdd"];
            PersistanceParametres.CheminFichierExtractionC212P = (string)dr["cheminFichierExtractionC212P"];
            PersistanceParametres.CheminImageErreur = (string)dr["cheminImageErreur"];
            PersistanceParametres.DateMiseAJour = (DateTime)dr["dateMiseAJour"];
            dr.Close();
            _connexion.Close();
        }

        /// <summary>
        /// Retourne la liste des Id des équerres disponibles pour le chantier {pm} du bloc {bloc}, sans contrainte de hauteur ni de charge
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        public List<int> getEquerresDisponibles(Bloc bloc, bool pm)
        {
            return getEquerresDisponibles(bloc, pm, -1, -1);
        }

        /// <summary>
        /// Retourne le nombre de tins de type {libelleTin} réservés pour le chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="libelleTin"></param>
        /// <returns></returns>
        public int nombreDeTinsReserves(Bloc bloc, bool pm, string libelleTin)
        {
            DateTime dateModifiee, dateDebut, dateFin;
            string stringRequete, attribut, complementAttribut;
            if (pm)
            {
                dateModifiee = getDateModifiee(bloc.DateDebutPm, -1);
                dateDebut = getPremierJourSemaine(dateModifiee.Year, getSemaine(dateModifiee));
                dateModifiee = getDateModifiee(bloc.DateFinPm, 1);
                dateFin = getPremierJourSemaine(dateModifiee.Year, getSemaine(dateModifiee));
                attribut = "dateDebutPm";
                complementAttribut = "Pm";
            }
            else
            {
                dateModifiee = getDateModifiee(bloc.DateFinPm, -2);
                dateDebut = getPremierJourSemaine(dateModifiee.Year, getSemaine(dateModifiee));
                dateModifiee = getDateModifiee(bloc.DateFinBord, 1);
                dateFin = getPremierJourSemaine(dateModifiee.Year, getSemaine(dateModifiee));
                attribut = "dateFinPm";
                complementAttribut = "Bord";
            }


            stringRequete = "select sum(nombre) as nombre from reserveroutillage " +
                            "inner join bloc on bloc.id = reserveroutillage.idBloc " +
                            "inner join Outillage on Outillage.id = reserveroutillage.idOutillage " +
                            "where (" +
                            "		(" + attribut + " >= @dateDebut and dateFin" + complementAttribut + " <= @dateFin) " +
                            "	or  (" + attribut + " <= @dateDebut and dateFin" + complementAttribut + " >= @dateFin) " +
                            "	or  (" + attribut + " <= @dateDebut and dateFin" + complementAttribut + " <= @dateFin and dateFin" + complementAttribut + " >= @dateDebut) " +
                            "	or  (" + attribut + " >= @dateDebut and dateFin" + complementAttribut + " >= @dateFin and " + attribut + " <= @dateFin) " +
                            ")and Outillage.libelle = @libelle";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@dateDebut", dateDebut));
            _requete.Parameters.Add(new MySqlParameter("@dateFin", dateFin));
            _requete.Parameters.Add(new MySqlParameter("@libelle", libelleTin));

            MySqlDataReader dr = _requete.ExecuteReader();
            dr.Read();
            int nombre = Convert.IsDBNull(dr["nombre"]) ? 0 : Convert.ToInt32(dr["nombre"]);
            dr.Close();
            _connexion.Close();
            return nombre;
        }

        /// <summary>
        /// Retourne la liste des Id des équerres disponibles pour le chantier {pm} du bloc {bloc} répondant aux exigences en matière de hauteur et de charge
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="hauteur"></param>
        /// <param name="charge"></param>
        /// <returns></returns>
        public List<int> getEquerresDisponibles(Bloc bloc, bool pm, int hauteur, int charge)
        {
            List<int> listeIdEquerres = new List<int>();
            DateTime dateModifiee, dateDebut, dateFin;
            string stringRequete, attribut, complementAttribut;
            string conditionHauteurCharge = (hauteur == -1 && charge == -1) ? "" : "where hauteur between @hauteurMin and @hauteurMax and charge >= @charge ";

            if (pm)
            {
                dateModifiee = getDateModifiee(bloc.DateDebutPm, -1);
                dateDebut = getPremierJourSemaine(dateModifiee.Year, getSemaine(dateModifiee));
                dateModifiee = getDateModifiee(bloc.DateFinPm, 1);
                dateFin = getPremierJourSemaine(dateModifiee.Year, getSemaine(dateModifiee));
                attribut = "dateDebutPm";
                complementAttribut = "Pm";
            }
            else
            {
                dateModifiee = getDateModifiee(bloc.DateFinPm, -2);
                dateDebut = getPremierJourSemaine(dateModifiee.Year, getSemaine(dateModifiee));
                dateModifiee = getDateModifiee(bloc.DateFinBord, 1);
                dateFin = getPremierJourSemaine(dateModifiee.Year, getSemaine(dateModifiee));
                attribut = "dateFinPm";
                complementAttribut = "Bord";
            }

            stringRequete = "select distinct Equerre.id from Propriete "+
                            "inner join TypeEquerre on TypeEquerre.id = Propriete.idTypeEquerre "+
                            "inner join Equerre on Equerre.idTypeEquerre = TypeEquerre.id "+
                            conditionHauteurCharge+
                            "group by Equerre.id "+
                            "having Equerre.id not in ( "+
	                            "select distinct idEquerre from reserverEquerre "+
	                            "inner join Bloc on Bloc.id = reserverEquerre.idBloc "+
	                            "where "+
                                "		(" + attribut + " >= @dateDebut and dateFin" + complementAttribut + " <= @dateFin) " +
                                "	or  (" + attribut + " <= @dateDebut and dateFin" + complementAttribut + " >= @dateFin) " +
                                "	or  (" + attribut + " <= @dateDebut and dateFin" + complementAttribut + " <= @dateFin and dateFin" + complementAttribut + " >= @dateDebut) " +
                                "	or  (" + attribut + " >= @dateDebut and dateFin" + complementAttribut + " >= @dateFin and " + attribut + " <= @dateFin) " +
	                            "union "+
	                            "select Equerre.id from Equerre "+
	                            "inner join TravauxEquerre on TravauxEquerre.idEquerre = Equerre.id "+
	                            "where  "+
	                            "		(dateDebut >= @dateDebut and dateFin <= @dateFin) "+
	                            "	or  (dateDebut <= @dateDebut and dateFin >= @dateFin) "+
	                            "	or  (dateDebut <= @dateDebut and dateFin <= @dateFin and dateFin   >= @dateDebut) "+
	                            "	or  (dateDebut >= @dateDebut and dateFin >= @dateFin and dateDebut <= @dateFin) "+
	                            "union "+
	                            "select Equerre.id from Equerre "+
	                            "inner join Pret on Pret.idEquerre = Equerre.id "+
	                            "where  "+
	                            "		(dateDebut >= @dateDebut and dateFin <= @dateFin) "+
	                            "	or  (dateDebut <= @dateDebut and dateFin >= @dateFin) "+
	                            "	or  (dateDebut <= @dateDebut and dateFin <= @dateFin and dateFin   >= @dateDebut) "+
	                            "	or  (dateDebut >= @dateDebut and dateFin >= @dateFin and dateDebut <= @dateFin) "+
                            ") order by reglageHauteur,hauteur,charge";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@hauteurMin", hauteur - 800));
            _requete.Parameters.Add(new MySqlParameter("@hauteurMax", hauteur - 100));
            _requete.Parameters.Add(new MySqlParameter("@charge", charge));
            _requete.Parameters.Add(new MySqlParameter("@dateDebut", dateDebut));
            _requete.Parameters.Add(new MySqlParameter("@dateFin", dateFin));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeIdEquerres.Add(Convert.ToInt32(dr["id"]));
            }
            dr.Close();
            _connexion.Close();

            return listeIdEquerres;
        }

        /// <summary>
        /// Retourne la liste des Id des équerres réservées sur le chantier {pm} du bloc {bloc} et ayant les mêmes propriétés
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="hauteur"></param>
        /// <param name="charge"></param>
        /// <returns></returns>
        private List<int> getAllEquerresSimilairesReservees(Bloc bloc, bool pm, int hauteur, int charge)
        {
            List<int> listeIdEquerres = new List<int>();
            string stringRequete = "select idEquerre from ReserverEquerre " +
                                    "inner join Propriete on Propriete.id = ReserverEquerre.idPropriete " +
                                    "where idbloc = @idBloc and pm = @pm and hauteur = @hauteur and charge = @charge";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            _requete.Parameters.Add(new MySqlParameter("@hauteur", hauteur));
            _requete.Parameters.Add(new MySqlParameter("@charge", charge));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeIdEquerres.Add(Convert.ToInt32(dr["idEquerre"]));
            }
            dr.Close();
            _connexion.Close();

            return listeIdEquerres;
        }

        /// <summary>
        /// Retourne la liste des Id des équerres prêtées au chantier {pm} du bloc {bloc} et ayant les mêmes propriétés
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="hauteur"></param>
        /// <param name="charge"></param>
        /// <returns></returns>
        private List<int> getAllEquerresSimilairesPretees(Bloc bloc, bool pm, int hauteur, int charge)
        {
            List<int> listeIdEquerres = new List<int>();
            string stringRequete = "select idEquerre from pret " +
                                    "inner join Propriete on Propriete.id = pret.idPropriete " +
                                    "where idbloc = @idBloc and pm = @pm and hauteur = @hauteur and charge = @charge";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            _requete.Parameters.Add(new MySqlParameter("@hauteur", hauteur));
            _requete.Parameters.Add(new MySqlParameter("@charge", charge));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeIdEquerres.Add(Convert.ToInt32(dr["idEquerre"]));
            }
            dr.Close();
            _connexion.Close();

            return listeIdEquerres;
        }

        /// <summary>
        /// Retourne la liste des Id de toutes les équerres réservées sur le chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        public List<int> getAllEquerreBloc(Bloc bloc, bool pm)
        {
            List<int> listeIdEquerres = new List<int>();

            _connexion.Open();
            _requete = new MySqlCommand("select idEquerre from reserverEquerre where idBloc=@idBloc", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeIdEquerres.Add(Convert.ToInt32(dr["idEquerre"]));
            }
            dr.Close();
            _connexion.Close();

            return listeIdEquerres;
        }

        /// <summary>
        /// Retourne la liste des Id de toutes les équerres prêtées au chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        public List<int> getAllEquerrePreteesBloc(Bloc bloc, bool pm)
        {
            List<int> listeIdEquerres = new List<int>();

            _connexion.Open();
            _requete = new MySqlCommand("select idEquerre from pret where idBloc=@idBloc", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                listeIdEquerres.Add(Convert.ToInt32(dr["idEquerre"]));
            }
            dr.Close();
            _connexion.Close();

            return listeIdEquerres;
        }

        /// <summary>
        /// Retourne la liste des reprères de tous les blocs appartenants au navire ayant pour nom {nomNavire}
        /// </summary>
        /// <param name="nomNavire"></param>
        /// <returns></returns>
        public List<string> getAllBloc(string nomNavire)
        {
            List<string> repereDeTousLesBlocs = new List<string>();
            _connexion.Open();
            _requete = new MySqlCommand("select distinct repere from Bloc inner join Navire on Bloc.idNavire = Navire.id where Navire.nom=@nomNavire", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@nomNavire", nomNavire));

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                repereDeTousLesBlocs.Add((string)dr["repere"]);
            }
            dr.Close();
            _connexion.Close();

            return repereDeTousLesBlocs;
        }

        /// <summary>
        /// Retourne une liste représentant toutes les équerres réservée sur le chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="affichageSimplifie"></param>
        /// <returns></returns>
        public List<string> getAllEquerreBloc(Bloc bloc, bool pm, bool affichageSimplifie)
        {
            List<string> listeDonneesDgv = new List<string>();
            // SI affichageSimplifie :  [0] : typeequerre        [1] : nombre        [2] : hauteur       [3] : charge
            // SINON :                  [0] : repere             [1] : hauteur       [2] : charge        [3] : classe
            string stringRequete = affichageSimplifie ?
                "select typeequerre.repere as 'typeEquerre', count(*) as 'nombre',hauteur,charge " +
                "from reserverEquerre " +
                "inner join equerre on equerre.id = reserverEquerre.idEquerre " +
                "inner join typeequerre on typeequerre.id = equerre.idTypeEquerre " +
                "inner join Propriete   on Propriete.id = reserverEquerre.idPropriete " +
                "where idBloc = @idBloc and pm=@pm " +
                "group by idPropriete"
                :
                "select distinct equerre.repere,hauteur,charge,classe " +
                "from Equerre " +
                "inner join reserverEquerre on reserverEquerre.idEquerre = Equerre.id " +
                "inner join Propriete   on Propriete.id = reserverEquerre.idPropriete " +
                "inner join Deplacer on Deplacer.idPropriete = Propriete.id " +
                "inner join Transport on Transport.id = Deplacer.idTransport " +
                "where reserverEquerre.idBloc = @idBloc and pm=@pm ";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));

            MySqlDataReader dr = _requete.ExecuteReader();
            if (affichageSimplifie)
            {
                while (dr.Read())
                {
                    listeDonneesDgv.Add(Convert.ToString(dr["typeEquerre"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["nombre"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["hauteur"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["charge"]));
                }
            }
            else
            {
                while (dr.Read())
                {
                    listeDonneesDgv.Add(Convert.ToString(dr["repere"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["hauteur"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["charge"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["classe"]));
                }
            }

            dr.Close();
            _connexion.Close();

            return listeDonneesDgv;
        }

       /// <summary>
       /// Retourne une liste représentant toutes les équerres prêtées au chantier {pm} du bloc {bloc}
       /// </summary>
       /// <param name="bloc"></param>
       /// <param name="pm"></param>
       /// <param name="affichageSimplifie"></param>
       /// <returns></returns>
        public List<string> getAllPretsBloc(Bloc bloc, bool pm, bool affichageSimplifie)
        {
            List<string> listeDonneesDgv = new List<string>();
            // SI affichageSimplifie :  [0] : typeequerre        [1] : nombre        [2] : hauteur       [3] : charge       [4] : dateFin
            // SINON :                  [0] : repere             [1] : hauteur       [2] : charge        [3] : classe       [4] : dateFin
            string stringRequete = affichageSimplifie ?
                "select typeequerre.repere as 'typeEquerre', count(*) as 'nombre',hauteur,charge, datefin " +
                "from pret " +
                "inner join equerre on equerre.id = pret.idEquerre " +
                "inner join typeequerre on typeequerre.id = equerre.idTypeEquerre " +
                "inner join Propriete on Propriete.id = pret.idPropriete " +
                "where idBloc = @idBloc and pm = @pm " +
                "group by typeEquerre.repere,idPropriete,datefin"
                :
                "select repere,hauteur,charge,classe,datefin from pret "+
                "inner join Equerre on Equerre.id = Pret.idEquerre "+
                "inner join Propriete on Propriete.id = Pret.idPropriete "+
                "inner join Deplacer on Deplacer.idPropriete = Propriete.id "+
                "inner join Transport on Transport.id = Deplacer.idTransport "+
                "where idBloc = @idBloc and pm = @pm";

            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));

            MySqlDataReader dr = _requete.ExecuteReader();
            if (affichageSimplifie)
            {
                while (dr.Read())
                {
                    listeDonneesDgv.Add(Convert.ToString(dr["typeEquerre"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["nombre"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["hauteur"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["charge"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["dateFin"]));
                }
            }
            else
            {
                while (dr.Read())
                {
                    listeDonneesDgv.Add(Convert.ToString(dr["repere"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["hauteur"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["charge"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["classe"]));
                    listeDonneesDgv.Add(Convert.ToString(dr["dateFin"]));
                }
            }

            dr.Close();
            _connexion.Close();

            return listeDonneesDgv;
        }

        /// <summary>
        /// Retourne une liste représentant tous les utilisateurs
        /// </summary>
        /// <returns></returns>
        public List<string> getAllUtilisateurs()
        {
            List<string> donneesDeTousLesUtilisateurs = new List<string>();
            // [0] : trigramme      [1] : nom       [2] : prenom        [3] : rang
            _connexion.Open();
            _requete = new MySqlCommand("select * from Utilisateur", _connexion);

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                donneesDeTousLesUtilisateurs.Add( (string)dr["trigramme"] );
                donneesDeTousLesUtilisateurs.Add( (string)dr["nom"] );
                donneesDeTousLesUtilisateurs.Add( (string)dr["prenom"] );
                donneesDeTousLesUtilisateurs.Add( Convert.ToString(dr["rang"]) );
            }
            dr.Close();
            _connexion.Close();

            return donneesDeTousLesUtilisateurs;
        }

        /// <summary>
        /// Retourne une liste contenant le nom de tous les navires
        /// </summary>
        /// <returns></returns>
        public List<string> getAllNavire()
        {
            List<string> nomDeTousLesNavires = new List<string>();
            _connexion.Open();
            _requete = new MySqlCommand("select nom from Navire", _connexion);

            MySqlDataReader dr = _requete.ExecuteReader();
            while (dr.Read())
            {
                nomDeTousLesNavires.Add( (string)dr["nom"] );
            }
            dr.Close();
            _connexion.Close();

            return nomDeTousLesNavires;
        }

        // ############################################################ \\
        // ########################## Inserts ######################### \\
        // ############################################################ \\
        /// <summary>
        /// Réserve l'équerre ayant pour Id {idEquerre} pour le chantier {pm} du bloc {bloc} avec comme propriété {idPropriété}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="idEquerre"></param>
        /// <param name="idPropriete"></param>
        /// <param name="pm"></param>
        public void reserverEquerre(Bloc bloc, int idEquerre, int idPropriete, bool pm)
        {
            _connexion.Open();
            _requete = new MySqlCommand("insert into reserverEquerre(idBloc,idEquerre,idPropriete,pm) values(@idBloc,@idEquerre,@idPropriete,@pm)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@idEquerre", idEquerre));
            _requete.Parameters.Add(new MySqlParameter("@idPropriete", idPropriete));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            _requete.ExecuteNonQuery();
            int dernierId = (int)_requete.LastInsertedId;
            _connexion.Close();
        }

        /// <summary>
        /// Crée un nouvel utilisateur
        /// </summary>
        /// <param name="trigramme"></param>
        /// <returns></returns>
        public Utilisateur newUtilisateur(string trigramme)
        {
            _connexion.Open();
            _requete = new MySqlCommand("insert into Utilisateur(trigramme,nom,prenom,rang,main) values(@trigramme,@nom,@prenom,@rang,@main)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@trigramme", trigramme));
            _requete.Parameters.Add(new MySqlParameter("@nom", ""));
            _requete.Parameters.Add(new MySqlParameter("@prenom", ""));
            _requete.Parameters.Add(new MySqlParameter("@rang", 1));
            _requete.Parameters.Add(new MySqlParameter("@main", false));
            _requete.ExecuteNonQuery();
            int dernierId = (int)_requete.LastInsertedId;
            _connexion.Close();
            return new Utilisateur(dernierId, trigramme, 1, "", "",false);
        }

        /// <summary>
        /// Crée un nouveau navire
        /// </summary>
        /// <param name="nomNavire"></param>
        /// <returns></returns>
        public Navire newNavire(string nomNavire)
        {
            Navire navire = new Navire(0,nomNavire);
            _connexion.Open();
            _requete = new MySqlCommand("insert into Navire(nom) values(@nom)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@nom", nomNavire));
            _requete.ExecuteNonQuery();
            navire.Id = (int)_requete.LastInsertedId;
            _connexion.Close();
            return navire;
        }

        /// <summary>
        /// Crée un nouveau bloc
        /// </summary>
        /// <param name="navire"></param>
        /// <param name="repere"></param>
        /// <param name="dateDebutPm"></param>
        /// <param name="dateFinPm"></param>
        /// <returns></returns>
        public Bloc newBloc(Navire navire, string repere, DateTime dateDebutPm, DateTime dateFinPm)
        {
            _connexion.Open();
            _requete = new MySqlCommand("insert into Bloc(idNavire,repere,dateDebutPm,dateFinPm,dateFinBord,dateDebutPmVerrouillee,dateFinPmVerrouillee,remarque,stadeEtudePm,stadeEtudeBord) values (@idNavire,@repere,@dateDebutPm,@dateFinPm,@dateFinBord,@dateDebutPmVerrouillee,@dateFinPmVerrouillee,@remarque,@stadeEtudePm,@stadeEtudeBord)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idNavire", navire.Id));
            _requete.Parameters.Add(new MySqlParameter("@repere", repere));
            _requete.Parameters.Add(new MySqlParameter("@dateDebutPm", dateDebutPm));
            _requete.Parameters.Add(new MySqlParameter("@dateFinPm", dateFinPm));
            _requete.Parameters.Add(new MySqlParameter("@dateFinBord", new DateTime()));

            _requete.Parameters.Add(new MySqlParameter("@dateDebutPmVerrouillee", false));
            _requete.Parameters.Add(new MySqlParameter("@dateFinPmVerrouillee", false));

            _requete.Parameters.Add(new MySqlParameter("@remarque", ""));
            _requete.Parameters.Add(new MySqlParameter("@stadeEtudePm", false));
            _requete.Parameters.Add(new MySqlParameter("@stadeEtudeBord", false));
            _requete.ExecuteNonQuery();
            int dernierId = (int)_requete.LastInsertedId;
            _connexion.Close();

            Bloc bloc = new Bloc(dernierId, navire, repere, dateDebutPm, dateFinPm, new DateTime(), false, false, "", false, false);
            GenerateurPdf generateurPdf = new GenerateurPdf(PersistanceParametres.CheminDossierSauvegardePdf);
            DateTime dateDebut, dateFin;
            dateDebut = getDateModifiee(bloc.DateDebutPm, -1);
            dateFin = getDateModifiee(bloc.DateFinPm, 1);
            miseAJourPdf(bloc, true, generateurPdf.genererPdf(navire.Nom, true, repere, getSemaine(dateDebut), dateDebut.Year, getSemaine(dateFin), dateFin.Year, new ArrayList(), new ArrayList()));
            dateDebut = getDateModifiee(bloc.DateFinPm, -2);
            dateFin = getDateModifiee(bloc.DateFinPm, -2);
            miseAJourPdf(bloc, false, generateurPdf.genererPdf(navire.Nom, false, repere, getSemaine(dateDebut), dateDebut.Year, getSemaine(dateFin), dateFin.Year, new ArrayList(), new ArrayList()));
            
            return bloc;
        }

        /// <summary>
        /// Crée une nouvelle équerre
        /// </summary>
        /// <param name="repere"></param>
        /// <param name="remarque"></param>
        /// <param name="typeEquerre"></param>
        /// <returns></returns>
        public Equerre newEquerre(string repere, string remarque, TypeEquerre typeEquerre)
        {
            _connexion.Open();
            _requete = new MySqlCommand("insert into Equerre(idTypeEquerre,repere,remarque) values (@idTypeEquerre,@repere,@remarque)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idTypeEquerre", typeEquerre.Id));
            _requete.Parameters.Add(new MySqlParameter("@repere", repere));
            _requete.Parameters.Add(new MySqlParameter("@remarque", remarque));
            _requete.ExecuteNonQuery();
            int dernierId = (int)_requete.LastInsertedId;
            _connexion.Close();

            Equerre equerre = new Equerre(dernierId, repere, remarque);
            equerre.TypeEquerre = typeEquerre;

            return equerre;
        }

        /// <summary>
        /// Crée un nouveau type d'équerre
        /// </summary>
        /// <param name="repere"></param>
        /// <param name="numeroPlan"></param>
        /// <param name="semblable"></param>
        /// <param name="reglageHauteur"></param>
        /// <param name="cheminImage"></param>
        /// <returns></returns>
        public TypeEquerre newTypeEquerre(string repere, string numeroPlan, bool semblable, int reglageHauteur, string cheminImage)
        {
            _connexion.Open();
            _requete = new MySqlCommand("insert into TypeEquerre(repere,numeroPlan,semblable,reglageHauteur,cheminImage) values (@repere,@numeroPlan,@semblable,@reglageHauteur,@cheminImage)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@repere", repere));
            _requete.Parameters.Add(new MySqlParameter("@numeroPlan", numeroPlan));
            _requete.Parameters.Add(new MySqlParameter("@semblable", semblable));
            _requete.Parameters.Add(new MySqlParameter("@reglageHauteur", reglageHauteur));
            _requete.Parameters.Add(new MySqlParameter("@cheminImage", cheminImage));
            _requete.ExecuteNonQuery();
            int dernierId = (int)_requete.LastInsertedId;
            _connexion.Close();

            return new TypeEquerre(dernierId, repere, numeroPlan, semblable, reglageHauteur, cheminImage);
        }

        /// <summary>
        /// Crée un nouveau moyen de transport
        /// </summary>
        /// <param name="classe"></param>
        /// <param name="libelle"></param>
        /// <param name="difficulte"></param>
        /// <returns></returns>
        public Transport newTransport(string classe, string libelle, int difficulte)
        {
            _connexion.Open();
            _requete = new MySqlCommand("insert into Transport(classe,libelle) values (@classe,@libelle)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@classe", classe));
            _requete.Parameters.Add(new MySqlParameter("@libelle", libelle));
            _requete.ExecuteNonQuery();
            int dernierId = (int)_requete.LastInsertedId;
            _connexion.Close();

            return new Transport(dernierId, classe, libelle);
        }

        /// <summary>
        /// Crée un nouveau déplacement (permet d'associer une propriété à un moyen de transport)
        /// </summary>
        /// <param name="propriete"></param>
        /// <param name="transport"></param>
        private void newDeplacement(Propriete propriete, Transport transport)
        {
            _connexion.Open();
            _requete = new MySqlCommand("insert into Deplacer(idPropriete,idTransport) values (@idPropriete,@idTransport)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idPropriete", propriete.Id));
            _requete.Parameters.Add(new MySqlParameter("@idTransport", transport.Id));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        /// <summary>
        /// Crée une nouvelle propriété
        /// </summary>
        /// <param name="typeEquerre"></param>
        /// <param name="transport"></param>
        /// <param name="hauteur"></param>
        /// <param name="charge"></param>
        /// <returns></returns>
        public Propriete newPropriete(TypeEquerre typeEquerre, Transport transport, int hauteur, decimal charge)
        {
            _connexion.Open();
            _requete = new MySqlCommand("insert into Propriete(idTypeEquerre,hauteur,charge) values (@idTypeEquerre,@hauteur,@charge)", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idTypeEquerre", typeEquerre.Id));
            _requete.Parameters.Add(new MySqlParameter("@hauteur", hauteur));
            _requete.Parameters.Add(new MySqlParameter("@charge", charge));
            _requete.ExecuteNonQuery();
            int dernierId = (int)_requete.LastInsertedId;
            _connexion.Close();

            Propriete propriete = new Propriete(dernierId, hauteur, charge);
            newDeplacement(propriete, transport);


            return propriete; 
        }


        /// <summary>
        /// Réserve un nombre {nombre} de tins de type {libelleTin} pour le chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc">Bloc visé par la réservation</param>
        /// <param name="pm">TRUE si le chantier est PM, FALSE si le chantier est BORD</param>
        /// <param name="libelleTin">Libelle du type de tin</param>
        /// <param name="nombre">Nombre de tin(s) à réserver</param>
        public void reserverTin(Bloc bloc, bool pm, string libelleTin, int nombre)
        {
            string stringRequete = "insert into reserverOutillage(idBloc,pm,idOutillage,nombre) values (@idBloc,@pm,@idOutillage,@nombre)";
            int idTypeTin = getIdTypeTin(libelleTin);
            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            _requete.Parameters.Add(new MySqlParameter("@idOutillage", idTypeTin));
            _requete.Parameters.Add(new MySqlParameter("@nombre", nombre));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        // ############################################################ \\
        // ########################## Updates ######################### \\
        // ############################################################ \\

        /// <summary>
        /// Met à jour le nombre de tins réservés sur le chantier {pm} du bloc {bloc} ayant pour type de tin {libelleTin}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="libelleTin"></param>
        /// <param name="nombre">Nombre de tins à réserver</param>
        /// <param name="typeOperation">'-' si les tins sont à déréserver, sinon '+' si il faut incrémenter le nombre de réservations existantes pour les tins du type {libelleTin}</param>
        public void updateNombreDeTinsReserves(Bloc bloc, bool pm, string libelleTin, int nombre, char typeOperation)
        {
            string stringRequete = "update reserveroutillage " +
                                    "inner join Outillage on Outillage.id = reserveroutillage.idOutillage " +
                                    "set nombre = nombre " + typeOperation + " @nombre where idbloc = @idBloc and pm = @pm and libelle = @libelle";
            _connexion.Open();
            _requete = new MySqlCommand(stringRequete, _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@nombre", nombre));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            _requete.Parameters.Add(new MySqlParameter("@libelle", libelleTin));
            _requete.ExecuteNonQuery();
            _connexion.Close();
            supprimerReservationTinSiZero();
        }

        public void updateUtilisateur(Utilisateur utilisateur)
        {
            _connexion.Open();
            _requete = new MySqlCommand("update Utilisateur set trigramme=@trigramme, nom=@nom, prenom=@prenom, rang=@rang, main=@main where id=@id", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@trigramme", utilisateur.Trigramme));
            _requete.Parameters.Add(new MySqlParameter("@nom", utilisateur.Nom));
            _requete.Parameters.Add(new MySqlParameter("@prenom", utilisateur.Prenom));
            _requete.Parameters.Add(new MySqlParameter("@rang", utilisateur.Rang));
            _requete.Parameters.Add(new MySqlParameter("@id", utilisateur.Id));
            _requete.Parameters.Add(new MySqlParameter("@main", utilisateur.Main));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        /// <summary>
        /// Met à jour l'état du PDF de chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="majReussie"></param>
        private void miseAJourPdf(Bloc bloc, bool pm, bool majReussie)
        {
            string chantier = pm ? "pm" : "bord";
            _connexion.Open();
            _requete = new MySqlCommand("update EtatMiseAJourPdf set " + chantier + "=" + Convert.ToInt32(majReussie), _connexion);
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        public void updateBloc(Bloc bloc)
        {
            _connexion.Open();
            _requete = new MySqlCommand("update Bloc set dateDebutPm=@dateDebutPm, dateFinPm=@dateFinPm, dateFinBord=@dateFinBord, dateDebutPmVerrouillee=@dateDebutPmVerrouillee, dateFinPmVerrouillee=@dateFinPmVerrouillee, remarque=@remarque, stadeEtudePm=@stadeEtudePm, stadeEtudeBord=@stadeEtudeBord where id=@id", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@dateDebutPm", bloc.DateDebutPm));
            _requete.Parameters.Add(new MySqlParameter("@dateFinPm", bloc.DateFinPm));
            _requete.Parameters.Add(new MySqlParameter("@dateFinBord", bloc.DateFinBord));
            _requete.Parameters.Add(new MySqlParameter("@dateDebutPmVerrouillee", bloc.DateDebutPmVerrouillee));
            _requete.Parameters.Add(new MySqlParameter("@dateFinPmVerrouillee", bloc.DateFinPmVerrouillee));
            _requete.Parameters.Add(new MySqlParameter("@remarque", bloc.Remarque));
            _requete.Parameters.Add(new MySqlParameter("@stadeEtudePm", bloc.StadeEtudePm));
            _requete.Parameters.Add(new MySqlParameter("@stadeEtudeBord", bloc.StadeEtudeBord));
            _requete.Parameters.Add(new MySqlParameter("@id", bloc.Id));
            _requete.ExecuteNonQuery();
            _connexion.Close();

            GenerateurPdf generateurPdf = new GenerateurPdf(PersistanceParametres.CheminDossierSauvegardePdf);
            DateTime dateDebut, dateFin;
            ArrayList reservations, prets;

            dateDebut = getDateModifiee(bloc.DateDebutPm, -1);
            dateFin = getDateModifiee(bloc.DateFinPm, 1);
            reservations = getEquerresReserveesBlocPourPdf(bloc, true);
            prets = getEquerresPreteesBlocPourPdf(bloc, true);
            miseAJourPdf(bloc, true, generateurPdf.genererPdf(bloc.Navire.Nom, true, bloc.Repere, getSemaine(dateDebut), dateDebut.Year, getSemaine(dateFin), dateFin.Year, reservations, prets));

            dateDebut = getDateModifiee(bloc.DateFinPm, -2);
            dateFin = bloc.DateFinBord.Year == 1 ? getDateModifiee(bloc.DateFinPm, -2) : getDateModifiee(bloc.DateFinBord, 1);

            reservations = getEquerresReserveesBlocPourPdf(bloc, false);
            prets = getEquerresPreteesBlocPourPdf(bloc, false);
            miseAJourPdf(bloc, false, generateurPdf.genererPdf(bloc.Navire.Nom, false, bloc.Repere, getSemaine(dateDebut), dateDebut.Year, getSemaine(dateFin), dateFin.Year, reservations, prets));

        }

        public void updateNavire(Navire navire)
        {
            _connexion.Open();
            _requete = new MySqlCommand("update Navire set nom=@nom where id=@id", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@nom", navire.Nom));
            _requete.Parameters.Add(new MySqlParameter("@id", navire.Id));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        public void updateEquerre(Equerre equerre)
        {
            _connexion.Open();
            _requete = new MySqlCommand("update Equerre set idTypeEquerre=@idTypeEquerre, repere=@repere, remarque=@remarque where id=@id", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idTypeEquerre", equerre.TypeEquerre.Id));
            _requete.Parameters.Add(new MySqlParameter("@repere", equerre.Repere));
            _requete.Parameters.Add(new MySqlParameter("@remarque", equerre.Remarque));
            _requete.Parameters.Add(new MySqlParameter("@id", equerre.Id));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        public void updateTypeEquerre(TypeEquerre typeEquerre)
        {
            _connexion.Open();
            _requete = new MySqlCommand("update TypeEquerre set repere=@repere, numeroPlan=@numeroPlan, semblable=@semblable, reglageHauteur=@reglageHauteur, cheminImage=@cheminImage where id=@id", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@repere", typeEquerre.Repere));
            _requete.Parameters.Add(new MySqlParameter("@numeroPlan", typeEquerre.NumeroPlan));
            _requete.Parameters.Add(new MySqlParameter("@semblable", typeEquerre.Semblable));
            _requete.Parameters.Add(new MySqlParameter("@reglageHauteur", typeEquerre.ReglageHauteur));
            _requete.Parameters.Add(new MySqlParameter("@cheminImage", typeEquerre.CheminImage));
            _requete.Parameters.Add(new MySqlParameter("@id", typeEquerre.Id));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        public void updatePropriete(Propriete propriete)
        {
            _connexion.Open();
            _requete = new MySqlCommand("update Propriete set hauteur=@hauteur, charge=@charge where id=@id", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@hauteur", propriete.Hauteur));
            _requete.Parameters.Add(new MySqlParameter("@charge", propriete.Charge));
            _requete.Parameters.Add(new MySqlParameter("@id", propriete.Id));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        public void updateTransport(Transport transport)
        {
            _connexion.Open();
            _requete = new MySqlCommand("update Transport set classe=@classe, libelle=@libelle where id=@id", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@classe", transport.Classe));
            _requete.Parameters.Add(new MySqlParameter("@libelle", transport.Libelle));
            _requete.Parameters.Add(new MySqlParameter("@id", transport.Id));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }
    
        /// <summary>
        /// Met à jour la date de la dernière mise à jour grâce au fichier extraction C212P
        /// </summary>
        public void updatePersistanceParametresDateMiseAJour()
        {
            _connexion.Open();
            _requete = new MySqlCommand("update PersistanceParametres set dateMiseAJour=@dateMiseAJour", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@dateMiseAJour", DateTime.Now));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        public void updatePersistanceParametres()
        {
            _connexion.Open();
            _requete = new MySqlCommand("update PersistanceParametres set cheminDossierSauvegardePdf=@cheminDossierSauvegardePdf, cheminFichierTxtConnexionBdd=@cheminFichierTxtConnexionBdd, cheminFichierExtractionC212P=@cheminFichierExtractionC212P, cheminImageErreur=@cheminImageErreur", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@cheminDossierSauvegardePdf", PersistanceParametres.CheminDossierSauvegardePdf));
            _requete.Parameters.Add(new MySqlParameter("@cheminFichierTxtConnexionBdd", PersistanceParametres.CheminFichierTxtConnexionBdd));
            _requete.Parameters.Add(new MySqlParameter("@cheminFichierExtractionC212P", PersistanceParametres.CheminFichierExtractionC212P));
            _requete.Parameters.Add(new MySqlParameter("@cheminImageErreur", PersistanceParametres.CheminImageErreur));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        /// <summary>
        /// Tous les utilisateurs ayant la main la perdent
        /// </summary>
        public void resetMain()
        {
            _connexion.Open();
            _requete = new MySqlCommand("update Utilisateur set main=0 where main=1", _connexion);
            MySqlDataReader dr = _requete.ExecuteReader();
            _connexion.Close();
        }

        /// <summary>
        /// Met à jour les données de la base de données en s'appuyant sur le fichier Extraction C212P
        /// </summary>
        public void updateBDD()
        {
            // todo : gérer les contraintes de réservation
            // Exemple : soit une équerre appelée 'E', un bloc 'B1' et un second bloc 'B2'
            // Le bloc B2 commence juste après la fin du bloc B1. E est réservée sur B1 puis sur B2
            // Seulement, lors de la mise à jour, la date de fin de B1 est retardée. B2 aura commencé avant la fin de B1
            // E ne peut pas se libérer pour être disponible à temps sur B2. Il faut donc modifier la réservation de E sur B2 pour remplacer E par une équerre du même type.
            // Si une équerre du même type est disponible, on remplace E par celle-ci, sinon on prévient l'équipe CM qu'il va falloir modifier certaines réservations

            List<string> listeNavires = getAllNavire();
            List<string> listeBlocs = new List<string>();
            string ligne = "";
            Bloc bloc;

            StreamReader sr = new StreamReader(PersistanceParametres.CheminFichierExtractionC212P);
            char[] delimiteur = new char[] { '\t' }; // Caractère de split : tabulation
            int k = 0;
            // On saute les 3 1ères lignes du fichier
            sr.ReadLine();
            sr.ReadLine();
            sr.ReadLine();
            Navire navire = new Navire(0, "premierNavire");

            while ((ligne = sr.ReadLine()) != null)
            {
                k++;
                string[] explode = ligne.Split(delimiteur, StringSplitOptions.RemoveEmptyEntries);

                if (navire.Nom != explode[0])
                {
                    // Si le navire a changé
                    if (listeNavires.Contains(explode[0]))
                    {
                        // Si le navire existe
                        navire = getNavire(explode[0]);
                        listeBlocs = getAllBloc(navire.Nom);

                        // Si le bloc n'existe pas, on le crée
                        if (!listeBlocs.Contains(explode[2]))
                        {
                            newBloc(navire, explode[2], Convert.ToDateTime(explode[3]), Convert.ToDateTime(explode[4]));
                        }
                        else
                        {
                            // update du bloc existant
                            bloc = getBloc(navire, explode[2]);
                            if( (bloc.DateDebutPm != Convert.ToDateTime(explode[3])) && bloc.DateDebutPmVerrouillee == false) { bloc.DateDebutPm = Convert.ToDateTime(explode[3]); }
                            if( (bloc.DateFinPm != Convert.ToDateTime(explode[4]))   && bloc.DateFinPmVerrouillee   == false) { bloc.DateFinPm = Convert.ToDateTime(explode[4]); }
                            updateBloc(bloc);
                        }
                    }
                    else
                    {
                        // Si le navire n'existe pas
                        navire = newNavire(explode[0]);
                        listeNavires.Add(explode[0]);
                        listeBlocs = new List<string>();
                    }
                }
                else
                {
                    if (listeBlocs.Count == 0 || listeBlocs.Contains(explode[2]) == false)
                    {
                        newBloc(navire, explode[2], Convert.ToDateTime(explode[3]), Convert.ToDateTime(explode[4]));
                    }
                }

            }
        }

        // ############################################################ \\
        // ########################## Delete ########################## \\
        // ############################################################ \\
        /// <summary>
        /// Supprime toutes les réservations d'outillages ayant pour nombre de tins réservés 0 (zéro)
        /// Évite d'afficher dans le tableau aperçu des réservations de tins, une ligne avec comme nombre d'outillages réservés la valeur zéro
        /// </summary>
        public void supprimerReservationTinSiZero()
        {
            _connexion.Open();
            _requete = new MySqlCommand("delete from reserveroutillage where nombre=0", _connexion);
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        /// <summary>
        /// Supprime la réservation d'une équerre
        /// </summary>
        /// <param name="bloc">Bloc pour lequel l'équerre avait été réservée</param>
        /// <param name="pm">Chantier du bloc concerné</param>
        /// <param name="idEquerre">ID de l'équerre à déréserver</param>
        private void deleteReservationEquerre(Bloc bloc, bool pm, int idEquerre)
        {
            _connexion.Open();
            _requete = new MySqlCommand("delete from reserverEquerre where idBloc=@idBloc and idEquerre=@idEquerre and pm=@pm", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@idEquerre", idEquerre));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }

        /// <summary>
        /// Supprime le prêt de l'équerre ayant pour id {idEquerre} pour le chantier {pm} du bloc {bloc}
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="idEquerre"></param>
        private void deletePretEquerreBloc(Bloc bloc, bool pm, int idEquerre)
        {
            _connexion.Open();
            _requete = new MySqlCommand("delete from pret where idBloc=@idBloc and idEquerre=@idEquerre and pm=@pm", _connexion);
            _requete.Parameters.Add(new MySqlParameter("@idBloc", bloc.Id));
            _requete.Parameters.Add(new MySqlParameter("@idEquerre", idEquerre));
            _requete.Parameters.Add(new MySqlParameter("@pm", pm));
            _requete.ExecuteNonQuery();
            _connexion.Close();
        }




        // getters
        /// <summary>
        /// Retourne le numéro de semaine d'un date {fromDate}
        /// </summary>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public int getSemaine(DateTime fromDate)
        {
            // Code trouvé sur :
            // http://codebetter.com/petervanooijen/2005/09/26/iso-weeknumbers-of-a-date-a-c-implementation/

            // Get jan 1st of the year
            DateTime startOfYear = fromDate.AddDays(-fromDate.Day + 1).AddMonths(-fromDate.Month + 1);
            // Get dec 31st of the year
            DateTime endOfYear = startOfYear.AddYears(1).AddDays(-1);
            // ISO 8601 weeks start with Monday
            // The first week of a year includes the first Thursday
            // DayOfWeek returns 0 for sunday up to 6 for saterday
            int[] iso8601Correction = { 6, 7, 8, 9, 10, 4, 5 };
            int nds = fromDate.Subtract(startOfYear).Days + iso8601Correction[(int)startOfYear.DayOfWeek];
            int wk = nds / 7;
            switch (wk)
            {
                case 0:
                    // Return weeknumber of dec 31st of the previous year
                    return getSemaine(startOfYear.AddDays(-1));
                case 53:
                    // If dec 31st falls before thursday it is week 01 of next year
                    if (endOfYear.DayOfWeek < DayOfWeek.Thursday)
                        return 1;
                    else
                        return wk;
                default: return wk;
            }

        }

        /// <summary>
        /// Retourne le premier jour de la semaine {numeroSemaine} de l'année {annee}
        /// </summary>
        /// <param name="annee">Annee de la date recherchée</param>
        /// <param name="numeroSemaine">Numéro de la semaine de la date recherchée</param>
        /// <returns></returns>
        public DateTime getPremierJourSemaine(int annee, int numeroSemaine)
        {
            // Code trouvé sur :
            // https://stackoverflow.com/questions/662379/calculate-date-from-week-number
            DateTime jan1 = new DateTime(annee, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = numeroSemaine;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }

        /// <summary>
        /// Retourne la date initiale à laquelle on enlève/ajoute un nombre de semaines {nbSemaines}
        /// </summary>
        /// <param name="dateInitiale">Date initiale</param>
        /// <param name="nbSemaines">Nombre de semaines à enlever[Ex : -2] / ajouter[Ex : 1].</param>
        /// <returns></returns>
        public DateTime getDateModifiee(DateTime dateInitiale, int nbSemaines)
        {
            return CultureInfo.InvariantCulture.Calendar.AddWeeks(dateInitiale, nbSemaines);
        }



        // void
        /// <summary>
        /// Supprime la réservation d'un certain nombre {nombre} équerres d'un même type
        /// </summary>
        /// <param name="bloc">Bloc pour lequel les équerres avaient été réservées</param>
        /// <param name="pm">Chantier du bloc concerné</param>
        /// <param name="typeEquerre">Type d'équerre des équerres à déréserver</param>
        /// <param name="nombre">Nombre d'équerres à déréserver</param>
        public void retirerEquerresBloc(Bloc bloc, bool pm, TypeEquerre typeEquerre, int nombre, int hauteur, int charge)
        {
            int nombreEquerresSupprimees = 0;
            // Recupération des ID de toutes les équerres du Bloc pour le chantier Pm
            List<int> listeIdEquerresDuBloc = getAllEquerresSimilairesReservees(bloc, pm, hauteur, charge);
            for (int k = 0; k < listeIdEquerresDuBloc.Count; k++)
            {
                TypeEquerre typeEquerreTemp = getTypeEquerre(listeIdEquerresDuBloc[k]);     // Récupération du type d'équerre
                if (typeEquerreTemp.Id == typeEquerre.Id)
                {
                    deleteReservationEquerre(bloc, pm, listeIdEquerresDuBloc[k]);       // Déréservation de l'équerre
                    nombreEquerresSupprimees++;
                    if (nombreEquerresSupprimees == nombre) { break; }                  // On arrête la boucle for lorsque le nombre d'équerres à déréserver a été atteint
                }
            }
        }

        /// <summary>
        /// Supprime le prêt d'un certain nombre {nombre} d'équerres du même type
        /// </summary>
        /// <param name="bloc"></param>
        /// <param name="pm"></param>
        /// <param name="typeEquerre"></param>
        /// <param name="nombre"></param>
        /// <param name="hauteur"></param>
        /// <param name="charge"></param>
        public void retirerPretEquerreBloc(Bloc bloc, bool pm, TypeEquerre typeEquerre, int nombre, int hauteur, int charge)
        {
            int nombreEquerresSupprimees = 0;
            // Recupération des ID de toutes les équerres du Bloc pour le chantier Pm
            List<int> listeIdEquerresDuBloc = getAllEquerresSimilairesPretees(bloc, pm, hauteur, charge);
            for (int k = 0; k < listeIdEquerresDuBloc.Count; k++)
            {
                TypeEquerre typeEquerreTemp = getTypeEquerre(listeIdEquerresDuBloc[k]);     // Récupération du type d'équerre
                if (typeEquerreTemp.Id == typeEquerre.Id)
                {
                    deletePretEquerreBloc(bloc, pm, listeIdEquerresDuBloc[k]);       // Déréservation de l'équerre
                    nombreEquerresSupprimees++;
                    if (nombreEquerresSupprimees == nombre) { break; }                  // On arrête la boucle for lorsque le nombre d'équerres à déréserver a été atteint
                }
            }
        }



    }
}
