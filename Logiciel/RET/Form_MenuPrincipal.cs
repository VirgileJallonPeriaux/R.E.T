using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

using System.Globalization;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Windows.Forms.DataVisualization.Charting;

namespace RET
{
    // Dernière mise à jour :   Jallon Virgile      02/03/2018
    public partial class Form_MenuPrincipal : Form
    {
        // Données membres
        private BDDQuery _BDDQuery;
        private Utilisateur _utilisateurActuel;
        private Navire _navireActuel;
        private Bloc _blocActuel;
        private Equerre _equerreActuelle;
        private TypeEquerre _typeEquerreActuelle;
        private List<string> _listeTypeEquerres = new List<string>();
        private List<int> _listeIdEquerresdDisponibles = new List<int>();
        private bool _etatBtnValiderReservation = false;
        private bool _etatBtnReserver = false;
        private bool _chargementEnCours = false;
        private List<int> _listeFenetresOuvertes = new List<int>();
        private bool _fakeClick = false;
        private bool _etatBtnEquerresModifDateFin = false;
        private bool _etatBtnEquerresModifDateDebut = false;
        private string _fenetreActuelle ="";
        private List<string> _listeBlocs = new List<string>();
        private ArrayList _listeObjetsCreesDynamiquement = new ArrayList();
        private List<Panel> _listePanelsCodeCouleur = new List<Panel>();
        private int _dgvPageActuelle = 0;
        private int _dgvPageMax = 0;
        private int _hauteur;
        private int _charge;
        private int _nombre;
        private bool _modificationGestion = false;
        private TypeEquerre _typeEquerreGestion;
        private bool _etatBtnAfficherPret = false;
        private GenerateurPdf _generateurPdf;

        public Form_MenuPrincipal()
        {
            InitializeComponent();
        }

        // todo : certains paramétrages des textbox, combobox... se font dans un événement click.
        // Il serait judicieux de les déplacer dans Form_MenuPrincipal_Load pour ne pas les répéter à chaque fois que le bouton est cliqué
        // todo : certains composants sont cachés/affichés "en même temps" car ils vont "ensemble". Il peut être judicieux de
        // créer une procédure pour clarifier le code.
        private void Form_MenuPrincipal_Load(object sender, System.EventArgs e)
        {
           // Chargement et paramétrage des combobox
            cb_gestion_typesEquerres.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_gestion_typesEquerres.DropDownHeight = cb_gestion_typesEquerres.ItemHeight * 10;
            cb_gestion_reglageHauteur.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_gestion_reglageHauteur.DropDownHeight = cb_gestion_reglageHauteur.ItemHeight * 10;
            cb_stats_typeEquerre.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_stats_typeEquerre.DropDownHeight = cb_navire.ItemHeight * 10;
            cb_gestion_reglageHauteur.Items.Add("Statique");
            cb_gestion_reglageHauteur.Items.Add("Palier");
            cb_gestion_reglageHauteur.Items.Add("Réglable");
            btn_afficherPrets.Text = "Prêts";

            tb_parametres_cheminDossierPdf.ReadOnly = true;
            tb_parametres_cheminFichierC212P.ReadOnly = true;
            tb_parametres_cheminFichierSauvegardeParametres.ReadOnly = true;
            tb_parametres_cheminImage.Visible = false;
            tb_gestion_lienImage.Visible = false;

            string texteParDefautLblErreurChart = "Aucunes statistiques\n        à afficher";
            lbl_stats_erreurNombreTravaux.Text = texteParDefautLblErreurChart;
            lbl_stats_erreurNombrePretsBloc.Text = texteParDefautLblErreurChart;
            lbl_stats_erreurNombrePret.Text = texteParDefautLblErreurChart;
            lbl_stats_erreurTauxOccupation.Text = texteParDefautLblErreurChart;
            lbl_stats_erreurBlocsAyantEuTypeEquerre.Text = texteParDefautLblErreurChart;
            lbl_stats_erreurNombreReservationsParMois.Text = texteParDefautLblErreurChart;

            // todo : si connexion via user + mdp  &&  connexion BDD ok -> Créer un compte root pour le trigramme actuel (si problème bdd, plus personne n'est root)
            // todo : Si l'utilisateur arrive à rétablir la connexion à la base de données après avoir saisi le mot de passe administrateur, il faut alors
            // créer un compte administrateur à cet utilisateur (si la base de données a été supprimée, il faut pouvoir avoir au moins un admin)

            // On crée une connexion à la base de données
            MySqlConnection connexion = ConnexionBDD.Connexion;
            if (connexion == null)
            {
                // Si la connexion échoue, on peut supposer que le problème vient du fait que certains paramètres (identifiant, mot de passe, numéro de port...)
                // ne sont pas corrects. On tente donc de récupérer les derniers paramètres valides dans le fichier .txt
                if (File.Exists(PersistanceParametres.CheminFichierTxtConnexionBdd))
                {
                    // Une fois le fichier ouvert, on en extrait les paramètres puis on retente une connexion avec les paramètres du fichier .txt
                    string[] fichierTxtExplode = File.ReadAllText(PersistanceParametres.CheminFichierTxtConnexionBdd).Split(new string[] { ";;;;" }, StringSplitOptions.None);
                    ConnexionBDD.User = fichierTxtExplode[5];
                    ConnexionBDD.Password = fichierTxtExplode[6];
                    ConnexionBDD.Database = fichierTxtExplode[7];
                    ConnexionBDD.Host = fichierTxtExplode[8];
                    ConnexionBDD.Port = fichierTxtExplode[9];
                    connexion = ConnexionBDD.ReloadConnexion();
                    if (connexion == null)
                    {
                        // Les paramètres du fichier .txt sont incorrects ou bien le problème vient d'ailleurs (serveur de la base de données, droits d'accès...)
                        MessageBox.Show("Impossible de se connecter à la base de données", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        // Ou ouvre la fenêtre permettant de modifier les paramètres de la connexion à la base de données
                        //Form_ModificationParamsBDD nf = new Form_ModificationParamsBDD();
                        //nf.Visible = true;
                        // afficherPageParametres();
                        btn_menuEquerres.Visible = false;
                        btn_menuHistorique.Visible = false;
                        btn_menuPrets.Visible = false;
                        btn_menuStatistiques.Visible = false;
                        btn_menuTins.Visible = false;
                    }
                    else
                    {
                        // Connexion à la base de données réussie
                        MessageBox.Show("Connexion à la base de données réussie", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //_BDDQuery = new BDDQuery();
                        //_BDDQuery.getPersistanceParametres();
                        //File.WriteAllText(PersistanceParametres.CheminFichierTxtConnexionBdd, "user;;;;password;;;;database;;;;host;;;;port;;;;" + ConnexionBDD.User + ";;;;" + ConnexionBDD.Password + ";;;;" + ConnexionBDD.Database + ";;;;" + ConnexionBDD.Host + ";;;;" + ConnexionBDD.Port);
                    }
                }
                else
                {
                    // Le fichier .txt recherché est introuvable, peut-être a-t-il été déplacé
                    MessageBox.Show("Impossible de se connecter à la base de données\n                        [Fichier manquant]", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //Form_ModificationParamsBDD nf = new Form_ModificationParamsBDD();
                    //nf.Visible = true;
                    // afficherPageParametres();
                    btn_menuEquerres.Visible = false;
                    btn_menuHistorique.Visible = false;
                    btn_menuPrets.Visible = false;
                    btn_menuStatistiques.Visible = false;
                    btn_menuTins.Visible = false;
                    pnl_arrierePlanMenuPrincipal.Visible = false;
                }
            }
            else
            {
                // Si la connexion a été réussie dès le 1er essai
                connexion.Close();
                _BDDQuery = new BDDQuery();
                // On charge des informations concernant l'utilisateur actuel
                _utilisateurActuel = _BDDQuery.getUtilisateur("lfm"); // todo : remplacer le trigramme de test par Environment.UserName
                if (_utilisateurActuel.Rang > 0)
                {
                    // Si l'utilisateur actuel n'est pas 'bloqué' (Banni)
                    _BDDQuery.getPersistanceParametres();
                    _generateurPdf = new GenerateurPdf(PersistanceParametres.CheminDossierSauvegardePdf);
                    //File.WriteAllText(PersistanceParametres.CheminFichierTxtConnexionBdd, "user;;;;password;;;;database;;;;host;;;;port;;;;" + ConnexionBDD.User + ";;;;" + ConnexionBDD.Password + ";;;;" + ConnexionBDD.Database + ";;;;" + ConnexionBDD.Host + ";;;;" + ConnexionBDD.Port);

                    pnl_arrierePlanMenuPrincipal.Visible = false;
                    _fenetreActuelle = "menuPrincipal";

                    /*GenerateurPdf gpdf = new GenerateurPdf(PersistanceParametres.CheminDossierSauvegardePdf);
                    Navire nav = _BDDQuery.getNavire("c34");
                    Bloc bloc = _BDDQuery.getBloc(nav, "0104");
                    ArrayList alResa = _BDDQuery.getEquerresReserveesBlocPourPdf(bloc, false);
                    ArrayList alPret = _BDDQuery.getEquerresPreteesBlocPourPdf(bloc, false);
                    MessageBox.Show(gpdf.genererPdf("F22", false, "1123", 23, 2017, 12, 2025,alResa,alPret).ToString());
                    _BDDQuery.updateBDD();*/
                    
                    // Si la BDD n'a pas été mise à jour par un membre de l'équipe Coque Métallique cette semaine, la met à jour si l'utilisateur accepte
                    if (_utilisateurActuel.Rang > 1)
                    {
                        DateTime aujourdHui = DateTime.Now;
                        if (PersistanceParametres.DateMiseAJour.Year != aujourdHui.Year || getSemaine(PersistanceParametres.DateMiseAJour) != getSemaine(aujourdHui))
                        {
                            DialogResult dialogResult = MessageBox.Show("La base de données n'a pas été mise à jour cette semaine. Souhaitez-vous la mettre à jour maintenant ?\nL'opération durera quelques minutes", "Mise à jour de la base de données", MessageBoxButtons.YesNo);
                            if (dialogResult == DialogResult.Yes)
                            {
                                if (File.Exists(PersistanceParametres.CheminFichierExtractionC212P) && Directory.Exists(PersistanceParametres.CheminDossierSauvegardePdf))
                                {
                                    _BDDQuery.updateBDD();
                                    _BDDQuery.updatePersistanceParametresDateMiseAJour();
                                    MessageBox.Show("Mise à jour de la base de données terminée");
                                }
                                else
                                {
                                    string messge = File.Exists(PersistanceParametres.CheminFichierExtractionC212P) ? "Impossible de trouver le dossier de sauvegarde des PDFs" + PersistanceParametres.CheminDossierSauvegardePdf : "Impossible de trouver le fichier d'extraction C212P " + PersistanceParametres.CheminFichierExtractionC212P;
                                    MessageBox.Show(messge, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    // afficherPageParametres();
                                }
                            }
                        }
                    }
                    else
                    {
                        btn_menuGestion.Visible = false;
                        btn_menuHistorique.Visible = false;
                        btn_menuPrets.Visible = true;
                        btn_menuStatistiques.Visible = false;
                    }

                    // Si tous les PDFs n'ont pas pu être mis à jour lors de la dernière mise à jour :  mise à jour obligatoire

                    if(_BDDQuery.miseAJourPdfIncomplete())
                    {
                        MessageBox.Show("La base de données n'a pas été mise à jour complètement. L'opération durera au maximum 30s", "Mise à jour de la base de données", MessageBoxButtons.OK);
                        if (File.Exists(PersistanceParametres.CheminFichierExtractionC212P) && Directory.Exists(PersistanceParametres.CheminDossierSauvegardePdf))
                        {
                            _BDDQuery.updateBDD();
                            MessageBox.Show("Mise à jour de la base de données terminée");
                        }
                        else
                        {
                            string messge = File.Exists(PersistanceParametres.CheminFichierExtractionC212P) ? "Impossible de trouver le dossier de sauvegarde des PDFs" + PersistanceParametres.CheminDossierSauvegardePdf : "Impossible de trouver le fichier d'extraction C212P " + PersistanceParametres.CheminFichierExtractionC212P;
                            MessageBox.Show(messge, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            // afficherPageParametres();
                        }
                    }






                    // si plusieurs utilisateurs ont la main, on reset.
                    //if(_BDDQuery.getUtilisateursAyantLaMain() > 1)
                    //{
                    //    _BDDQuery.resetMain();
                    //}


                        //MessageBox.Show("Veuillez patienter quelques instants, une mise à jour est en cours", "Mise à jour en cours", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //var watch = System.Diagnostics.Stopwatch.StartNew();

                        //watch.Stop();
                        //MessageBox.Show("Millisecondes : "+watch.ElapsedMilliseconds.ToString()); 
                        /*ArrayList al = _BDDQuery.getStatsNombreReservationsParMois("E54");
                        int annee = 0;
                        string resas = "";

                        for(int k=0; k<5; k++)
                        {
                            annee = (int)al[k*13];
                            for (int m = 1+(13*k); m < 13+(13*k); m++)
                            {
                                resas += al[ m ].ToString() + " ";
                            }
                            MessageBox.Show(annee + " : " + resas);
                            annee = 0;
                            resas = "";
                        }
                        */
                        /*
                        Navire nav = _BDDQuery.getNavire("c34");
                        Bloc bloc = _BDDQuery.getBloc(nav, "0104");
                        */
                        // ArrayList al = _BDDQuery.getEquerresReserveesBlocPourPdf(bloc, true);
                        /*ArrayList al = _BDDQuery.getEquerresPreteesBlocPourPdf(bloc, true);
                        string s = "";
                        foreach (var lettre in al)
                        {
                            if (lettre is string)
                            {
                                s += lettre + " ";
                            }
                            else
                            {
                                string[] t = (string[])lettre;
                                // for (int m = 0; m < t.Count; m++)
                                for (int m = 0; m < t.Length; m++)
                                {
                                    s += t[m] + " ";
                                }
                            }
                        }
                        MessageBox.Show(s);
                        */

                }
                else
                {
                    // L'utilisateur actuel est 'bloqué' (Banni), on l'en informe puis la fenêtre se ferme
                    MessageBox.Show("Vous ne possédez pas les droits nécessaires", "Contenu protégé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                }
            }

        }


        private void btn_menuEquerres_Click(object sender, EventArgs e)
        {


            // Permet, en vérifiant l'état de _chargementEnCours, de ne pas effectuer certaines actions lorsque les événements (rbtChanged, cbSelectedIndexChanged...)
            // se déclenchent
            //  Ex : Lorsqu'on charge la fenêtre, on effectue un click sur rbt_pm :  Lors de ce changement d'état, on récupère des infos sur l'objet bloc actuel
            //  Hors celui-ci n'est pas encore instancié lors du 1er changement. On évite donc une erreur
            _chargementEnCours = true;

            //dgv.Columns[2].Visible = true;
            //dgv.Columns[3].Visible = true;

            
            // Hide / Show des composants de la fenêtre
            lbl_typeTin.Visible = false;
            cb_typeTin.Visible = false;
            pnl_infosBloc.Visible = true;
            pnl_arrierePlanMenuPrincipal.Visible = true;
            cb_modificationAnneeDateFin.Visible = false;
            cb_modificationSemaineDateFin.Visible = false;
            cb_modificationAnneeDateDebut.Visible = false;
            cb_modificationSemaineDateDebut.Visible = false;

            lbl_apercuResasDuBloc.Visible = false;
            dgv_apercuResasDuBloc.Visible = false;
            lv_typeEquerresDisponibles.Visible = false;

            btn_reservation.Visible = false;
            btn_laisserLaMain.Visible = false;

            pnl_reservation.Visible = false;
            pnl_ficheTechniqueEquerre.Visible = false;
            lb_detailTypeEquerre.Visible = false;
            lbl_reservationIndisponible.Visible = false;

            tb_charge.Visible = true;
            tb_hauteur.Visible = true;
            lbl_charge.Visible = true;
            lbl_hauteur.Visible = true;
           

            // Enabled , ReadOnly , Selected ...
            // rbt_equerres_pm.Checked = true;
            rbt_pm.PerformClick();
            cb_dateDebut.Enabled = false;
            cb_dateFin.Enabled = false;
            nud_nombre.ReadOnly = true;
            dgv_apercuResasDuBloc.ReadOnly = true;
            dgv_apercuResasDuBloc.ReadOnly = true;
            dgv_apercuResasDuBloc.AllowUserToAddRows = false;
            dgv_apercuResasDuBloc.RowHeadersVisible = false;
            dgv_proprietesEquerre.AllowUserToAddRows = false;
            dgv_proprietesEquerre.RowHeadersVisible = false;

            lv_typeEquerresDisponibles.CheckBoxes = true;
            lv_typeEquerresDisponibles.View = View.List;
            _fenetreActuelle = "menuEquerres";
            
            lbl_stats_erreurBlocsAyantEuTypeEquerre.Visible = false;
            lbl_stats_erreurNombrePret.Visible = false;
            lbl_stats_erreurNombrePretsBloc.Visible = false;
            lbl_stats_erreurNombreReservationsParMois.Visible = false;
            lbl_stats_erreurNombreTravaux.Visible = false;
            lbl_stats_erreurTauxOccupation.Visible = false;

            chart_blocsAyantEuTypeEquerre.Visible = false;
            chart_nombrePrets.Visible = false;
            chart_nombrePretsBloc.Visible = false;
            chart_nombreReservationsParMois.Visible = false;
            chart_nombreTravaux.Visible = false;
            chart_tauxOccupation.Visible = false;

            cb_stats_typeEquerre.Visible = false;
            btn_gestion_modifier.Visible = false;
            btn_gestion_nouveau.Visible = false;
            tab_gestion.Visible = false;
            tmr_verificationDisponibiliteMain.Stop();
            btn_pageSuivante.Visible = false;
            btn_pagePrecedente.Visible = false;
            lbl_indicationNumeroPage.Visible = false;
            dgv_apercuResasDuBloc.AllowUserToResizeColumns = false;
            dgv_apercuResasDuBloc.AllowUserToResizeRows = false;
            _etatBtnValiderReservation = false;
            btn_afficherPrets.Visible = false;

            // nud_equerres_reserver_nombreEquerres.ReadOnly = true;

            // Paramétrage des composants de la fenêtre

            cb_modificationAnneeDateFin.Items.Clear();
            cb_modificationSemaineDateFin.Items.Clear();

            tb_rechercheBloc.AutoCompleteMode = AutoCompleteMode.Suggest;
            tb_rechercheBloc.AutoCompleteSource = AutoCompleteSource.CustomSource;

            cb_navire.DataSource = _BDDQuery.getAllNavire();
            cb_navire.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_navire.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_dateDebut.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_dateDebut.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_dateFin.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_dateFin.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_modificationAnneeDateFin.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_modificationAnneeDateFin.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_modificationSemaineDateFin.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_modificationSemaineDateFin.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_modificationAnneeDateDebut.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_modificationAnneeDateDebut.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_modificationSemaineDateDebut.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_modificationSemaineDateDebut.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_stadeEtudeBloc.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_stadeEtudeBloc.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_stadeEtudeBloc.Items.Clear();
            cb_stadeEtudeBloc.Items.Add("PRELI");
            cb_stadeEtudeBloc.Items.Add("BPE");

            // On ajoute un string vide dans les cb affichant les dates et on sélectionne ce string vide par défaut.
            //  - Simplifie, pour plus tard, le fait d'afficher une date dans la cb en utilisant cb.Items[0] = "Texte"
            cb_dateDebut.Items.Add("");
            cb_dateFin.Items.Add("");
            cb_dateFin.SelectedIndex = 0;
            cb_dateDebut.SelectedIndex = 0;


            // Derniers paramétrages
            pnl_infosDetailleesBloc.Visible = false;         // Impossible de mettre plus haut dans le code, sinon on ne peut pas effectuer un click sur le rbt_pm



            _chargementEnCours = false;
        }


        private void cb_navire_SelectedIndexChanged(object sender, EventArgs e)
        {
            // L'utilisateur vient de sélectionner un navire, celui-ci deviendra le navire actuel
            _navireActuel = _BDDQuery.getNavire(cb_navire.SelectedItem.ToString());
            // On charge tous les blocs du navire
            _listeBlocs = _BDDQuery.getAllBloc(cb_navire.SelectedItem.ToString());
            // Cette liste de blocs deviendra la source de données de la textbox tb_rechercheBloc.
            // Celle-ci sera donc capable de proposer des valeurs (ici, tous les repères des blocs du navire)
            tb_rechercheBloc.Text = "";
            tb_rechercheBloc.AutoCompleteCustomSource = setSource(_listeBlocs);
        }


  
        private void tb_rechercheBloc_TextChanged(object sender, EventArgs e)
        {
            pnl_ficheTechniqueEquerre.Visible = false;

            if(_listeBlocs.Contains(tb_rechercheBloc.Text))
            {
                // Le repère saisi par l'utilisateur fait bien partie de la liste des repères des blocs du navire

                // Le bloc actuel devient celui sélectionné par l'utilisateur
                _blocActuel = _BDDQuery.getBloc(_navireActuel, tb_rechercheBloc.Text);
                // On change le texte du label
                // Ex : Aperçu des équerres pour le bloc 0114
                string typeRepere = (tb_rechercheBloc.Text[0]=='0') ? "bloc" : "panneau";
                string typeSelectionne = (_fenetreActuelle == "menuEquerres") ? "équerres" : "tins";
                lbl_apercuResasDuBloc.Text = "Aperçu des "+typeSelectionne+" pour le "+typeRepere+" "+ tb_rechercheBloc.Text;
                // On affiche / cache certain éléments
                lbl_apercuResasDuBloc.Visible = true;
                dgv_apercuResasDuBloc.Visible = true;
                btn_reservation.Visible = _utilisateurActuel.Rang > 1;
                lbl_indicationNumeroPage.Visible = true;
                btn_pagePrecedente.Visible = false;
                btn_pageSuivante.Visible = false;
                lbl_indicationNumeroPage.Visible = false;
                pnl_infosDetailleesBloc.Visible = true;

                // Par défaut, la page affichée dans le DataGridView est la page 1 (d'indice 0)
                _dgvPageActuelle = 0;
                _dgvPageMax = 0;


                // Paramétrage et remplissage du DataGridView reprenant les réservations associées au bloc
                if (_fenetreActuelle == "menuEquerres")
                {
                    btn_afficherPrets.Visible = true;
                    rafraichirDgvEquerres();
                }
                else
                {
                    btn_afficherPrets.Visible = false;
                    rafraichirDgvTins();
                }
                rbt_bord.PerformClick();
                rbt_pm.PerformClick();


                if (_utilisateurActuel.Rang > 1)
                {
                    // Si l'utilisateur actuel est autorisé à apporter des modifications (réserver des équerres...) on démarre le timer qui permet 
                    // de vérifier si un autre utilisateur a actuellement la main sur la réservation. Le timer se déclenche toutes les 3s (3 000 ms)
                    tmr_verificationDisponibiliteMain.Start();
                    // Afin de ne pas attendre 3s avant de savoir si l'on peut, ou pas, réserver, on execute une 1ère fois le code contenu dans l'évènement
                    // Tick du timer. On affiche un message à l'utilisateur si il ne peut pas prendre la main pour le moment.
                    Utilisateur utilisateur = _BDDQuery.getUtilisateurEnTrainDeReserver();
                    if (utilisateur.Id == 0 || utilisateur.Id == _utilisateurActuel.Id)
                    {
                        btn_reservation.Enabled = true;
                        lbl_reservationIndisponible.Visible = false;
                        lbl_reservationIndisponible.Text = "";
                    }
                    else
                    {
                        btn_reservation.Enabled = false;
                        lbl_reservationIndisponible.Visible = true;
                        lbl_reservationIndisponible.Text = "Une réservation est en cours.\n" + utilisateur.Nom + " " + utilisateur.Prenom + "   [" + utilisateur.Trigramme.ToUpper() + "]";
                    }
                }
            }
            else
            {
                // Si le bloc saisi n'est pas un des blocs du navire, on propose à l'utilisateur de le créer
                // Todo : proposer à l'utilisateur de créer le bloc saisi
                // On cache certains éléments (Ex : on ne doit pas pouvoir cliquer sur le bouton pour réserver alors que le bloc saisi n'existe pas
                lbl_apercuResasDuBloc.Visible = false;
                dgv_apercuResasDuBloc.Visible = false;
                btn_reservation.Visible = false;
                pnl_infosDetailleesBloc.Visible = false;
                pnl_reservation.Visible = false;
                lbl_indicationNumeroPage.Visible = false;
                btn_afficherPrets.Visible = false;
            }
        }



        // todo : Les erreurs liées aux dates sont à "réparer" ici
        private void rbt_pm_CheckedChanged(object sender, EventArgs e)
        {
            if (!_chargementEnCours)
            {
                _dgvPageActuelle = 0;
                _blocActuel = _BDDQuery.getBloc(_navireActuel, tb_rechercheBloc.Text);

                // On rafraîchit le dataGridView aperçu des réservations
                if (_fenetreActuelle == "menuEquerres")
                {
                    //MessageBox.Show("rbtpm changed");
                    if (_fakeClick == false)
                    {
                        rafraichirDgvEquerres();
                    }
                }
                else
                {
                    rafraichirDgvTins();   // false
                }

                cb_modificationAnneeDateDebut.Visible = false;
                cb_modificationSemaineDateDebut.Visible = false;
                cb_modificationAnneeDateFin.Visible = false;
                cb_modificationSemaineDateFin.Visible = false;
                cb_dateDebut.Visible = true;
                cb_dateFin.Visible = true;
                _etatBtnEquerresModifDateDebut = false;
                _etatBtnEquerresModifDateFin = false;
                btn_modificationDateDebut.Text = "M";
                btn_modificationDateFin.Text = "M";

                // On modifie la date affichée en fonction du type du bloc :
                //  - PM :   -1 semaine / +1 semaine
                //  - BORD : -2 semaines / à définir

                // Affichage de la date de début et de fin du bloc
                // todo : Corriger le problème concernant les dates
                if (rbt_pm.Checked)
                {
                    ckb_dateDebutVerrouillee.Visible = true;
                    ckb_dateFinVerrouillee.Visible = true;
                    ckb_dateDebutVerrouillee.Checked = _blocActuel.DateDebutPmVerrouillee;
                    ckb_dateFinVerrouillee.Checked = _blocActuel.DateFinPmVerrouillee;
                    btn_modificationDateDebut.Visible = true;

                    DateTime dtDebutModifiee = getDateModifiee(_blocActuel.DateDebutPm, -1);
                    cb_dateDebut.Items[0] = "S" + getSemaine(dtDebutModifiee).ToString() + " / " + dtDebutModifiee.Year.ToString();

                    DateTime dtFinModifiee = getDateModifiee(_blocActuel.DateFinPm, 1);
                    cb_dateFin.Items[0] = "S" + getSemaine(dtFinModifiee).ToString() + " / " + dtFinModifiee.Year.ToString();
                    cb_stadeEtudeBloc.SelectedIndex = (Convert.ToInt32(_blocActuel.StadeEtudePm));

                }
                else
                {
                    ckb_dateDebutVerrouillee.Visible = false;
                    btn_modificationDateDebut.Visible = false;
                    ckb_dateDebutVerrouillee.Visible = false;
                    ckb_dateFinVerrouillee.Visible = false;
                    DateTime dtDebutModifiee = getDateModifiee(_blocActuel.DateFinPm, -2);
                    cb_dateDebut.Items[0] = "S" + getSemaine(dtDebutModifiee).ToString() + " / " + dtDebutModifiee.Year.ToString();

                    if (_blocActuel.DateFinBord.Year != 1)
                    {
                        DateTime dtFinModifiee = getDateModifiee(_blocActuel.DateFinBord, 1);
                        cb_dateFin.Items[0] = "S" + getSemaine(dtFinModifiee).ToString() + " / " + dtFinModifiee.Year.ToString();
                    }
                    else
                    {
                        cb_dateFin.Items[0] = "N/A";
                        // btn_reservation.Visible = false;
                    }
                    cb_stadeEtudeBloc.SelectedIndex = (Convert.ToInt32(_blocActuel.StadeEtudeBord));
                }
            }
        }
        private void cb_modificationAnneeDateDebut_SelectedIndexChanged(object sender, EventArgs e)
        {
            int nbSemaines = getSemaine(new DateTime(Convert.ToInt32(cb_modificationAnneeDateDebut.SelectedItem), 12, 31));
            int x = cb_modificationSemaineDateDebut.SelectedIndex == 52 && nbSemaines!=53 ? 1 : 0;
            cb_modificationSemaineDateDebut.Items.Clear();

            // On aurait pu faire for(int k=0; k<nbSemaines; k++) pour éviter de faire un if à la suite du for
            // mais pour je ne sais quelle raison nbSemaines prend parfois des valeurs impossibles
            for (int k = 0; k < 52; k++)
            {
                cb_modificationSemaineDateDebut.Items.Add((k + 1).ToString());
            }
            if (nbSemaines == 53) { cb_modificationSemaineDateDebut.Items.Add(53); }

            DateTime dateModifiee = getDateModifiee(_blocActuel.DateDebutPm, -1);
            cb_modificationSemaineDateDebut.SelectedIndex = (_blocActuel.DateDebutPm.Year != 1) ? getSemaine(dateModifiee) - 1 - x : 0;
        }
        private void cb_modificationAnneeDateFin_SelectedIndexChanged(object sender, EventArgs e)
        {
            int nbSemaines = getSemaine(new DateTime(Convert.ToInt32(cb_modificationAnneeDateFin.SelectedItem), 12, 31));
            int x = cb_modificationSemaineDateFin.SelectedIndex == 52 && nbSemaines != 53 ? 1 : 0;
            cb_modificationSemaineDateFin.Items.Clear();
            for (int k = 0; k < 52; k++)
            {
                cb_modificationSemaineDateFin.Items.Add((k + 1).ToString());
            }
            if (nbSemaines == 53) { cb_modificationSemaineDateFin.Items.Add(53); }

            if(rbt_pm.Checked)
            {
                DateTime dateModifiee = getDateModifiee(_blocActuel.DateFinPm, 1);
                cb_modificationSemaineDateFin.SelectedIndex = (_blocActuel.DateFinPm.Year != 1) ? getSemaine(dateModifiee) - 1 - x : 0;
            }
            else
            {
                DateTime dateModifiee =getDateModifiee(_blocActuel.DateFinBord, 1);
                cb_modificationSemaineDateFin.SelectedIndex = (_blocActuel.DateFinBord.Year != 1) ? getSemaine(dateModifiee) - 1 - x : 0;
            }

        }
        private void btn_modificationDateDebut_Click(object sender, EventArgs e)
        {
            if (_etatBtnEquerresModifDateDebut == false)
            {
                // On propose à l'utilisateur de modifier la date
                btn_modificationDateDebut.Text = "ok";
                cb_dateDebut.Visible = false;
                cb_modificationAnneeDateDebut.Visible = true;
                cb_modificationSemaineDateDebut.Visible = true;

                cb_modificationAnneeDateDebut.Items.Clear();
                DateTime dtModifiee = getDateModifiee(_blocActuel.DateDebutPm, -1);
                int anneedebut;

                if (getSemaine(dtModifiee) != 1)
                {
                    anneedebut = (_blocActuel.DateDebutPm.Year == 1) ? DateTime.Now.Year : dtModifiee.Year;
                }
                else
                {
                    anneedebut = (_blocActuel.DateDebutPm.Year == 1) ? DateTime.Now.Year : dtModifiee.Year + 1;         // -1 / +1
                }

                _chargementEnCours = true;
                for (int k = -2; k < 5; k++)
                {
                    cb_modificationAnneeDateDebut.Items.Add(anneedebut + k);
                }
                cb_modificationAnneeDateDebut.SelectedIndex = 2;
                _chargementEnCours = false;
            }
            else
            {
                // On sauvegarde la modification de l'utilisateur si les dates concordent
                DateTime dateDebutModif = getDateModifiee(getPremierJourSemaine(Convert.ToInt32(cb_modificationAnneeDateDebut.SelectedItem), Convert.ToInt32(cb_modificationSemaineDateDebut.SelectedItem)), -1);
                DateTime dateFin = getPremierJourSemaine(_blocActuel.DateFinPm.Year, getSemaine(_blocActuel.DateFinPm));
                DateTime dateFinModif = getPremierJourSemaine(dateFin.Year, getSemaine(dateFin));

                if (DateTime.Compare(dateDebutModif, dateFinModif) <= 0)
                {
                    cb_dateDebut.Visible = true;
                    cb_modificationAnneeDateDebut.Visible = false;
                    cb_modificationSemaineDateDebut.Visible = false;

                    cb_dateDebut.Items[0] = "S" + cb_modificationSemaineDateDebut.SelectedItem.ToString() + " / " + cb_modificationAnneeDateDebut.SelectedItem.ToString();

                    DateTime premierjour = getPremierJourSemaine(Convert.ToInt32(cb_modificationAnneeDateDebut.SelectedItem), Convert.ToInt32(cb_modificationSemaineDateDebut.SelectedItem));
                    _blocActuel.DateDebutPm = getDateModifiee(premierjour, 1);
                    btn_modificationDateDebut.Text = "M";
                    _BDDQuery.updateBloc(_blocActuel);
                }
                else
                {
                    MessageBox.Show("La date de début d'un bloc doit être antérieure à sa date de fin", "Erreur - Date non valide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            _etatBtnEquerresModifDateDebut = !_etatBtnEquerresModifDateDebut;


        }
        private void btn_modificationDateFin_Click(object sender, EventArgs e)
        {
            if (_etatBtnEquerresModifDateFin == false)
            {
                // On propose à l'utilisateur de définir ou de modifier une date de fin 
                btn_modificationDateFin.Text = "ok";
                cb_dateFin.Visible = false;
                cb_modificationAnneeDateFin.Visible = true;
                cb_modificationSemaineDateFin.Visible = true;
                cb_modificationAnneeDateFin.Items.Clear();

                if (rbt_pm.Checked)
                {
                    DateTime dtModifiee = getDateModifiee(_blocActuel.DateFinPm, 1);
                    int anneedebut;
                    // TODO : modifier sinon erreur ex : 2019-01 sauvegarde en bdd 2019-12-27 (erreur année)
                    if (getSemaine(dtModifiee) != 1)
                    {
                        anneedebut = (_blocActuel.DateFinPm.Year == 1) ? DateTime.Now.Year : dtModifiee.Year;
                        //anneedebut = (dtModifiee.Year == 1) ? DateTime.Now.Year : dtModifiee.Year;
                    }
                    else
                    {
                        anneedebut = (_blocActuel.DateFinPm.Year == 1) ? DateTime.Now.Year : dtModifiee.Year + 1;       // -1 / +1
                        //anneedebut = (dtModifiee.Year == 1) ? DateTime.Now.Year : dtModifiee.Year+1;
                    }

                    _chargementEnCours = true;
                    for (int k = -2; k < 5; k++)
                    {
                        cb_modificationAnneeDateFin.Items.Add(anneedebut + k);
                    }
                    cb_modificationAnneeDateFin.SelectedIndex = 2;
                    _chargementEnCours = false;
                }
                else
                {
                    DateTime dtModifiee = getDateModifiee(_blocActuel.DateFinBord, 1);
                    int anneedebut;
                    // TODO : voir toto précédent
                    if (getSemaine(dtModifiee) != 1)
                    {
                        anneedebut = (_blocActuel.DateFinBord.Year == 1) ? DateTime.Now.Year : dtModifiee.Year;
                    }
                    else
                    {
                        anneedebut = (_blocActuel.DateFinBord.Year == 1) ? DateTime.Now.Year : dtModifiee.Year + 1;         // -1 / +1
                    }
                    _chargementEnCours = true;
                    for (int k = -2; k < 5; k++)
                    {
                        cb_modificationAnneeDateFin.Items.Add(anneedebut + k);
                    }
                    cb_modificationAnneeDateFin.SelectedIndex = 2;
                    _chargementEnCours = false;
                }
            }
            else
            {
                DateTime dateDebut;
                DateTime dateDebutModif;
                DateTime dateFinModif;

                if (rbt_pm.Checked)
                {
                    dateFinModif = getDateModifiee(getPremierJourSemaine(Convert.ToInt32(cb_modificationAnneeDateFin.SelectedItem), Convert.ToInt32(cb_modificationSemaineDateFin.SelectedItem)), 1);
                    dateDebut = getPremierJourSemaine(_blocActuel.DateDebutPm.Year, getSemaine(_blocActuel.DateDebutPm));
                    dateDebutModif = getPremierJourSemaine(dateDebut.Year, getSemaine(dateDebut));

                    if (DateTime.Compare(dateDebutModif, dateFinModif) <= 0)
                    {
                        cb_dateFin.Visible = true;
                        cb_modificationAnneeDateFin.Visible = false;
                        cb_modificationSemaineDateFin.Visible = false;

                        cb_dateFin.Items[0] = "S" + cb_modificationSemaineDateFin.SelectedItem.ToString() + " / " + cb_modificationAnneeDateFin.SelectedItem.ToString();
                        btn_modificationDateDebut.Text = "M";

                        DateTime premierjour = getPremierJourSemaine(Convert.ToInt32(cb_modificationAnneeDateFin.SelectedItem), Convert.ToInt32(cb_modificationSemaineDateFin.SelectedItem));
                        _blocActuel.DateFinPm = getDateModifiee(premierjour, -1);
                        _BDDQuery.updateBloc(_blocActuel);
                    }
                    else
                    {
                        MessageBox.Show("erreur date fin pm");
                    }
                }
                else
                {
                    // TODO : quand date modifiée et que, par la suite, reclick sur button
                    // Alors par défaut est sélectionnée 2018/01 alors que devrait être selectionnée la date enregistrée

                    dateFinModif = getDateModifiee(getPremierJourSemaine(Convert.ToInt32(cb_modificationAnneeDateFin.SelectedItem), Convert.ToInt32(cb_modificationSemaineDateFin.SelectedItem)), 1);
                    DateTime dateDebutBord = getDateModifiee(_blocActuel.DateFinPm, -1);
                    dateDebut = getPremierJourSemaine(dateDebutBord.Year, getSemaine(dateDebutBord));
                    dateDebutModif = getPremierJourSemaine(dateDebut.Year, getSemaine(dateDebut));

                    MessageBox.Show("Début : " + dateDebutModif.Year.ToString() + "-" + getSemaine(dateDebutModif).ToString() + "\nFin : " + dateFinModif.Year.ToString() + "-" + getSemaine(dateFinModif).ToString());
                    if (DateTime.Compare(dateDebutModif, dateFinModif) <= 0)
                    {
                        cb_dateFin.Visible = true;
                        cb_modificationAnneeDateFin.Visible = false;
                        cb_modificationSemaineDateFin.Visible = false;

                        cb_dateFin.Items[0] = "S" + cb_modificationSemaineDateFin.SelectedItem.ToString() + " / " + cb_modificationAnneeDateFin.SelectedItem.ToString();
                        btn_modificationDateDebut.Text = "M";

                        DateTime premierjour = getPremierJourSemaine(Convert.ToInt32(cb_modificationAnneeDateFin.SelectedItem), Convert.ToInt32(cb_modificationSemaineDateFin.SelectedItem));
                        _blocActuel.DateFinPm = getDateModifiee(premierjour, -1);
                        _BDDQuery.updateBloc(_blocActuel);

                        //rbt_pm.PerformClick();
                        //rbt_bord.PerformClick();
                    }
                    else
                    {
                        MessageBox.Show("erreur date fin bord");
                    }
                }



            }
            // TODO : quand modification validée : le txt du buton reste à "ok" au lieu de "M"
            _etatBtnEquerresModifDateFin = !_etatBtnEquerresModifDateFin;
        }






        private void ckb_dateDebutVerrouillee_CheckedChanged(object sender, EventArgs e)
        {
            // On met à jour en base de données l'état de DateDebutPmVerrouillee
            if (rbt_pm.Checked)
            {
                _blocActuel.DateDebutPmVerrouillee = ckb_dateDebutVerrouillee.Checked;
                _BDDQuery.updateBloc(_blocActuel);
            }
        }
        private void ckb_dateFinVerrouillee_CheckedChanged(object sender, EventArgs e)
        {
            // On met à jour en base de données l'état de DateFinPmVerrouillee
            _blocActuel.DateFinPmVerrouillee = ckb_dateFinVerrouillee.Checked;
            _BDDQuery.updateBloc(_blocActuel);
        }




        private void btn_validerReservation_Click(object sender, EventArgs e)
        {
            if (_fenetreActuelle == "menuEquerres")
            {
                // Si la réservation concerne des équerres

                // Chargement du listView des types d'équerres
          
                List<int> nbEquerresDeChaqueType = new List<int>();
                _listeIdEquerresdDisponibles = _BDDQuery.getEquerresDisponibles(_blocActuel, rbt_pm.Checked, _hauteur, _charge);
                _listeTypeEquerres.Clear();
                string repereTypeEquerre = "";
                // On liste tous les types d'équerres disponibles et le nombre d'équerres de chaque type qui le sont aussi
                for (int k = 0; k < _listeIdEquerresdDisponibles.Count; k++)
                {
                    repereTypeEquerre = _BDDQuery.getRepereTypeEquerre(_listeIdEquerresdDisponibles[k]);
                    if (!_listeTypeEquerres.Contains(repereTypeEquerre))
                    {
                        _listeTypeEquerres.Add(repereTypeEquerre);
                        nbEquerresDeChaqueType.Add(1);
                    }
                    else
                    {
                        nbEquerresDeChaqueType[_listeTypeEquerres.IndexOf(repereTypeEquerre)]++;
                    }
                }

                if (_etatBtnValiderReservation == false)
                {
                    // Si on souhaite afficher le tableau des types d'équerres

                    if (_listeIdEquerresdDisponibles.Count < _nombre && _listeIdEquerresdDisponibles.Count != 0)
                    {
                        MessageBox.Show("Vous pourrez réserver au maximum " + _listeIdEquerresdDisponibles.Count.ToString() + " équerres");
                        nud_nombre.Value = _listeIdEquerresdDisponibles.Count;
                    }

                    // On supprime les panels code couleur créés précedemment
                    for (int k = 0; k < _listePanelsCodeCouleur.Count; k++)
                    {
                        Panel panel = (Panel)_listePanelsCodeCouleur[k];
                        lv_typeEquerresDisponibles.Controls.Remove(panel);
                    }
                    _listePanelsCodeCouleur.Clear();
                    lv_typeEquerresDisponibles.Clear();


                    // On crée les nouveaux panels code couleur
                    if (_listeTypeEquerres.Count != 0)
                    {
                        // Si des équerres correspondent aux critères de recherche
                        decimal ratio = 255 / _listeTypeEquerres.Count;
                        int rg = (int)Math.Round(ratio, 0);

                        ColumnHeader header = new ColumnHeader();
                        header.Text = "MyHeader";
                        header.Name = "MyColumn1";
                        header.Width = lv_typeEquerresDisponibles.Width;
                        lv_typeEquerresDisponibles.Columns.Add(header);

                        for (int k = 0; k < _listeTypeEquerres.Count; k++)
                        {
                            lv_typeEquerresDisponibles.Items.Add(_listeTypeEquerres[k]);
                            lv_typeEquerresDisponibles.Items[k].Font = new Font(lv_typeEquerresDisponibles.Font.FontFamily, 10);
                            Panel panel = new Panel();
                            panel.Location = new Point(210, -1 + 14 * k);
                            panel.Size = new Size(15, 15);
                            panel.Name = "pnlCodeCouleurTypeEquerre" + k.ToString();
                            panel.BackColor = Color.FromArgb(rg + rg * k, 255 - rg * k, 0);
                            _listePanelsCodeCouleur.Add(panel);
                            lv_typeEquerresDisponibles.Controls.Add(panel);
                        }
                        _etatBtnValiderReservation = true;
                        btn_validerReservation.Enabled = false;
                        lv_typeEquerresDisponibles.Visible = true;
                    }
                    else
                    {
                        // Sinon, on prévient l'utilisateur qu'aucune équerre ne peut répondre à son besoin
                        string s = rbt_bord.Checked && _blocActuel.DateFinBord.Year == 1 ? "\nAssurez vous de bien avoir renseigné la date de fin" : "";
                        MessageBox.Show("Aucune équerre correspondante n'a été trouvée." + s);
                        lv_typeEquerresDisponibles.Visible = false;
                        _etatBtnValiderReservation = false;
                        tb_charge.Enabled = true;
                        tb_charge.ReadOnly = false;
                        tb_hauteur.Enabled = true;
                        tb_hauteur.ReadOnly = false;
                        nud_nombre.Enabled = true;
                    }

                }
                else
                {
                    // Si on valide la réservation
                    tb_hauteur.ReadOnly = false;
                    tb_charge.ReadOnly = false;
                    nud_nombre.Enabled = true;

                    // On calcule le nombre maximum d'équerres disponibles qui appartiennent aux types d'équerres sélectionnés par l'utilisateur
                    int somme = 0;
                    for (int k = 0; k < lv_typeEquerresDisponibles.CheckedItems.Count; k++)
                    {
                        somme += nbEquerresDeChaqueType[_listeTypeEquerres.IndexOf(lv_typeEquerresDisponibles.CheckedItems[k].Text)];
                    }

                    if (somme >= _nombre)
                    {
                        // Si le choix des types d'équerres permet de réserver autant d'équerres que demandé
                        int nombreEquerresDuType;
                        TypeEquerre typeEquerre;
                        int nombreEquerresReservees = 0;
                        int idPropriete;

                        // On réserve les équerres en commençant par les équerres du type le plus en haut de la liste, puis on "descend"
                        for (int k = 0; k < lv_typeEquerresDisponibles.CheckedItems.Count; k++)
                        {
                            nombreEquerresDuType = nbEquerresDeChaqueType[_listeTypeEquerres.IndexOf(lv_typeEquerresDisponibles.CheckedItems[k].Text)];
                            for (int m = 0; m < _listeIdEquerresdDisponibles.Count; m++)
                            {
                                typeEquerre = _BDDQuery.getTypeEquerre(_listeIdEquerresdDisponibles[m]);
                                if (typeEquerre.Repere == lv_typeEquerresDisponibles.CheckedItems[k].Text)
                                {
                                    idPropriete = _BDDQuery.getIdPropriete(typeEquerre.Repere, _hauteur, _charge);
                                    _BDDQuery.reserverEquerre(_blocActuel, _listeIdEquerresdDisponibles[m], idPropriete, rbt_pm.Checked);
                                    nombreEquerresReservees++;
                                }
                                if (nombreEquerresDuType == 0) { break; }
                                if (nombreEquerresReservees == _nombre) { break; }
                            }
                            if (nombreEquerresReservees == _nombre) { break; }
                        }

                        rafraichirDgvEquerres();
                        lv_typeEquerresDisponibles.Visible = false;

                        // On clique 2x sur le bouton réservation pour ouvrir de nouveau le panel pour une nouvelle résa
                        btn_reservation.PerformClick();
                        btn_reservation.PerformClick();

                        // On supprime les panels code couleur
                        for (int k = 0; k < _listePanelsCodeCouleur.Count; k++)
                        {
                            Panel panel = (Panel)_listePanelsCodeCouleur[k];
                            lv_typeEquerresDisponibles.Controls.Remove(panel);
                        }
                        _listePanelsCodeCouleur.Clear();
                        lv_typeEquerresDisponibles.Clear();
                    }
                    else
                    {
                        // Si l'utilisateur n'a pas choisi assez de types, on l'avertit
                        MessageBox.Show("Votre sélection ne vous permet pas de réserver les " +_nombre.ToString() + " équerres demandées.\nÉlargissez vos choix ou bien modifiez le nombre d'équerres à réserver");
                    }

                }
            }
            else
            {
                // Si la réservation concerne des tins
           
                // Si une réservation pour ce type de tin existe déjà pour ce bloc, on met à jour le nombre de tins réservés
                if (_BDDQuery.reservationTinDejaExistante(_blocActuel, rbt_pm.Checked, cb_typeTin.SelectedItem.ToString()))
                {
                    _BDDQuery.updateNombreDeTinsReserves(_blocActuel, rbt_pm.Checked, cb_typeTin.SelectedItem.ToString(), _nombre, '+');
                }
                else // Sinon, on crée une nouvelle réservation
                {
                    _BDDQuery.reserverTin(_blocActuel, rbt_pm.Checked, cb_typeTin.SelectedItem.ToString(), _nombre);
                }
                _etatBtnValiderReservation = false;
                btn_reservation.PerformClick();
                rafraichirDgvTins();
            }
        }


        private void btn_reservation_Click(object sender, EventArgs e)
        {
            lv_typeEquerresDisponibles.Clear();
            lb_detailTypeEquerre.Items.Clear();

            Utilisateur utilisateur = _BDDQuery.getUtilisateurEnTrainDeReserver();
            if (utilisateur.Id == 0)
            {
                // Personne n'est en train de réserver : l'utilisateur actuel prend la main
                _utilisateurActuel.Main = true;
                _BDDQuery.updateUtilisateur(_utilisateurActuel);
                _etatBtnReserver = !_etatBtnReserver;
                pnl_reservation.Visible = _etatBtnReserver;
                btn_validerReservation.Enabled = false;
                btn_laisserLaMain.Visible = true;

                if (_fenetreActuelle == "menuEquerres")
                {
                    tb_hauteur.Text = "";
                    tb_charge.Text = "";
                }
                else
                {
                    // Chargement de la combobox avec les types de tins
                    cb_typeTin.DataSource = _BDDQuery.getListeOutillages();
                    cb_typeTin.SelectedIndex = 0;
                }
            }
            else if (utilisateur.Id == _utilisateurActuel.Id)
            {
                // Si l'utilisateur actuel a déjà la main
                _etatBtnReserver = !_etatBtnReserver;
                pnl_reservation.Visible = _etatBtnReserver;
                btn_validerReservation.Enabled = false;
                tb_hauteur.Text = "";
                tb_charge.Text = "";
                btn_laisserLaMain.Visible = true;

                if (_fenetreActuelle == "menuEquerres")
                {
                    nud_nombre.Value = 1;   
                    _nombre = 1;
                }
                else
                {
                    // Chargement de la combobox avec les types de tins
                    cb_typeTin.DataSource = _BDDQuery.getListeOutillages();
                    cb_typeTin.SelectedIndex = 0;
                    if (_BDDQuery.nombreDeTinsReserves(_blocActuel, rbt_pm.Checked, cb_typeTin.SelectedItem.ToString()) <= 0)
                    {
                        // todo : nud_nombre.Value = 0; pose parfois problème
                        nud_nombre.Value = 0;  
                        _nombre = 0;
                    }
                }
            }

        }







        private void dgv_apercuResasDuBloc_SelectionChanged(object sender, EventArgs e)
        {
            if (_chargementEnCours == false)
            {
                string repere = dgv_apercuResasDuBloc[0, dgv_apercuResasDuBloc.CurrentCell.RowIndex].Value.ToString();
                if (_utilisateurActuel.Rang == 1)
                {
                    // Si l'utilisateur est un membre du PM, on affiche la fiche technique de l'équerre sélectionnée
                    if (_fenetreActuelle == "menuEquerres")
                    {
                        _equerreActuelle = _BDDQuery.getEquerre(repere);
                        _equerreActuelle.TypeEquerre = _BDDQuery.getTypeEquerre(repere);

                        dgv_proprietesEquerre.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                        dgv_proprietesEquerre.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

                        dgv_proprietesEquerre.Rows.Clear();
                        dgv_proprietesEquerre.ColumnCount = 3;
                        dgv_proprietesEquerre.Columns[0].Name = "Hauteur";
                        dgv_proprietesEquerre.Columns[1].Name = "C.U";
                        dgv_proprietesEquerre.Columns[2].Name = "Transport";
                        for (int k = 0; k < 3; k++)
                        {
                            dgv_proprietesEquerre.Columns[k].Width = 150;
                            dgv_proprietesEquerre.Columns[k].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        }

                        List<Propriete> proprietes = _equerreActuelle.TypeEquerre.Proprietes;
                        int dernierIdAjoute = _equerreActuelle.TypeEquerre.Proprietes[0].Id;
                        int nombreLignesDgv = 0;
                        for (int k = 0; k < proprietes.Count; k++)
                        {
                            if (dernierIdAjoute == _equerreActuelle.TypeEquerre.Proprietes[k].Id && k != 0)
                            {
                                // Modification de la case Transport précédente si une propriété a plusieurs moyens de transport
                                dgv_proprietesEquerre[2, nombreLignesDgv - 1].Value += Environment.NewLine + proprietes[k].Transport.Libelle;
                            }
                            else
                            {
                                dgv_proprietesEquerre.Rows.Add(proprietes[k].Hauteur.ToString(), proprietes[k].Charge.ToString(), proprietes[k].Transport.Libelle);
                                dernierIdAjoute = _equerreActuelle.TypeEquerre.Proprietes[k].Id;
                                dgv_proprietesEquerre.Rows[nombreLignesDgv].ReadOnly = true;
                                nombreLignesDgv++;
                            }

                        }

                        // On charge l'image du type d'équerre dans la picture box
                        pb_MiniatureDocumentationEquerre.SizeMode = PictureBoxSizeMode.StretchImage;
                        string cheminImage;
                        btn_zoomerMiniature.Visible = false;
                        if (File.Exists(_equerreActuelle.TypeEquerre.CheminImage))
                        {
                            cheminImage = _equerreActuelle.TypeEquerre.CheminImage;
                            if (!_listeFenetresOuvertes.Contains(_equerreActuelle.TypeEquerre.Id))
                            {
                                btn_zoomerMiniature.Visible = true;
                            }
                        }
                        else
                        {
                            cheminImage = PersistanceParametres.CheminImageErreur;
                        }
                        FileStream fileStream = new FileStream(cheminImage, FileMode.Open);
                        pb_MiniatureDocumentationEquerre.Image = Image.FromStream(fileStream);
                        fileStream.Close();

                        lbl_titreFicheTechniqueEquerre.Text = "Fiche technique équerre " + repere;
                        pnl_ficheTechniqueEquerre.Visible = true;
                        lbl_typeEquerre.Text = "Type d'équerre : " + _equerreActuelle.TypeEquerre.Repere;
                        lbl_numeroPlan.Text = "Numéro du plan : " + _equerreActuelle.TypeEquerre.NumeroPlan;
                        lbl_semblable.Text = "Sem/Sym : " + ((_equerreActuelle.TypeEquerre.Semblable) ? "Semblable" : "Symétrique");
                        string[] arrayReglageHauteur = new string[3] { "Statique", "Palier", "Réglable" };
                        lbl_reglageHauteur.Text = "Hauteur : " + arrayReglageHauteur[_equerreActuelle.TypeEquerre.ReglageHauteur - 1];
                        rtb_remarqueEquerre.Text = _equerreActuelle.Remarque;
                        if (_equerreActuelle.Remarque == "") { lbl_aucuneRemarque.Visible = true; } else { lbl_aucuneRemarque.Visible = false; }
                        rtb_remarqueEquerre.ReadOnly = true;

                    }
                    else
                    {
                        // Si l'utilisateur est un membre de CM
                        dgv_apercuResasDuBloc.ClearSelection();
                    }
                }
            }
            else
            {
                pnl_ficheTechniqueEquerre.Visible = false;
            }
        }


        private void btn_zoomerMiniature_Click(object sender, EventArgs e)
        {
            if (!_listeFenetresOuvertes.Contains(_equerreActuelle.TypeEquerre.Id))
            {
                // Ouverture d'une fenêtre pour afficher l'image en plus grand 
                _listeFenetresOuvertes.Add(_equerreActuelle.TypeEquerre.Id);
                Form_AffichageImage formImage = new Form_AffichageImage(pb_MiniatureDocumentationEquerre.Image, _equerreActuelle, this);
                formImage.Visible = true;
            }
            btn_zoomerMiniature.Visible = false;
        }




        // ###################################################################################### \\
        // ###################################################################################### \\
        // ###################################################################################### \\
        private void btn_menuTins_Click(object sender, EventArgs e)
        {
            _chargementEnCours = true;

            lbl_stats_erreurBlocsAyantEuTypeEquerre.Visible = false;
            lbl_stats_erreurNombrePret.Visible = false;
            lbl_stats_erreurNombrePretsBloc.Visible = false;
            lbl_stats_erreurNombreReservationsParMois.Visible = false;
            lbl_stats_erreurNombreTravaux.Visible = false;
            lbl_stats_erreurTauxOccupation.Visible = false;

            chart_blocsAyantEuTypeEquerre.Visible = false;
            chart_nombrePrets.Visible = false;
            chart_nombrePretsBloc.Visible = false;
            chart_nombreReservationsParMois.Visible = false;
            chart_nombreTravaux.Visible = false;
            chart_tauxOccupation.Visible = false;

            cb_stats_typeEquerre.Visible = false;
            btn_gestion_modifier.Visible = false;
            btn_gestion_nouveau.Visible = false;
            btn_afficherPrets.Visible = false;
            tab_gestion.Visible = false;
            tmr_verificationDisponibiliteMain.Stop();
            _fenetreActuelle = "menuTins";
            btn_laisserLaMain.Visible = false;
            _etatBtnValiderReservation = false;
            dgv_apercuResasDuBloc.AllowUserToResizeColumns = false;
            dgv_apercuResasDuBloc.AllowUserToResizeRows = false;
            _dgvPageActuelle = 0;
            _dgvPageMax = 0;
            lbl_indicationNumeroPage.Visible = false;
            btn_pageSuivante.Visible = false;
            btn_pagePrecedente.Visible = false;

            //// ######## même chose que dans le load des equerres : simplifier avec une procédure ?

            // Hide et Show nécessaires pour l'affichage correct de la fenêtre
            pnl_infosBloc.Visible = true;
            pnl_arrierePlanMenuPrincipal.Visible = true;
            cb_modificationAnneeDateFin.Visible = false;
            cb_modificationSemaineDateFin.Visible = false;
            lbl_apercuResasDuBloc.Visible = false;
            dgv_apercuResasDuBloc.Visible = false;
            btn_reservation.Visible = false;
            pnl_reservation.Visible = false;
            cb_dateFin.Visible = true;
            pnl_ficheTechniqueEquerre.Visible = false;
            lv_typeEquerresDisponibles.Visible = false;
            lb_detailTypeEquerre.Visible = false;


            cb_stadeEtudeBloc.Items.Clear();
            cb_stadeEtudeBloc.Items.Add("PRELI");
            cb_stadeEtudeBloc.Items.Add("BPE");


            // Enabled , ReadOnly , Selected ...
            rbt_pm.PerformClick();
            cb_dateDebut.Enabled = false;
            cb_dateFin.Enabled = false;
            nud_nombre.ReadOnly = true;
            dgv_apercuResasDuBloc.ReadOnly = true;
            // Paramétrage des composants de la fenêtre

            cb_modificationAnneeDateFin.Items.Clear();
            cb_modificationSemaineDateFin.Items.Clear();

            tb_rechercheBloc.AutoCompleteMode = AutoCompleteMode.Suggest;
            tb_rechercheBloc.AutoCompleteSource = AutoCompleteSource.CustomSource;

            cb_navire.DataSource = _BDDQuery.getAllNavire();
            cb_navire.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_navire.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_dateDebut.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_dateDebut.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_dateFin.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_dateFin.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_modificationAnneeDateFin.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_modificationAnneeDateFin.DropDownHeight = cb_navire.ItemHeight * 10;

            cb_modificationSemaineDateFin.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_modificationSemaineDateFin.DropDownHeight = cb_navire.ItemHeight * 10;


            // On ajoute un string vide dans les cb affichant les dates et on sélectionne ce string vide par défaut.
            //  - Simplifie, pour plus tard, le fait d'afficher une date dans la cb en utilisant cb.Items[0] = "Texte"
            cb_dateDebut.Items.Add("");
            cb_dateFin.Items.Add("");
            cb_dateFin.SelectedIndex = 0;
            cb_dateDebut.SelectedIndex = 0;


            // Derniers paramétrages
            pnl_infosDetailleesBloc.Visible = false;         // Impossible de mettre plus haut dans le code, sinon on ne peut pas effectuer un click sur le rbt_pm

            cb_typeTin.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_typeTin.DropDownHeight = cb_navire.ItemHeight * 10;

            lbl_charge.Visible = false;
            lbl_hauteur.Visible = false;
            tb_charge.Visible = false;
            tb_hauteur.Visible = false;
            lbl_typeTin.Visible = true;
            cb_typeTin.Visible = true;

            lbl_reservationIndisponible.Visible = false;
            _chargementEnCours = false;
        }

        public AutoCompleteStringCollection setSource(List<string> liste)
        {
            // Transforme une List<string> en AutoCompleteStringCollection pour servir de dataSource aux textBox
            AutoCompleteStringCollection source = new AutoCompleteStringCollection();
            for(int k=0; k<liste.Count; k++)
            {
                source.Add(liste[k]);
            }
            return source;
        }



        private void rafraichirDgvTins()
        {
            // Met à jour le dataGridView aperçu des réservations de tins
            dgv_apercuResasDuBloc.AllowUserToAddRows = false;
            dgv_apercuResasDuBloc.RowHeadersVisible = false;

            dgv_apercuResasDuBloc.ColumnCount = 2;
            dgv_apercuResasDuBloc.Columns[0].Name = "Catégorie";
            dgv_apercuResasDuBloc.Columns[1].Name = "Nombre";
            dgv_apercuResasDuBloc.Rows.Clear();


            for (int k = 0; k < dgv_apercuResasDuBloc.ColumnCount; k++)
            {
                dgv_apercuResasDuBloc.Columns[k].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            dgv_apercuResasDuBloc.RowTemplate.Height = 30;

            List<string> listeDonneesDgv = _BDDQuery.getTinsReserves(_blocActuel, rbt_pm.Checked);

            // Calculs concernant le DGV (nombre de pages pouvant être affichées, hauteur du DGV, nombre de lignes à afficher...) 
            // Par défaut : 7 lignes par page
            int nombreDeLignesParPage = 7;
            int nombreLignes = listeDonneesDgv.Count / 2;
            if (nombreLignes > nombreDeLignesParPage)
            {
                _dgvPageMax = ((nombreLignes - (nombreLignes % nombreDeLignesParPage)) / nombreDeLignesParPage) - 1;
                _dgvPageMax = nombreLignes % nombreDeLignesParPage == 0 ? _dgvPageMax : _dgvPageMax + 1;
                btn_pageSuivante.Visible = _dgvPageActuelle < _dgvPageMax;
                btn_pagePrecedente.Visible = _dgvPageActuelle > 0;
            }
            else
            {
                _dgvPageMax = 0;
                btn_pageSuivante.Visible = false;
                btn_pagePrecedente.Visible = false;
            }
            int min = _dgvPageActuelle * nombreDeLignesParPage * 2;
            int max = _dgvPageActuelle == _dgvPageMax ? listeDonneesDgv.Count : nombreDeLignesParPage * 2 * (_dgvPageActuelle + 1);
            int nombreLignesAAfficher = (max - min) / 2;
            dgv_apercuResasDuBloc.Height = 30 * nombreLignesAAfficher + 23;
            lbl_indicationNumeroPage.Visible = true;
            lbl_indicationNumeroPage.Text = "Page " + (_dgvPageActuelle + 1).ToString() + "/" + (_dgvPageMax + 1).ToString();


            if (_utilisateurActuel.Rang > 1)
            {
                // Si l'utilisateur est un membre de l'équipe CM

                // On supprime les boutons et les numericUpDown créés précedemment
                for (int k = 0; k < _listeObjetsCreesDynamiquement.Count; k += 3)
                {
                    Button button = (Button)_listeObjetsCreesDynamiquement[k];
                    dgv_apercuResasDuBloc.Controls.Remove(button);
                    NumericUpDown nud = (NumericUpDown)_listeObjetsCreesDynamiquement[k + 1];
                    dgv_apercuResasDuBloc.Controls.Remove(nud);
                }
                _listeObjetsCreesDynamiquement.Clear();

                for (int k = min; k < max; k += 2)
                {
                    // On affiche les données dans le dgv
                    dgv_apercuResasDuBloc.Rows.Add(listeDonneesDgv[k], listeDonneesDgv[k + 1]);
                    int m = (k-min) / 2;

                    // On crée des boutons sur chaque ligne du dgv
                    Button button = new Button();
                    button.Location = new Point(410, 23 + 30 * m);
                    button.Size = new Size(25, 25);
                    button.Name = "btnModifNombreTins" + m.ToString();
                    button.BackColor = System.Drawing.Color.Gainsboro;        // dimgray
                    button.Font = new Font(button.Font.FontFamily, 6, FontStyle.Bold);
                    button.Text = "M";
                    button.Click += new EventHandler(btn_modifNombreTins_Click);
                    _listeObjetsCreesDynamiquement.Add(button);

                    // ainsi qu'un numericUpDown pour pouvoir modifier le nombre de réservations
                    NumericUpDown nud = new NumericUpDown();
                    nud.Location = new Point(450, 23 + 30 * m);
                    nud.Size = new Size(40, 30);
                    nud.Name = "nudModifNombreTins" + m.ToString();
                    nud.Minimum = 1;
                    nud.Maximum = Convert.ToInt32(listeDonneesDgv[k + 1]);
                    nud.ReadOnly = true;
                    nud.Visible = false;
                    _listeObjetsCreesDynamiquement.Add(nud);
                    _listeObjetsCreesDynamiquement.Add(false);

                    dgv_apercuResasDuBloc.Controls.Add(button);
                    dgv_apercuResasDuBloc.Controls.Add(nud);
                    // _listeObjetsCreesDynamiquement : [0] Bouton   [1] NumericUpDown   [2] EtatBouton
                }
            }
            else
            {
                // Si l'utilisateur est PM, on affiche uniquement les données dans le dgv
                for (int k = min; k < max; k += 2)
                {
                    dgv_apercuResasDuBloc.Rows.Add(listeDonneesDgv[k], listeDonneesDgv[k + 1]);
                }
            }
        }

        private void rafraichirDgvEquerres()
        {
            dgv_apercuResasDuBloc.Rows.Clear();
            _chargementEnCours = true;

            List<string> listeDonneesDgv = new List<string>();
            if (_utilisateurActuel.Rang == 1)
            {
                // Tableau pour équipe PM
                if (_etatBtnAfficherPret == false)
                {
                    // Préparation du dgv pour affichage des équerres réservées
                    dgv_apercuResasDuBloc.ColumnCount = 4;
                    dgv_apercuResasDuBloc.Columns[0].Name = "Repère";
                    dgv_apercuResasDuBloc.Columns[1].Name = "Hauteur";
                    dgv_apercuResasDuBloc.Columns[2].Name = "C.U";
                    dgv_apercuResasDuBloc.Columns[3].Name = "Classe";
                    listeDonneesDgv = _BDDQuery.getAllEquerreBloc(_blocActuel, rbt_pm.Checked, false);
                }
                else
                {
                    // Préparation du dgv pour affichage des équerres prêtées
                    dgv_apercuResasDuBloc.ColumnCount = 5;
                    dgv_apercuResasDuBloc.Columns[0].Name = "Repère";
                    dgv_apercuResasDuBloc.Columns[1].Name = "Hauteur";
                    dgv_apercuResasDuBloc.Columns[2].Name = "C.U";
                    dgv_apercuResasDuBloc.Columns[3].Name = "Classe";
                    dgv_apercuResasDuBloc.Columns[4].Name = "DateFin";
                    listeDonneesDgv = _BDDQuery.getAllPretsBloc(_blocActuel, rbt_pm.Checked, false);
                }
                if (listeDonneesDgv.Count > 0) { pnl_ficheTechniqueEquerre.Visible = true; } else { pnl_ficheTechniqueEquerre.Visible = false; }
            }
            else
            {
                // Tableau simplifié pour équipe CM

                if (_etatBtnAfficherPret == false)
                {
                    // Préparation du dgv pour affichage des équerres réservées
                    dgv_apercuResasDuBloc.ColumnCount = 4;
                    dgv_apercuResasDuBloc.Columns[0].Name = "Type Équerre";
                    dgv_apercuResasDuBloc.Columns[1].Name = "Nombre";
                    dgv_apercuResasDuBloc.Columns[2].Name = "Hauteur";
                    dgv_apercuResasDuBloc.Columns[3].Name = "C.U";
                    listeDonneesDgv = _BDDQuery.getAllEquerreBloc(_blocActuel, rbt_pm.Checked, true);
                }
                else
                {
                    // Préparation du dgv pour affichage des équerres prêtées
                    dgv_apercuResasDuBloc.ColumnCount = 5;
                    dgv_apercuResasDuBloc.Columns[0].Name = "Type Équerre";
                    dgv_apercuResasDuBloc.Columns[1].Name = "Nombre";
                    dgv_apercuResasDuBloc.Columns[2].Name = "Hauteur";
                    dgv_apercuResasDuBloc.Columns[3].Name = "C.U";
                    dgv_apercuResasDuBloc.Columns[4].Name = "DateFin";
                    listeDonneesDgv = _BDDQuery.getAllPretsBloc(_blocActuel, rbt_pm.Checked, true);
                    
                }

                // Suppression des boutons + numeric up down créés précedemment
                for (int k = 0; k < _listeObjetsCreesDynamiquement.Count; k += 3)
                {
                    Button button = (Button)_listeObjetsCreesDynamiquement[k];
                    dgv_apercuResasDuBloc.Controls.Remove(button);
                    NumericUpDown nud = (NumericUpDown)_listeObjetsCreesDynamiquement[k + 1];
                    dgv_apercuResasDuBloc.Controls.Remove(nud);
                }
                _listeObjetsCreesDynamiquement.Clear();
                
            }

            // Paramétrage du tableau
            for (int k = 0; k < dgv_apercuResasDuBloc.ColumnCount; k++)
            {
                dgv_apercuResasDuBloc.Columns[k].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            dgv_apercuResasDuBloc.RowTemplate.Height = 30;

            // Calculs concernant le DGV (nombre de pages pouvant être affichées, hauteur du DGV, nombre de lignes à afficher...) 
            // Par défaut : 7 lignes par page
            int nombreDeLignesParPage = 7;
            int nombreDeColonnes = dgv_apercuResasDuBloc.Columns.Count;
            int nombreLignes = listeDonneesDgv.Count / nombreDeColonnes;
            if (nombreLignes > nombreDeLignesParPage)
            {
                _dgvPageMax = ((nombreLignes - (nombreLignes % nombreDeLignesParPage)) / nombreDeLignesParPage) - 1;
                _dgvPageMax = nombreLignes % nombreDeLignesParPage == 0 ? _dgvPageMax : _dgvPageMax + 1;
                btn_pageSuivante.Visible = _dgvPageActuelle < _dgvPageMax;
                btn_pagePrecedente.Visible = _dgvPageActuelle > 0;
            }
            else
            {
                _dgvPageMax = 0;
                btn_pageSuivante.Visible = false;
                btn_pagePrecedente.Visible = false;
            }
            int min = _dgvPageActuelle * nombreDeLignesParPage * nombreDeColonnes;
            int max = _dgvPageActuelle == _dgvPageMax ? listeDonneesDgv.Count : nombreDeLignesParPage * nombreDeColonnes * (_dgvPageActuelle+1);
            int nombreLignesAAfficher = (max - min) / nombreDeColonnes;
            dgv_apercuResasDuBloc.Height = 30 * nombreLignesAAfficher + 23;
            lbl_indicationNumeroPage.Visible = true;
            lbl_indicationNumeroPage.Text = "Page " + (_dgvPageActuelle + 1).ToString() + "/" + (_dgvPageMax + 1).ToString();

            int decalageX = _etatBtnAfficherPret ? 100 : 0;
            if (nombreLignesAAfficher > 0)
            {
                // Si il y a des données à afficher
                for (int k = min; k < max; k += nombreDeColonnes)
                {
                    // Remplissage du DGV avec les données à afficher
                    if (_utilisateurActuel.Rang != 1)
                    {
                        if(_etatBtnAfficherPret == false)
                        {
                            dgv_apercuResasDuBloc.Rows.Add(listeDonneesDgv[k], listeDonneesDgv[k + 1], listeDonneesDgv[k + 2], listeDonneesDgv[k + 3]);
                        }
                        else
                        {
                            dgv_apercuResasDuBloc.Rows.Add(listeDonneesDgv[k], listeDonneesDgv[k + 1], listeDonneesDgv[k + 2], listeDonneesDgv[k + 3], listeDonneesDgv[k+4]);
                        }
                        
                        // On associe à chaque ligne à afficher, un bouton ainsi qu'un numeric up down que l'on positionne à la hauteur de la ligne en question
                        int m = (k - min) / nombreDeColonnes;

                        // Création dynamique du bouton
                        Button button = new Button();
                        button.Location = new Point(410 + decalageX, 23 + 30 * m);
                        button.Size = new Size(25, 25);
                        button.Name = "btnModifNombreEquerres" + m.ToString();
                        button.BackColor = System.Drawing.Color.Gainsboro;
                        button.Font = new Font(button.Font.FontFamily, 6, FontStyle.Bold);
                        button.Text = "M";
                        button.Click += new EventHandler(btn_modifNombreEquerres_Click);
                        _listeObjetsCreesDynamiquement.Add(button);

                        // Création dynamique du numeric up down
                        NumericUpDown nud = new NumericUpDown();
                        nud.Location = new Point(450 + decalageX, 23 + 31 * m);
                        nud.Size = new Size(40, 30);
                        nud.Name = "nudModifNombreEquerres" + m.ToString();
                        nud.Minimum = 1;
                        nud.Maximum = Convert.ToInt32(listeDonneesDgv[k + 1]);
                        nud.ReadOnly = true;
                        nud.Visible = false;
                        _listeObjetsCreesDynamiquement.Add(nud);
                        _listeObjetsCreesDynamiquement.Add(false);

                        dgv_apercuResasDuBloc.Controls.Add(button);
                        dgv_apercuResasDuBloc.Controls.Add(nud);
                    }
                    else
                    {
                        dgv_apercuResasDuBloc.Rows.Add(listeDonneesDgv[k], listeDonneesDgv[k + 1], listeDonneesDgv[k + 2], listeDonneesDgv[k + 3]);
                    }
                }
            }

            dgv_apercuResasDuBloc.ClearSelection();
            _chargementEnCours = false;

        }



        private void btn_menuGestion_Click(object sender, EventArgs e)
        {
            // Cache / affiche certains composants de la fenêtre
            _fenetreActuelle = "menuGestion";
            pnl_arrierePlanMenuPrincipal.Visible = true;
            pnl_ficheTechniqueEquerre.Visible = false;
            pnl_infosBloc.Visible = false;
            pnl_reservation.Visible = false;
            lbl_apercuResasDuBloc.Visible = false;
            lbl_indicationNumeroPage.Visible = false;
            btn_pagePrecedente.Visible = false;
            btn_pageSuivante.Visible = false;
            btn_reservation.Visible = false;
            dgv_apercuResasDuBloc.Visible = false;
            lbl_reservationIndisponible.Visible = false;
            btn_laisserLaMain.Visible = false;
            tab_gestion.Visible = true;
            btn_afficherPrets.Visible = false;
            lbl_stats_erreurBlocsAyantEuTypeEquerre.Visible = false;
            lbl_stats_erreurNombrePret.Visible = false;
            lbl_stats_erreurNombrePretsBloc.Visible = false;
            lbl_stats_erreurNombreReservationsParMois.Visible = false;
            lbl_stats_erreurNombreTravaux.Visible = false;
            lbl_stats_erreurTauxOccupation.Visible = false;
            cb_stats_typeEquerre.Visible = false;
            chart_blocsAyantEuTypeEquerre.Visible = false;
            chart_nombrePrets.Visible = false;
            chart_nombrePretsBloc.Visible = false;
            chart_nombreReservationsParMois.Visible = false;
            chart_nombreTravaux.Visible = false;
            chart_tauxOccupation.Visible = false;
        }

        // Evenements concernant le dgv des utilisateurs. Reprise d'un code qui fonctionnait
        /*
        private void dgv_apercuResasDuBloc_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            int x = dgv.CurrentCell.ColumnIndex;
            int y = dgv.CurrentCell.RowIndex;
            dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            if (x == 3)
            {
                Color[] couleursDroits = new Color[5] { Color.Red, Color.Green, Color.Orange, Color.BlueViolet, Color.Blue };
                int index = Convert.ToInt32(dgv[x, y].Value.ToString());
                dgv.Rows[y].Cells[3].Style.ForeColor = couleursDroits[index];
                dgv.CurrentCell = dgv[0, y];
            }
        }

        private void dgv_apercuResasDuBloc_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //if (_x == 4)
            //{
                Color[] couleursDroits = new Color[5] { Color.Red, Color.Green, Color.Orange, Color.BlueViolet, Color.Blue };
                int index = Convert.ToInt32(dgv_gestion_utilisateurs[_x, _y].Value.ToString());
                dgv_gestion_utilisateurs.Rows[_y].Cells[4].Style.ForeColor = couleursDroits[index];
            //}

            //int x = dgv_gestion_utilisateurs.CurrentCell.ColumnIndex;
            //int y = dgv_gestion_utilisateurs.CurrentCell.RowIndex;
            //if (x == 4)
            //{
            //    dgv_gestion_utilisateurs.Rows[y].Cells[4].Style.ForeColor = Color.Black;
            //}
        }

        private void dgv_apercuResasDuBloc_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

        }
        */
    

        // Fonctions présentes dans BDDQuery
        public int getSemaine(DateTime fromDate)
        {
            // return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
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
        public DateTime getPremierJourSemaine(int annee, int numeroSemaine)
        {
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
        public DateTime getDateModifiee(DateTime dateInitiale, int nbSemaines)
        {
            return CultureInfo.InvariantCulture.Calendar.AddWeeks(dateInitiale, nbSemaines);
        }

        // Accesseur
        public List<int> ListeFenetresOuvertes { get { return _listeFenetresOuvertes; } set { _listeFenetresOuvertes = value; } }

        // Accessible depuis FORM_AffichageImage
        public void RafraichirEtatBoutonZoom(int idTypeEquerre)
        {
            if(_equerreActuelle.TypeEquerre.Id == idTypeEquerre)
            {
                btn_zoomerMiniature.Visible = true;
            }
        }

        private void dgv_apercuResasDuBloc_ficheTechniqueProprietes_SelectionChanged(object sender, EventArgs e)
        {
            // Empêche la sélection d'une cellule dans le dgv des propriétés
            dgv_proprietesEquerre.ClearSelection();  
        }


        private void cb_stadeEtudeBloc_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Met à jour le stade étude d'un bloc en fonction du chantier sélectionné
            if(rbt_pm.Checked)
            {
                _blocActuel.StadeEtudePm = Convert.ToBoolean(cb_stadeEtudeBloc.SelectedIndex);
            }
            else
            {
                _blocActuel.StadeEtudeBord = Convert.ToBoolean(cb_stadeEtudeBloc.SelectedIndex);
            }
            _BDDQuery.updateBloc(_blocActuel);
        }

        void btn_modifNombreEquerres_Click(object sender, System.EventArgs e)
        {
            Button button = sender as Button;
            int index = Convert.ToInt32(button.Name.Replace("btnModifNombreEquerres", ""));
            NumericUpDown nud = (NumericUpDown)_listeObjetsCreesDynamiquement[index * 3 + 1];
            if( (bool)_listeObjetsCreesDynamiquement[index*3+2] == false )
            {
                // Si le bouton est cliqué pour la première fois, on propose à l'utilisateur de modifier le nombre d'équerres
                nud.Visible = true;
                button.Text = "O";
                _listeObjetsCreesDynamiquement[index * 3 + 2] = true;
            }
            else
            {
                // Si le bouton est cliqué pour la deuxième fois
                string s, complementTitre;
                if(nud.Value>1)
                {
                    s="s";
                    complementTitre="des équerres";
                }
                else
                {
                    s="";
                    complementTitre="une équerre";
                }
                string bloc = _blocActuel.Repere[0]=='0' ? "bloc" : "panneau";
                _typeEquerreActuelle = _BDDQuery.getTypeEquerreViaRepere(dgv_apercuResasDuBloc[0, index].Value.ToString());
                // On demande confirmation à l'utilisateur avant de modifier le nombre d'équerres
                DialogResult dialogResult = MessageBox.Show("Êtes-vous certain de vouloir retirer "+nud.Value.ToString()+" équerre"+s+" de type "+_typeEquerreActuelle.Repere+" ?", "Retirer "+complementTitre+" du "+bloc+" "+_blocActuel.Repere+" ?", MessageBoxButtons.YesNo);
                _listeObjetsCreesDynamiquement[index * 3 + 2] = false;
                if (dialogResult == DialogResult.Yes)
                {
                    // Si l'utilisateur accepte, on modifie le nombre d'équerres 
                    if (_etatBtnAfficherPret == false)
                    {
                        _BDDQuery.retirerEquerresBloc(_blocActuel, rbt_pm.Checked, _typeEquerreActuelle, Convert.ToInt32(nud.Value), Convert.ToInt32(dgv_apercuResasDuBloc[2, index].Value), Convert.ToInt32(dgv_apercuResasDuBloc[3, index].Value));
                    }
                    else
                    {
                        _BDDQuery.retirerPretEquerreBloc(_blocActuel, rbt_pm.Checked, _typeEquerreActuelle, Convert.ToInt32(nud.Value), Convert.ToInt32(dgv_apercuResasDuBloc[2, index].Value), Convert.ToInt32(dgv_apercuResasDuBloc[3, index].Value));
                    }
                    // On rafraîchit le dgv
                    _dgvPageActuelle = _dgvPageActuelle > 0 ? _dgvPageActuelle-1 : _dgvPageActuelle;
                    rafraichirDgvEquerres();
                }
                nud.Visible = false;
                button.Text = "M";
                
            }

        }


        void btn_modifNombreTins_Click(object sender, System.EventArgs e)
        {
            Button button = sender as Button;
            int index = Convert.ToInt32(button.Name.Replace("btnModifNombreTins", ""));
            NumericUpDown nud = (NumericUpDown)_listeObjetsCreesDynamiquement[index * 3 + 1];
            if ((bool)_listeObjetsCreesDynamiquement[index * 3 + 2] == false)
            {
                // Si le bouton est cliqué pour la première fois, on propose à l'utilisateur de modifier le nombre de tins
                nud.Visible = true;
                button.Text = "O";
                _listeObjetsCreesDynamiquement[index * 3 + 2] = true;
            }
            else
            {
                // Si le bouton est cliqué pour la deuxième fois
                string s, complementTitre;
                if (nud.Value > 1)
                {
                    s = "s";
                    complementTitre = "des tins";
                }
                else
                {
                    s = "";
                    complementTitre = "un tin";
                }
                string bloc = _blocActuel.Repere[0] == '0' ? "bloc" : "panneau";
                string typeTin = dgv_apercuResasDuBloc[0, index].Value.ToString();
                // On demande confirmation à l'utilisateur avant de modifier le nombre d'équerres
                DialogResult dialogResult = MessageBox.Show("Êtes-vous certain de vouloir retirer " + nud.Value.ToString() + " tin" + s + " de type '" +typeTin+ "'" + " ?", "Retirer " + complementTitre + " du " + bloc + " " + _blocActuel.Repere + " ?", MessageBoxButtons.YesNo);
                _listeObjetsCreesDynamiquement[index * 3 + 2] = false;
                if (dialogResult == DialogResult.Yes)
                {
                    // Si l'utilisateur accepte, on modifie le nombre d'équerres
                    _BDDQuery.updateNombreDeTinsReserves(_blocActuel, rbt_pm.Checked, typeTin, Convert.ToInt32(nud.Value),'-');
                    _dgvPageActuelle = _dgvPageActuelle > 0 ? _dgvPageActuelle - 1 : _dgvPageActuelle;
                    // On rafraîchit le dgv
                    rafraichirDgvTins();
                    btn_reservation.PerformClick();
                    btn_reservation.PerformClick();
                }

                nud.Visible = false;
                button.Text = "M";

            }

        }




        private void lb_detailTypeEquerre_SelectedIndexChanged(object sender, EventArgs e)
        {
            // https://stackoverflow.com/questions/3720012/regular-expression-to-split-string-and-number

            if (lv_typeEquerresDisponibles.SelectedIndices.Count > 0)
            {
                // Si un type d'équerre est cliqué dans la liste, on affiche toutes les équerres du type sélectionné
                int index = lv_typeEquerresDisponibles.SelectedIndices[0];
                string repere = lv_typeEquerresDisponibles.Items[index].Text;

                List<string> listeReperes = _BDDQuery.getReperesEquerresDuType(repere);
                lb_detailTypeEquerre.Items.Clear();
                lb_detailTypeEquerre.Visible = true;
                if (listeReperes.Count > 0)
                {
                    // Permet d'afficher les équerres de la manière suivante :
                    // E400 + E401 + E402 + E403 + E405 +E406 --> E400 à E403   E405 + E406
                    Regex regex = new Regex("(?<Alpha>[a-zA-Z]*)(?<Numeric>[0-9]*)");
                    Match match = regex.Match(listeReperes[0]);

                    string stringDebut = match.Groups["Alpha"].Value;
                    int min = Convert.ToInt32(match.Groups["Numeric"].Value);
                    int max = min;
                    int numActuel = min;
                    string complement = "";
                    string a = "";
                    if (listeReperes.Count != 1)
                    {
                        for (int k = 1; k < listeReperes.Count; k++)
                        {
                            match = regex.Match(listeReperes[k]);
                            numActuel = Convert.ToInt32(match.Groups["Numeric"].Value);

                            if (numActuel == max + 1)
                            {
                                max = numActuel;
                            }
                            else
                            {
                                a = max == min + 1 ? " + " : " à ";
                                complement = min == max ? "" : a + stringDebut + max.ToString();
                                lb_detailTypeEquerre.Items.Add(stringDebut + min.ToString() + complement);
                                min = numActuel;
                                max = min;
                            }
                            if (k == listeReperes.Count - 1)
                            {
                                max = numActuel;
                                a = max == min + 1 ? " + " : " à ";
                                complement = min == max ? "" : a + stringDebut + max.ToString();
                                lb_detailTypeEquerre.Items.Add(stringDebut + min.ToString() + complement);
                            }
                        }
                    }
                    else
                    {
                        lb_detailTypeEquerre.Items.Add(stringDebut + min.ToString() + complement);
                    }
                }
            }
        }

        private void lb_detailTypeEquerre_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            // Si l'utilisateur sélectionne au moins un type d'équerre, on lui permet de valider sa réservation en activant le bouton
            lb_detailTypeEquerre.Visible = false;
            if (lv_typeEquerresDisponibles.CheckedItems.Count > 0)
            {
                _etatBtnValiderReservation = true;
                btn_validerReservation.Enabled = true;
            }
            else
            {
                _etatBtnValiderReservation = false;
                btn_validerReservation.Enabled = false;
            }
        }

        // Si la valeur saisie / choisie 
        private void tb_hauteur_TextChanged(object sender, EventArgs e)
        {
            btn_validerReservation.Enabled = false;
            if (estUnEntier(tb_hauteur.Text))
            {
                if (_etatBtnValiderReservation == false)
                {
                    _hauteur = Convert.ToInt32(tb_hauteur.Text);
                }
                else
                {
                    // Si la valeur change alors que le bouton a déjà été cliqué, cache la liste des types d'équerres
                    _etatBtnValiderReservation = false;
                    lv_typeEquerresDisponibles.Visible = false;
                }
                btn_validerReservation.Enabled = estUnEntier(tb_charge.Text);
            }
            else
            {
                if (tb_hauteur.Text.Length >= 1)
                {
                    tb_hauteur.Text = tb_hauteur.Text.Substring(0, tb_hauteur.Text.Length - 1);
                    tb_hauteur.SelectionStart = tb_hauteur.Text.Length;
                }
            }

        }
        private void tb_charge_TextChanged(object sender, EventArgs e)
        {
            btn_validerReservation.Enabled = false;
            if (estUnEntier(tb_charge.Text))
            {
                if (_etatBtnValiderReservation == false)
                {
                    _charge = Convert.ToInt32(tb_charge.Text);
                }
                else                                         
                {
                    // Si la valeur change alors que le bouton a déjà été cliqué, cache la liste des types d'équerres
                    _etatBtnValiderReservation = false;
                    lv_typeEquerresDisponibles.Visible = false;
                }
                btn_validerReservation.Enabled = estUnEntier(tb_hauteur.Text);
            }
            else
            {
                if (tb_charge.Text.Length >= 1)
                {
                    tb_charge.Text = tb_charge.Text.Substring(0, tb_charge.Text.Length - 1);
                    tb_charge.SelectionStart = tb_charge.Text.Length;
                }
            }
        }
        private void nud_nombre_ValueChanged(object sender, EventArgs e)
        {
            if (_fenetreActuelle == "menuEquerres")
            {
                btn_validerReservation.Enabled = false;
                if (estUnEntier(tb_hauteur.Text) && estUnEntier(tb_charge.Text))
                {
                    btn_validerReservation.Enabled = true;
                    if (_etatBtnValiderReservation == false)  
                    {
                        _nombre = Convert.ToInt32(nud_nombre.Value);
                    }
                    else                                              
                    {
                        // Si la valeur change alors que le bouton a déjà été cliqué, cache la liste des types d'équerres
                        _etatBtnValiderReservation = false;
                        lv_typeEquerresDisponibles.Visible = false;
                    }

                }
            }
            else
            {
                _nombre = Convert.ToInt32(nud_nombre.Value);
            }
        }


        /// <summary>
        /// Retourne TRUE si {texte} est un entier, sinon retourne FALSE
        /// </summary>
        /// <param name="texte"></param>
        /// <returns></returns>
        private bool estUnEntier(string texte)
        {
            //  https://stackoverflow.com/questions/273141/regex-for-numbers-only
            Regex regex = new Regex("^[0-9]+$");
            return regex.IsMatch(texte);
        }



        private void tmr_verificationDisponibiliteMain_Tick(object sender, EventArgs e)
        {
            // Lorsque le timer tick, récupération de l'utilisateur ayant la main
            Utilisateur utilisateur = _BDDQuery.getUtilisateurEnTrainDeReserver();
            if (utilisateur.Id == 0  ||  utilisateur.Id == _utilisateurActuel.Id)
            {
                // Si personne n'a la main, affiche le bouton permettant de réserver
                btn_reservation.Enabled = true;
                lbl_reservationIndisponible.Visible = false;
                lbl_reservationIndisponible.Text = "";
            }
            else
            {
                // Empêche l'utilisateur de réserver
                btn_reservation.Enabled = false;
                lbl_reservationIndisponible.Visible = true;
                lbl_reservationIndisponible.Text = "Une réservation est en cours.\n" + utilisateur.Nom + " " + utilisateur.Prenom + "   [" + utilisateur.Trigramme.ToUpper() + "]";
            }
        }

        private void Form_MenuPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Lorsque la fenêtre se ferme
            if (pnl_parametres_connexion.Visible == false)
           {
                // L'utilisateur actuel perd la main
                _utilisateurActuel.Main = false;
                _BDDQuery.updateUtilisateur(_utilisateurActuel);
           }
        }



        private void btn_laisserLaMain_Click(object sender, EventArgs e)
        {
            // L'utilisateur actuel laisse la main
            btn_reservation.PerformClick();
            if(pnl_reservation.Visible == true)
            {
                btn_reservation.PerformClick();
            }
            _utilisateurActuel.Main = false;
            _BDDQuery.updateUtilisateur(_utilisateurActuel);
            btn_laisserLaMain.Visible = false;
        }

        private void cb_typeTin_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_typeTin.SelectedItem.ToString() != "")
            {
                // Lorsque le type de tin change
                btn_validerReservation.Enabled = true;
                string typeTin = cb_typeTin.SelectedItem.ToString();
                int maximumDuType = _BDDQuery.getNombreTinsMaximumDuType(typeTin);
                int nombreDejaReserves = _BDDQuery.nombreDeTinsReserves(_blocActuel, rbt_pm.Checked, cb_typeTin.SelectedItem.ToString());
                // On valorise les attributs Minimum, Maximum et Value du numericUpDown nud_nombre
                // De cette manière on ne peut pas réserver plus de tins qu'il n'y en a de disponibles
                if(maximumDuType > nombreDejaReserves)
                {
                    nud_nombre.Minimum = 1;
                    nud_nombre.Value = 1;
                    nud_nombre.Maximum = maximumDuType - nombreDejaReserves;
                }
                else
                {
                    nud_nombre.Minimum = 0;
                    nud_nombre.Value = 0;
                    nud_nombre.Maximum = 0;
                }

                // MessageBox.Show(String.Format("Maximum du type : {0}\nNombre de tins déjà réservés : {1}",maximumDuType,nombreDejaReserves));
            }
        }


        private void btn_pageSuivante_Click(object sender, EventArgs e)
        {
            // On affiche la page suivante du dgv
            _dgvPageActuelle++;
            if (_fenetreActuelle == "menuEquerres")
            {
                rafraichirDgvEquerres();
            }
            else
            {
                rafraichirDgvTins();
            }
        }


        private void btn_pagePrecedente_Click(object sender, EventArgs e)
        {
            // On affiche la page précédente du dgv
            _dgvPageActuelle--;
            if (_fenetreActuelle == "menuEquerres")
            {
                rafraichirDgvEquerres();
            }
            else
            {
                rafraichirDgvTins();
            }
        }


        private void tab_gestion_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Lorsque l'onglet de tab_gestion change
            switch(tab_gestion.SelectedIndex)
            {
                // Équerres
                case 0:
                    break;

                // Types Équerres
                case 1:
                    cb_gestion_typesEquerres.DataSource = _BDDQuery.getAllReperesTypesEquerres();
                    
                    btn_gestion_modifier.Visible = true;
                    btn_gestion_nouveau.Visible = true;

                    pb_gestion_miniatureDocumentation.Visible = false;
                    btn_gestion_parcourirImage.Visible = false;
                    tb_gestion_repere.Visible = false;
                    tb_gestion_numeroPlan.Visible = false;
                    rbt_gestion_semblable.Visible = false;
                    rbt_gestion_symetrique.Visible = false;
                    cb_gestion_reglageHauteur.Visible = false;
                    btn_gestion_validerModifications.Visible = false;
                    grpBox_gestion_SemSym.Visible = false;
                    lbl_gestion_repere.Visible = false;
                    lbl_gestion_numeroPlan.Visible = false;
                    lbl_gestion_reglageHauteur.Visible = false;

                    

                    break;

                // Tins
                case 2:
                    break;

                // Travaux
                case 3:
                    break;

                // Navires
                case 4:
                    break;

                // Transports
                case 5:
                    break;

                // Utilisateurs
                case 6:
                    // todo : enregistrer changements en BDD
                    // todo : code couleur des rangs
                    dgv_gestion_utilisateurs.Rows.Clear();
                    dgv_gestion_utilisateurs.Visible = true;
                    //dgv_gestion_utilisateurs.ReadOnly = false;
                    dgv_gestion_utilisateurs.Width = 592+20;
                    dgv_gestion_utilisateurs.ScrollBars = ScrollBars.Vertical;
                    dgv_gestion_utilisateurs.AllowUserToAddRows = false;
                    dgv_gestion_utilisateurs.AllowUserToResizeRows = false;
                    dgv_gestion_utilisateurs.AllowUserToResizeColumns = false;
                    dgv_gestion_utilisateurs.RowHeadersVisible = false;

                    dgv_gestion_utilisateurs.ColumnCount = 3;
                    dgv_gestion_utilisateurs.Columns[0].Name = "Trigramme";
                    dgv_gestion_utilisateurs.Columns[1].Name = "Nom";
                    dgv_gestion_utilisateurs.Columns[2].Name = "Prénom";
                    dgv_gestion_utilisateurs.Columns[0].ReadOnly = true;
                    dgv_gestion_utilisateurs.Columns[0].Width = 90;
                    dgv_gestion_utilisateurs.Columns[1].Width = 175;
                    dgv_gestion_utilisateurs.Columns[2].Width = 175;
                    for (int k = 0; k < dgv_gestion_utilisateurs.ColumnCount; k++)
                    {
                        dgv_gestion_utilisateurs.Columns[k].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    List<string> listeDonneesDgv = _BDDQuery.getAllUtilisateurs();



                    DataTable droits = new DataTable();
                    droits.Columns.Add("Item");
                    droits.Columns.Add("Value");
                    droits.Rows.Add("Bloqué", "0");
                    droits.Rows.Add("Visiteur", "1");
                    droits.Rows.Add("Contributeur", "2");
                    droits.Rows.Add("Manager", "3");
                    droits.Rows.Add("Administrateur", "4");

                    DataGridViewComboBoxColumn cbColonne = new DataGridViewComboBoxColumn();
                    // cbColonne.ReadOnly = false;
                    cbColonne.Width = 150;
                    cbColonne.DataSource = droits;
                    dgv_gestion_utilisateurs.Columns.Add(cbColonne);
                    cbColonne.FlatStyle = FlatStyle.Flat;
                    cbColonne.Name = "Rang";
                    cbColonne.HeaderText = "Rang";
                    cbColonne.DisplayMember = "Item";
                    cbColonne.ValueMember = "Value";
                    cbColonne.DisplayIndex = 3;

                    for (int k = 0; k < listeDonneesDgv.Count; k += 4)
                    {
                        dgv_gestion_utilisateurs.Rows.Add(listeDonneesDgv[k].ToUpper(), listeDonneesDgv[k + 1], listeDonneesDgv[k + 2]);
                        dgv_gestion_utilisateurs.Rows[k / 4].Cells[3].Value = (cbColonne.Items[Convert.ToInt32(listeDonneesDgv[k + 3])] as DataRowView).Row[1].ToString();
                        dgv_gestion_utilisateurs.Rows[k / 4].Cells[3].Style.Font = new Font(dgv_gestion_utilisateurs.Font, FontStyle.Bold);
                        Color[] couleursDroits = new Color[5] { Color.Red, Color.Green, Color.Orange, Color.BlueViolet, Color.Blue };
                        int index = Convert.ToInt32(dgv_gestion_utilisateurs[3, k / 4].Value.ToString());
                        dgv_gestion_utilisateurs.Rows[k / 4].Cells[3].Style.ForeColor = couleursDroits[index];
                    }

                    break;

                // Paramètres
                case 7:
                    // Paramètres relatifs à la BDD
                    tb_parametres_user.Text = ConnexionBDD.User;
                    mtb_parametres_password.Text = ConnexionBDD.Password;
                    tb_parametres_database.Text = ConnexionBDD.Database;
                    tb_parametres_host.Text = ConnexionBDD.Host;
                    tb_parametres_port.Text = ConnexionBDD.Port;

                    if (ConnexionBDD.Connexion != null)
                    {
                        tb_parametres_cheminDossierPdf.Text = PersistanceParametres.CheminDossierSauvegardePdf.Replace(@"\\",@"\");
                        tb_parametres_cheminFichierC212P.Text = PersistanceParametres.CheminFichierExtractionC212P.Replace(@"\\", @"\");
                        tb_parametres_cheminFichierSauvegardeParametres.Text = PersistanceParametres.CheminFichierTxtConnexionBdd.Replace(@"\\", @"\");

                        pb_parametres_imageDefaut.SizeMode = PictureBoxSizeMode.StretchImage;
                        if (PersistanceParametres.CheminImageErreur != "")
                        {
                            FileStream fileStream = new FileStream(PersistanceParametres.CheminImageErreur, FileMode.Open);
                            pb_parametres_imageDefaut.Image = Image.FromStream(fileStream);
                            fileStream.Close();
                        }
                        lbl_parametres_etatConnexionBdd.Text = "Connexion à la base de données réussie";
                        // pnl_parametres_modification.Visible = true;
                        grpBox_parametres_baseDeDonnees.Visible = true;
                        grpBox_Parametres_fichiers.Visible = true;
                        pnl_parametres_connexion.Visible = false;
                    }
                    else
                    {
                        lbl_parametres_etatConnexionBdd.Text = "Impossible de se connecter à la base de données";
                        // pnl_parametres_modification.Visible = false;
                        // MessageBox.Show("choisir fichier");

                        grpBox_parametres_baseDeDonnees.Visible = false;
                        grpBox_Parametres_fichiers.Visible = false;
                        pnl_parametres_connexion.Visible = true;

                    }
                    



                    break;
            }
        }



        private void btn_menuPrets_Click(object sender, EventArgs e)
        {
            // Hide et Show nécessaires pour l'affichage correct de la fenêtre
            _fenetreActuelle = "menuPrets";
            btn_afficherPrets.Visible = false;
            pnl_arrierePlanMenuPrincipal.Visible = true;
            pnl_ficheTechniqueEquerre.Visible = false;
            pnl_infosBloc.Visible = false;
            pnl_reservation.Visible = false;
            lbl_apercuResasDuBloc.Visible = false;
            lbl_indicationNumeroPage.Visible = false;
            btn_pagePrecedente.Visible = false;
            btn_pageSuivante.Visible = false;
            btn_reservation.Visible = false;
            dgv_apercuResasDuBloc.Visible = false;
            lbl_reservationIndisponible.Visible = false;
            btn_laisserLaMain.Visible = false;
            
            btn_gestion_modifier.Visible = true;
            btn_gestion_nouveau.Visible = true;
            tab_gestion.Visible = true;

            chart_blocsAyantEuTypeEquerre.Visible = false;
            chart_nombrePrets.Visible = false;
            chart_nombrePretsBloc.Visible = false;
            chart_nombreReservationsParMois.Visible = false;
            chart_nombreTravaux.Visible = false;
            chart_tauxOccupation.Visible = false;
            lbl_stats_erreurBlocsAyantEuTypeEquerre.Visible = false;
            lbl_stats_erreurNombrePret.Visible = false;
            lbl_stats_erreurNombrePretsBloc.Visible = false;
            lbl_stats_erreurNombreReservationsParMois.Visible = false;
            lbl_stats_erreurNombreTravaux.Visible = false;
            lbl_stats_erreurTauxOccupation.Visible = false;
            tab_gestion.Visible = false;
            btn_gestion_modifier.Visible = false;
            btn_gestion_nouveau.Visible = false;
        }


        private void btn_afficherPrets_Click(object sender, EventArgs e)
        {
            // On affiche le dgv en fonction du choix de l'utilisateur : équerres réservées par défaut, sinon équerres prêtées
            if(_etatBtnAfficherPret == false)
            {
                // Si c'est la première fois que le bouton est cliqué, on passe en mode prêt
                btn_afficherPrets.Text = "Équerres";
            }
            else
            {
                // Sinon, on passe en mode équerre
                btn_afficherPrets.Text = "Prêts";
            }
            _dgvPageActuelle = 0;
            _etatBtnAfficherPret = !_etatBtnAfficherPret;
            rafraichirDgvEquerres();
        }

        private void btn_menuStatistiques_Click(object sender, EventArgs e)
        {

            // Hide et Show nécessaires pour l'affichage correct de la fenêtre
            btn_afficherPrets.Visible = false;
            cb_stats_typeEquerre.Visible = true;
            cb_stats_typeEquerre.DataSource = _BDDQuery.getAllReperesTypesEquerres();
            if (cb_stats_typeEquerre.Items.Count >= 0)
            {
                cb_stats_typeEquerre.SelectedIndex = 0;
            }
            

            tab_gestion.Visible = false;
            pnl_arrierePlanMenuPrincipal.Visible = true;
            pnl_ficheTechniqueEquerre.Visible = false;
            pnl_infosBloc.Visible = false;
            pnl_reservation.Visible = false;
            lbl_apercuResasDuBloc.Visible = false;
            dgv_apercuResasDuBloc.Visible = false;
            btn_reservation.Visible = false;
            btn_gestion_modifier.Visible = false;
            btn_gestion_nouveau.Visible = false;
            lbl_indicationNumeroPage.Visible = false;
            lbl_reservationIndisponible.Visible = false;
            btn_pagePrecedente.Visible = false;
            btn_pageSuivante.Visible = false;
            btn_laisserLaMain.Visible = false;

            chart_blocsAyantEuTypeEquerre.Visible = true;
            chart_nombrePrets.Visible = true;
            chart_nombrePretsBloc.Visible = true;
            chart_nombreReservationsParMois.Visible = true;
            chart_nombreTravaux.Visible = true;
            chart_tauxOccupation.Visible = true;



        }



        private void btn_gestion_modifier_Click(object sender, EventArgs e)
        {
            // Lorsque le bouton est cliqué, on propose à l'utilisateur de modifier les données affichées sur mla page actuelle
            _modificationGestion = true;
            switch (tab_gestion.SelectedIndex)
            {
                // Équerres
                case 0:
                    break;

                // Types Équerres
                case 1:
                    _typeEquerreGestion = _BDDQuery.getTypeEquerre(cb_gestion_typesEquerres.SelectedItem.ToString());
                    tb_gestion_numeroPlan.Text = _typeEquerreGestion.NumeroPlan;
                    tb_gestion_repere.Text = _typeEquerreGestion.Repere;
                    cb_gestion_reglageHauteur.SelectedIndex = _typeEquerreGestion.ReglageHauteur - 1;
                    rbt_gestion_semblable.Checked = _typeEquerreGestion.Semblable;
                    pb_gestion_miniatureDocumentation.SizeMode = PictureBoxSizeMode.StretchImage;
                    string cheminImage;
                    if(File.Exists(_typeEquerreGestion.CheminImage))
                    {
                        cheminImage = _typeEquerreGestion.CheminImage;
                    }
                    else
                    {
                        cheminImage = PersistanceParametres.CheminImageErreur;
                    }
                    FileStream fileStream = new FileStream(cheminImage, FileMode.Open);
                    pb_gestion_miniatureDocumentation.Image = Image.FromStream(fileStream);
                    fileStream.Close();

                    pb_gestion_miniatureDocumentation.Visible = true;
                    btn_gestion_parcourirImage.Visible = true;
                    tb_gestion_repere.Visible = true;
                    tb_gestion_numeroPlan.Visible = true;
                    rbt_gestion_semblable.Visible = true;
                    rbt_gestion_symetrique.Visible = true;
                    cb_gestion_reglageHauteur.Visible = true;
                    btn_gestion_validerModifications.Visible = true;
                    grpBox_gestion_SemSym.Visible = true;
                    lbl_gestion_repere.Visible = true;
                    lbl_gestion_numeroPlan.Visible = true;
                    lbl_gestion_reglageHauteur.Visible = true;
                    break;



            }
        }
        private void btn_gestion_nouveau_Click(object sender, EventArgs e)
        {
            // Lorsque le bouton est cliqué, on propose à l'utilisateur de créer une nouvelle occurence en base de données
            _modificationGestion = false;
            switch (tab_gestion.SelectedIndex)
            {
                // Équerres
                case 0:
                    break;

                // Types Équerres
                case 1:
                    pb_gestion_miniatureDocumentation.Visible = true;
                    btn_gestion_parcourirImage.Visible = true;
                    tb_gestion_repere.Visible = true;
                    tb_gestion_numeroPlan.Visible = true;
                    rbt_gestion_semblable.Visible = true;
                    rbt_gestion_symetrique.Visible = true;
                    cb_gestion_reglageHauteur.Visible = true;
                    btn_gestion_validerModifications.Visible = true;
                    grpBox_gestion_SemSym.Visible = true;
                    lbl_gestion_repere.Visible = true;
                    lbl_gestion_numeroPlan.Visible = true;
                    lbl_gestion_reglageHauteur.Visible = true;
                    tb_gestion_lienImage.Text = "";
                    tb_gestion_numeroPlan.Text = "";
                    tb_gestion_repere.Text = "";
                    cb_gestion_reglageHauteur.SelectedIndex = 0;
                    break;

            }
        }

        private void btn_gestion_parcourirImage_Click(object sender, EventArgs e)
        {
            // Permet à l'utilisateur de choisir une image pour un type d'équerre
            OpenFileDialog ofd = new OpenFileDialog();
            // https://msdn.microsoft.com/fr-fr/library/system.windows.controls.openfiledialog.filter(v=vs.95).aspx
            ofd.Filter = "Images (*.jpg, *.png, *.jpeg)| *.jpg; *.png; *jpeg";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                tb_gestion_lienImage.Text = ofd.FileName.Replace(@"\",@"\\");
                pb_gestion_miniatureDocumentation.SizeMode = PictureBoxSizeMode.StretchImage;
                FileStream fileStream = new FileStream(tb_gestion_lienImage.Text, FileMode.Open);
                pb_gestion_miniatureDocumentation.Image = Image.FromStream(fileStream);
                fileStream.Close();
            }
        }

        
        private void btn_gestion_validerModifications_Click(object sender, EventArgs e)
        {
            // Si le bouton valider est cliqué, il faut effectuer un switch et, en fonction de l'onglet du tab, modifier en base de données
            _BDDQuery.updateTypeEquerre(_typeEquerreGestion);
        }


        public void parametrerGraphique(Chart chart, string titre)
        {
            // Paramètre le graphique : cache le cadrillage, affiche la valeur en légende...
            // https://stackoverflow.com/questions/11019086/net-chart-clear-and-re-add
            foreach (var series in chart.Series)
            {
                series.Points.Clear();
            }
            chart.Titles.Clear();
            chart.Titles.Add(titre);
            chart.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
            chart.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
            chart.Series[0].IsValueShownAsLabel = true;
            chart.Series[0].IsVisibleInLegend = false;
        }

        private void cb_stats_typeEquerre_SelectedIndexChanged(object sender, EventArgs e)
        {

            // Lorsque le type d'équerre change, on met à jour tous les graphes
            string typeEquerre = cb_stats_typeEquerre.SelectedValue.ToString();
            ArrayList blocsAyantEuTypeEquerre = _BDDQuery.getStatsBlocsAyantEuTypeEquerre(typeEquerre);      
            ArrayList nombrePrets = _BDDQuery.getStatsNombrePrets(typeEquerre);                              
            ArrayList nombrePretsBloc = _BDDQuery.getStatsNombrePretsBloc(typeEquerre);                      
            ArrayList nombreTravaux = _BDDQuery.getStatsNombreTravaux(typeEquerre);                        
            ArrayList nombreReservationsParMois = _BDDQuery.getStatsNombreReservationsParMois(typeEquerre);     
            ArrayList tauxOccupation = _BDDQuery.getStatsTauxOccupation(typeEquerre);

            // Nombre de travaux
            parametrerGraphique(chart_nombreTravaux, "Nombre de travaux débutés");
            for (int k = 0; k < nombreTravaux.Count; k += 2)
            {
                chart_nombreTravaux.Series[0].Points.AddXY((int)nombreTravaux[k], (int)nombreTravaux[k + 1]);
            }
            chart_nombreTravaux.ChartAreas[0].RecalculateAxesScale();
            lbl_stats_erreurNombreTravaux.Visible = nombreTravaux.Count == 0 ? true : false;


            // Nombre de prêts
            parametrerGraphique(chart_nombrePrets, "Nombre de prêts");
            for (int k = 0; k < nombrePrets.Count; k += 2)
            {
                chart_nombrePrets.Series[0].Points.AddXY((int)nombrePrets[k], (int)nombrePrets[k + 1]);
            }
            chart_nombrePrets.ChartAreas[0].RecalculateAxesScale();
            lbl_stats_erreurNombrePret.Visible = nombrePrets.Count == 0 ? true : false;

            // Nombre de prêts bloc
            parametrerGraphique(chart_nombrePretsBloc, "Nombre de prêts associés à un bloc");
            for (int k = 0; k < nombrePretsBloc.Count; k += 2)
            {
                chart_nombrePretsBloc.Series[0].Points.AddXY((int)nombrePretsBloc[k], (int)nombrePretsBloc[k + 1]);
            }
            chart_nombrePretsBloc.ChartAreas[0].RecalculateAxesScale();
            lbl_stats_erreurNombrePretsBloc.Visible = nombrePretsBloc.Count == 0 ? true : false;


            // Blocs ayant eu type équerre
            parametrerGraphique(chart_blocsAyantEuTypeEquerre, "Présence moyenne des équerres de type " + typeEquerre + " parmi les blocs de chaque navire");
            for (int k = 0; k < blocsAyantEuTypeEquerre.Count; k += 2)
            {
                chart_blocsAyantEuTypeEquerre.Series[0].Points.AddXY(blocsAyantEuTypeEquerre[k], blocsAyantEuTypeEquerre[k + 1] );
            }
            chart_blocsAyantEuTypeEquerre.ChartAreas[0].RecalculateAxesScale();
            lbl_stats_erreurBlocsAyantEuTypeEquerre.Visible = blocsAyantEuTypeEquerre.Count == 0 ? true : false;
            // chart_blocsAyantEuTypeEquerre.Series[0].ChartType = SeriesChartType.StackedColumn;

            // Taux d'occupation
            parametrerGraphique(chart_tauxOccupation, "Taux d'occupation moyen (%)");
            for (int k = 0; k < tauxOccupation.Count; k += 2)
            {
                chart_tauxOccupation.Series[0].Points.AddXY(tauxOccupation[k], tauxOccupation[k + 1]);
            }
            chart_tauxOccupation.ChartAreas[0].RecalculateAxesScale();
            lbl_stats_erreurTauxOccupation.Visible = tauxOccupation.Count == 0 ? true : false;



            // Nombre de réservations par mois
            chart_nombreReservationsParMois.Series.Clear();
            chart_nombreReservationsParMois.Titles.Clear();
            chart_nombreReservationsParMois.Titles.Add("Nombre de réservations");
            chart_nombreReservationsParMois.ChartAreas[0].AxisX.Interval = 1;
            chart_nombreReservationsParMois.ChartAreas[0].AxisY.Interval = 1;
            chart_nombreReservationsParMois.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
            chart_nombreReservationsParMois.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;

            string[] mois = new string[] {"Jan","Fév","Mar","Avr","Mai","Jui","Jui","Aoû","Sep","Oct","Nov","Dec"};

            // Tri de la liste des années
            List<int> listeAnnees = new List<int>();
            for (int k = 0; k < nombreReservationsParMois.Count; k += 13)
            {
                listeAnnees.Add((int)nombreReservationsParMois[k]);
            }
            listeAnnees.Sort();
            
            int annee = 0;
            Series series = new Series();
            for (int k=0; k<5; k++)
            {
                annee = listeAnnees[k];
                series = new Series(annee.ToString());
                series.IsValueShownAsLabel = true;

                for (int m = 1 + (13 * k); m < 13 + (13 * k); m++)
                {
                    series.Points.AddXY(mois[m - (k * 13) - 1], nombreReservationsParMois[m]);
                }
                chart_nombreReservationsParMois.Series.Add(series);
            }
            chart_nombreReservationsParMois.ChartAreas[0].RecalculateAxesScale();
            
            // Correspond à l'ArrayList retournée si aucune réservation n'a été faite sur les 5 ans (2015,0,0,0,0,0,0,0,0,0,0,0,0,2016,0,0...)
            ArrayList al = new ArrayList();
            int anneeDebutStat = DateTime.Now.Year - 3;
            for(int k=0; k<5; k++)
            {
                al.Add(anneeDebutStat + k);
                for(int m=0; m<12; m++)
                {
                    al.Add(0);
                }
            }
            
            bool estEgale = true;
            for(int k=0; k<al.Count; k++)
            {
                if((int)al[k] != (int)nombreReservationsParMois[k])
                {
                    estEgale = false;
                    break;
                }
            }

            lbl_stats_erreurNombreReservationsParMois.Visible = estEgale ? true : false;


        }


        // Propose à l'utilisateur de choisir un dossier / fichier pour stocker le chemin dans une des variables de PersistanceParametres
        private void btn_parametres_parcourirDossier_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if (folderBrowserDialog.SelectedPath != "")
                {
                    tb_parametres_cheminDossierPdf.Text = folderBrowserDialog.SelectedPath; //.Replace(@"\", @"\\");
                }
                else
                {
                    MessageBox.Show("Votre sélection n'est pas valide");
                }
            }
        }
        private void btn_parametres_parcourirFichierC212P_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier texte (*.txt)| *.txt";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog.FileName != "")
                {
                    tb_parametres_cheminFichierC212P.Text = openFileDialog.FileName; //.Replace(@"\", @"\\");
                }
                else
                {
                    MessageBox.Show("Votre sélection n'est pas valide");
                }
            }
        }
        private void btn_parametres_parcourirFichierSauvegarde_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier texte (*.txt)| *.txt";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog.FileName != "")
                {
                    tb_parametres_cheminFichierSauvegardeParametres.Text = openFileDialog.FileName; //.Replace(@"\", @"\\");
                }
                else
                {
                    MessageBox.Show("Votre sélection n'est pas valide");
                }
            }
        }
        private void btn_parametres_parcourirImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Images (*.jpg, *.png, *.jpeg)| *.jpg; *.png; *jpeg";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog.FileName != "")
                {
                    tb_parametres_cheminFichierSauvegardeParametres.Text = openFileDialog.FileName; //.Replace(@"\", @"\\");
                    tb_parametres_cheminImage.Text = openFileDialog.FileName; //.Replace(@"\", @"\\");
                    pb_parametres_imageDefaut.SizeMode = PictureBoxSizeMode.StretchImage;
                    FileStream fileStream = new FileStream(tb_parametres_cheminImage.Text, FileMode.Open);
                    pb_parametres_imageDefaut.Image = Image.FromStream(fileStream);
                    fileStream.Close();
                }
                else
                {
                    MessageBox.Show("Votre sélection n'est pas valide");
                }
            }
        }
        private void btn_parametres_parcourir_Click(object sender, EventArgs e)
        {
            // Propose à l'utilisateur de choisir un fichier texte
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier texte (*.txt)| *.txt";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog.FileName != "")
                {
                    // Si l'utilisateur a choisi un fichier pour se connecter à la base de données
                    // todo : Il faut tester la connexion avec les valeurs stockées dans le fichier texte.
                    // Voir comment sauvegarder l'emplacement du dossier

                    // tb_parametres_cheminFichierSauvegardeParametres.Text = openFileDialog.FileName;
                    string[] fichierTxtExplode = File.ReadAllText(openFileDialog.FileName).Split(new string[] { ";;;;" }, StringSplitOptions.None);
                    /*tb_parametres_user.Text = fichierTxtExplode[5];
                    mtb_parametres_password.Text = fichierTxtExplode[6];
                    tb_parametres_database.Text = fichierTxtExplode[7];
                    tb_parametres_host.Text = fichierTxtExplode[8];
                    tb_parametres_port.Text = fichierTxtExplode[9];*/
                    // connexion = ConnexionBDD.ReloadConnexion();
                    /*ConnexionBDD.User = fichierTxtExplode[5];
                    ConnexionBDD.Password = fichierTxtExplode[6];
                    ConnexionBDD.Database = fichierTxtExplode[7];
                    ConnexionBDD.Host = fichierTxtExplode[8];
                    ConnexionBDD.Port = fichierTxtExplode[9];
                    ConnexionBDD.ReloadConnexion();
                    if(ConnexionBDD.Connexion != null)
                    {
                        _BDDQuery = new BDDQuery();
                        _BDDQuery.getPersistanceParametres();
                        PersistanceParametres.CheminFichierTxtConnexionBdd = openFileDialog.FileName.Replace(@"\", @"\\");
                        _BDDQuery.updatePersistanceParametres();
                        Application.Restart();
                    }
                    else
                    {
                        MessageBox.Show("Impossible de se connecter");
                    }*/
                    // grpBox_parametres_baseDeDonnees.Visible = true;

                    // btn_parametres_appliquerChangementsBdd.PerformClick();
                    // MessageBox.Show("ok");
                }
                else
                {
                    MessageBox.Show("Votre sélection n'est pas valide");
                }
            }
        }

        private void btn_parametres_appliquerChangementsFichiers_Click(object sender, EventArgs e)
        {
            // Met à jour en base de données les nouvelles valeurs de PersistanceParametres
            PersistanceParametres.CheminDossierSauvegardePdf = tb_parametres_cheminDossierPdf.Text.Replace(@"\", @"\\");
            PersistanceParametres.CheminFichierExtractionC212P = tb_parametres_cheminFichierC212P.Text.Replace(@"\", @"\\");
            PersistanceParametres.CheminFichierTxtConnexionBdd = tb_parametres_cheminFichierSauvegardeParametres.Text.Replace(@"\", @"\\");
            PersistanceParametres.CheminImageErreur = tb_parametres_cheminImage.Text.Replace(@"\", @"\\");
            _BDDQuery.updatePersistanceParametres();
        }
        private void btn_parametres_appliquerChangementsBdd_Click(object sender, EventArgs e)
        {
            ConnexionBDD.Database = tb_parametres_database.Text;
            ConnexionBDD.Host = tb_parametres_host.Text;
            ConnexionBDD.User = tb_parametres_user.Text;
            ConnexionBDD.Password = mtb_parametres_password.Text;
            ConnexionBDD.Port = tb_parametres_port.Text;

            MySqlConnection connexion = ConnexionBDD.ReloadConnexion();
            if (connexion != null)
            {
                lbl_parametres_etatConnexionBdd.Text = "Connexion à la base de données réussie";

                // Crée ou passe l'utilisateur qui fait la modification des paramètres en admin
                _BDDQuery = new BDDQuery();
                Utilisateur utilisateur = _BDDQuery.getUtilisateur(Environment.UserName);
                utilisateur.Rang = 4;
                _BDDQuery.updateUtilisateur(utilisateur);

                // On sauvegarde les bons paramètres dans le fichier texte
                File.WriteAllText(@"C:\\Users\\"+Environment.UserName+"\\Desktop\\fichierConnexionBDD.txt", "user;;;;password;;;;database;;;;host;;;;port;;;;" + ConnexionBDD.User + ";;;;" + ConnexionBDD.Password + ";;;;" + ConnexionBDD.Database + ";;;;" + ConnexionBDD.Host + ";;;;" + ConnexionBDD.Port);
                MessageBox.Show("L'application va redémarrer pour mettre à jour les changements");
                Application.Restart();
            }
            else
            {
                lbl_parametres_etatConnexionBdd.Text = "Impossible de se connecter à la base de données";
            }
        }

        private void mtb_parametres_mdpAdministrateur_TextChanged(object sender, EventArgs e)
        {
            // todo : Pourquoi ne pas mettre en place un mot de passe qui change ?
            // En fonction de la date, de l'heure etc
            string motDePasse = "root";
            if(mtb_parametres_mdpAdministrateur.Text == motDePasse)
            {
                // Si le mot de passe saisi est valide, affiche la page de modification des paramètres
                grpBox_parametres_baseDeDonnees.Visible = true;
                grpBox_Parametres_fichiers.Visible = true;
                pnl_parametres_connexion.Visible = false;
                mtb_parametres_mdpAdministrateur.Text = "";
            }
        }


        /*private void afficherPageParametres()
        {
            btn_menuGestion.PerformClick();
            tab_gestion.SelectedIndex = 7;
        }*/
    }
}
