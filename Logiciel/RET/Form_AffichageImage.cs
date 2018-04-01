using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RET
{
    public partial class Form_AffichageImage : Form
    {
        private Form_MenuPrincipal _formMenuPrincipal;
        private Equerre _equerre;

        public Form_AffichageImage(Image image, Equerre equerre, Form_MenuPrincipal formMenuPrincipal)
        {
            InitializeComponent();

            _formMenuPrincipal = formMenuPrincipal;
            _equerre = equerre;

            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Documentation relative aux équerre de type " + equerre.TypeEquerre.Repere;
            pb_Schema.Image = image;
            this.Size = new Size(image.Width+15, image.Height+35);
            pb_Schema.Size = new Size(image.Width, image.Height);
            pb_Schema.SizeMode = PictureBoxSizeMode.StretchImage;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void Form_AffichageImage_FormClosed(object sender, FormClosedEventArgs e)
        {
            _formMenuPrincipal.ListeFenetresOuvertes.Remove(_equerre.TypeEquerre.Id);
            _formMenuPrincipal.RafraichirEtatBoutonZoom(_equerre.TypeEquerre.Id);
        }
    }
}
