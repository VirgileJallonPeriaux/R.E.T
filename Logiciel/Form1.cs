using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RET_testModifDate
{
    public partial class Form1 : Form
    {

        private bool _chargementencours ;
        private Bloc _blocActuel;
        private bool _btnDebut = false;
        private bool _btnFin = false;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _chargementencours = true;
            Navire navire = new Navire(1, "K34");
            _blocActuel = new Bloc(4444, navire, "0112", new DateTime(2017, 01, 01), new DateTime(2017, 01, 01), new DateTime(2018, 01, 01), true, true, true, "", true, true);

            cb_dateDebut.Items.Add("");
            cb_dateFin.Items.Add("");
            cb_dateDebut.SelectedIndex = 0;
            cb_dateFin.SelectedIndex = 0;

            rbt_pm.PerformClick();



            _chargementencours = false;
        }

        private void rbt_pm_CheckedChanged(object sender, EventArgs e)
        {
            cb_modifDateDebutAnnee.Hide();
            cb_modifDateDebutSemaine.Hide();
            cb_modifDateFinAnnee.Hide();
            cb_modifDateFinSemaine.Hide();
            cb_dateDebut.Show();
            cb_dateFin.Show();

            //reinitialise etat btns
            _btnDebut = false;
            btn_modifDateDebut.Text = "M";
            _btnFin = false;
            btn_modifDateFin.Text = "M";

            if (rbt_pm.Checked)
            {
                ckb_dateDebut.Show();
                ckb_dateDebut.Checked = !_blocActuel.DateDebutPmModifiable;
                ckb_dateFin.Checked = !_blocActuel.DateFinPmModifiable;
                btn_modifDateDebut.Show();

                DateTime dtDebutModifiee = getDateModifiee(_blocActuel.DateDebutPm, -1);
                cb_dateDebut.Items[0] = "S" + getSemaine(dtDebutModifiee).ToString() + " / " + dtDebutModifiee.Year.ToString();

                DateTime dtFinModifiee = getDateModifiee(_blocActuel.DateFinPm, 1);
                cb_dateFin.Items[0] = "S" + getSemaine(dtFinModifiee).ToString() + " / " + dtFinModifiee.Year.ToString();
                
            }
            else
            {
                ckb_dateDebut.Hide();
                ckb_dateFin.Checked = !_blocActuel.DateFinBordModifiable;
                btn_modifDateDebut.Hide();

                DateTime dtDebutModifiee = getDateModifiee(_blocActuel.DateFinPm, -2);
                cb_dateDebut.Items[0] = "S" + getSemaine(dtDebutModifiee).ToString() + " / " + dtDebutModifiee.Year.ToString();

                if (_blocActuel.DateFinBord.Year != 1)
                {
                    DateTime dtFinModifiee = getDateModifiee(_blocActuel.DateFinBord, 1);
                    cb_dateFin.Items[0] = "S" + getSemaine(dtFinModifiee).ToString() + " / " + dtFinModifiee.Year.ToString();
                }
                else
                {
                    cb_dateFin.Items[0] = "A RENSEIGNER";
                }

            }


        }

        // ok
        private void ckb_dateDebut_CheckedChanged(object sender, EventArgs e)
        {
            // MessageBox.Show(ckb_dateDebut.Checked.ToString());
            if(rbt_pm.Checked)
            {
                _blocActuel.DateDebutPmModifiable = ckb_dateDebut.Checked;
            }
        }
        private void ckb_dateFin_CheckedChanged(object sender, EventArgs e)
        {
            if (rbt_pm.Checked)
            {
                _blocActuel.DateFinPmModifiable = ckb_dateFin.Checked;
            }
            else
            {
                _blocActuel.DateFinBordModifiable = ckb_dateFin.Checked;
            }
        }



        private void btn_modifDateDebut_Click(object sender, EventArgs e)
        {
            if(_btnDebut == false)
            {
                btn_modifDateDebut.Text = "ok";
                // proposer modif
                cb_dateDebut.Hide();
                cb_modifDateDebutAnnee.Show();
                cb_modifDateDebutSemaine.Show();


                cb_modifDateDebutAnnee.Items.Clear();
                int anneedebut = (_blocActuel.DateDebutPm.Year == 1) ? DateTime.Now.Year : _blocActuel.DateDebutPm.Year;
                for (int k = 0; k < 5; k++)
                {
                    cb_modifDateDebutAnnee.Items.Add(anneedebut + k);
                }
            }
            else
            {
                // sauvegarde
                DateTime premierjour = getPremierJourSemaine((int)cb_modifDateDebutAnnee.SelectedItem, (int)cb_modifDateDebutSemaine.SelectedItem);
                _blocActuel.DateDebutPm = getDateModifiee(premierjour, 1);      // car -1 deja pris en compte
                MessageBox.Show("Debut PM : " + _blocActuel.DateDebutPm.ToString());
                btn_modifDateDebut.Text = "M";
            }


            _btnDebut = !_btnDebut;


        }

        private void btn_modifDateFin_Click(object sender, EventArgs e)
        {
            if (_btnFin == false)
            {
                btn_modifDateFin.Text = "ok";
                cb_dateFin.Hide();
                cb_modifDateFinAnnee.Show();
                cb_modifDateFinSemaine.Show();

                if (rbt_pm.Checked)
                {
                    cb_modifDateFinAnnee.Items.Clear();
                    int anneedebut = (_blocActuel.DateFinPm.Year == 1) ? DateTime.Now.Year : _blocActuel.DateFinPm.Year;
                    for (int k = 0; k < 5; k++)
                    {
                        cb_modifDateFinAnnee.Items.Add(anneedebut + k);
                    }
                }
                else
                {
                    cb_modifDateFinAnnee.Items.Clear();
                    int anneedebut = (_blocActuel.DateFinBord.Year == 1) ? DateTime.Now.Year : _blocActuel.DateFinBord.Year;
                    for (int k = 0; k < 5; k++)
                    {
                        cb_modifDateFinAnnee.Items.Add(anneedebut + k);
                    }
                }
            }
            else
            {
                btn_modifDateFin.Text = "M";
            }

            _btnFin = !_btnFin;
        }
















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

        private void cb_modifDateDebutAnnee_SelectedIndexChanged(object sender, EventArgs e)
        {
            cb_modifDateDebutSemaine.Items.Clear();
            int nbSemaines = getSemaine(new DateTime(Convert.ToInt32(cb_modifDateDebutAnnee.SelectedItem), 12, 31));
            for (int k = 0; k < 52; k++)
            {
                cb_modifDateDebutSemaine.Items.Add((k + 1).ToString());
            }
            if (nbSemaines == 53) { cb_modifDateDebutSemaine.Items.Add(53); }
        }

        private void cb_modifDateFinAnnee_SelectedIndexChanged(object sender, EventArgs e)
        {
            cb_modifDateFinSemaine.Items.Clear();
            int nbSemaines = getSemaine(new DateTime(Convert.ToInt32(cb_modifDateFinAnnee.SelectedItem), 12, 31));
            for (int k = 0; k < 52; k++)
            {
                cb_modifDateFinSemaine.Items.Add((k + 1).ToString());
            }
            if (nbSemaines == 53) { cb_modifDateFinSemaine.Items.Add(53); }
        }

    }
}
