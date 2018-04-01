using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RET
{
    public class GenerateurPdf
    {
        private string _cheminDossier;
        private int _dernierePageAvecFooter = 0;
        private iTextSharp.text.Font _policeGras = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 25);
        private iTextSharp.text.Font _policeGrasMoyenne = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 17);
        private iTextSharp.text.Font _policeReduite = FontFactory.GetFont(FontFactory.COURIER_BOLDOBLIQUE, 8);


        public GenerateurPdf(string cheminDossier)
        {
            _cheminDossier = cheminDossier;
        }

        public bool genererPdf(string navire, bool pm, string repere, int semaineDD, int anneeDD, int semaineDF, int anneeDF, ArrayList reservations, ArrayList prets)
        {
            // todo : Le footer n'apparaît plus
            string chantier = pm ? "PM" : "BORD";
            if (!Directory.Exists(_cheminDossier + "\\" + navire + "\\" + chantier))
            {
                Directory.CreateDirectory(_cheminDossier + "\\" + navire + "\\" + chantier);
            }

            string cheminFichier = _cheminDossier + "\\" + navire + "\\" + chantier + "\\" + repere + ".pdf";
            if (estEnCoursUtilisation(cheminFichier) == false || !File.Exists(cheminFichier))
            {
                _dernierePageAvecFooter = 0;
                FileStream fs = new FileStream(_cheminDossier + "\\" + navire + "\\" + chantier + "\\" + repere + ".pdf", FileMode.Create);
                Document doc = new Document(PageSize.A4);
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                string[] jours = new string[] { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
                string[] mois = new string[] { "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre" };
                // ######################################\\ DEBUT GENERATION PDF

                doc.Open();
                AfficherRectangle(writer, 0, 700, 595, 3, 255, 93, 0); //  barre séparation en-tête / tableau 1
                AfficherRectangle(writer, 215, 751, 300, 75, 120, 120, 120); // rectangle dd/df
                AfficherRectangle(writer, 230, 726, 150, 25, 200, 200, 200); // rectangle date maj

                DateTime aujourdHui = DateTime.Now;
                string heure = aujourdHui.Hour.ToString().Length < 2 ? "0" + aujourdHui.Hour.ToString() : aujourdHui.Hour.ToString();
                string minutes = aujourdHui.Minute.ToString().Length < 2 ? "0" + aujourdHui.Minute.ToString() : aujourdHui.Minute.ToString();
                AfficherTexte(writer, jours[(int)aujourdHui.DayOfWeek - 1] + " " + aujourdHui.Day + " " + mois[(int)aujourdHui.Month - 1] + " " + aujourdHui.Year, 240, 624, _policeReduite, 0, 0, 0);
                AfficherTexte(writer, heure + " h " + minutes, 240, 616, _policeReduite, 0, 0, 0);

                AfficherRectangleArrondi(writer, 60, 716, 170, 109, 15, 3, 255, 255, 255);

                AfficherTexte(writer, navire, 80, 645, _policeGras, 0, 0, 0);
                AfficherTexte(writer, repere, 170, 675, _policeGrasMoyenne, 0, 0, 0);
                int axeX = pm ? 177 : 166;
                AfficherTexte(writer, chantier, axeX, 620, _policeGrasMoyenne, 0, 0, 0);

                PdfContentByte cb = writer.DirectContent;
                cb.SetLineWidth(3);
                cb.Arc(150, 576, 150, 858, 0, 50);
                cb.Stroke();
                cb.Arc(92, 769, 230, 769, 0, 100);
                cb.Stroke();
                cb.SetLineWidth(1);
                cb.Arc(485, 676, 485, 827, 0, 80);
                cb.Stroke();

                AfficherTexte(writer, "Début", 240, 674, _policeGrasMoyenne, 255, 255, 255);
                AfficherTexte(writer, "Fin", 240, 649, _policeGrasMoyenne, 255, 255, 255);


                cb.SetLineWidth(1);
                AfficherRectangle(writer, 300, 785, 95, 20, 240, 240, 240);
                AfficherRectangle(writer, 300, 760, 95, 20, 240, 240, 240);

                string semaine = semaineDD.ToString().Length < 2 ? "0" + semaineDD.ToString() : semaineDD.ToString();
                AfficherTexte(writer, "S" + semaine + " / " + anneeDD, 307, 674, _policeGrasMoyenne, 0, 0, 0);
                semaine = semaineDF.ToString().Length < 2 ? "0" + semaineDF.ToString() : semaineDD.ToString();
                AfficherTexte(writer, "S" + semaine + " / " + anneeDF, 307, 649, _policeGrasMoyenne, 0, 0, 0);
                iTextSharp.text.Paragraph space = new iTextSharp.text.Paragraph("\n\n\n\n\n\n\n\n");

                doc.Add(space);
                iTextSharp.text.Paragraph petitSpace = new iTextSharp.text.Paragraph("\n");
                AfficherTableauRéservations(doc, writer, reservations);
                doc.Add(petitSpace);
                AfficherTableauPrets(doc, writer, prets);

                // Contient toutes les classes des équerres réservées ou prêtées
                List<string> listeClasses = new List<string>();
                if (reservations.Count > 0)
                {
                    for (int k = 3; k < reservations.Count; k += 5)
                    {
                        if (!listeClasses.Contains(reservations[k])) { listeClasses.Add((string)reservations[k]); }
                    }
                }
                if (prets.Count > 0)
                {
                    for (int k = 3; k < prets.Count; k += 6)
                    {
                        if (!listeClasses.Contains(prets[k])) { listeClasses.Add((string)prets[k]); }
                    }
                }
                listeClasses.Sort();
                for (int k = 0; k < listeClasses.Count; k++)
                {
                    switch (listeClasses[k])
                    {
                        case "A":
                            AfficherRectangleArrondi(writer, 490, 802, 20, 20, 5, 1, 0, 200, 0);
                            AfficherTexte(writer, "A", 494, 691, _policeGrasMoyenne, 255, 255, 255);
                            break;
                        case "B":
                            AfficherRectangleArrondi(writer, 490, 779, 20, 20, 5, 1, 255, 93, 0);
                            AfficherTexte(writer, "B", 494, 668, _policeGrasMoyenne, 255, 255, 255);
                            break;
                        case "C":
                            AfficherRectangleArrondi(writer, 490, 756, 20, 20, 5, 1, 255, 0, 0);
                            AfficherTexte(writer, "C", 494, 645, _policeGrasMoyenne, 255, 255, 255);
                            break;
                    }
                }

                doc.Close();
                // ######################################\\

                return true;
            }
            else
            {
                return false;
            }
        }
        private bool estEnCoursUtilisation(string chemin)
        {
            bool isInUse = true;
            try
            {
                 // https://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use
                using (FileStream fileStreamOpen = new FileStream(chemin, FileMode.Open))
                {
                    isInUse = false;
                }
            }
            catch
            {
                
            }
            return isInUse;
        }
        private void AfficherTableauRéservations(Document doc, PdfWriter writer, ArrayList listeReservations)
        {
            string[] titresColonnes = new string[] { "Équerre", "Hauteur", "C.U.", "Classe", "Transport" };
            float[] largeurColonnes = new float[] { 0.7f, 0.8f, 0.5f, 0.3f, 0.8f };
            PdfPCell cell = new PdfPCell();
            PdfPTable table = new PdfPTable(1);
            iTextSharp.text.Font fontNormale = FontFactory.GetFont(FontFactory.TIMES, 12);
            iTextSharp.text.Font fontNormaleBold = FontFactory.GetFont(FontFactory.TIMES_BOLD, 12);

            table.HorizontalAlignment = Element.ALIGN_LEFT;
            table.WidthPercentage = 87;

            // En tête
            cell = new PdfPCell(new Phrase(String.Format("Réservations"), fontNormaleBold));
            cell.BackgroundColor = new iTextSharp.text.BaseColor(Color.FromArgb(150, 150, 150));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.FixedHeight = 20;
            table.AddCell(cell);
            doc.Add(table);

            // Titre des colonnes
            table = new PdfPTable(largeurColonnes);
            table.HorizontalAlignment = Element.ALIGN_LEFT;
            table.WidthPercentage = 87;
            for (int k = 0; k < 5; k++)
            {
                cell = new PdfPCell(new Phrase(String.Format(titresColonnes[k]), fontNormaleBold));
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.BackgroundColor = new iTextSharp.text.BaseColor(Color.FromArgb(220, 220, 220));
                cell.FixedHeight = 20;
                table.AddCell(cell);
            }
            doc.Add(table);

            // Remplissage
            for (int k = 0; k < listeReservations.Count; k += 5)
            {
                table = new PdfPTable(largeurColonnes);
                table.HorizontalAlignment = Element.ALIGN_LEFT;
                table.WidthPercentage = 87;

                // Repère, hauteur, charge et classe
                for (int m = 0; m < 4; m++)
                {
                    cell = new PdfPCell(new Phrase(String.Format((string)listeReservations[k + m]), fontNormaleBold));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.FixedHeight = 20;
                    table.AddCell(cell);
                }

                // Transport(s)
                iTextSharp.text.Paragraph p = new Paragraph();
                p.Font = fontNormale;
                string[] listeTransports = (string[])listeReservations[(((k / 5) + 1) * 5) - 1];
                string ponctuation = "";
                for (int m = 0; m < listeTransports.Length; m++)
                {
                    ponctuation = (m == listeTransports.Length - 1) ? "" : ", ";
                    p.Add(listeTransports[m] + ponctuation);
                }
                table.AddCell(p);
                doc.Add(table);
                AfficherFooter(doc, writer);
            }
        }
        private void AfficherTableauPrets(Document doc, PdfWriter writer, ArrayList listePrets)
        {
            string[] titresColonnes = new string[] { "Équerre", "Hauteur", "C.U.", "Classe", "Transport", "Date de fin" };
            float[] largeurColonnes = new float[] { 0.7f, 0.8f, 0.5f, 0.3f, 0.8f, 0.8f };
            PdfPCell cell = new PdfPCell();
            PdfPTable table = new PdfPTable(1);
            iTextSharp.text.Font fontNormale = FontFactory.GetFont(FontFactory.TIMES, 12);
            iTextSharp.text.Font fontNormaleBold = FontFactory.GetFont(FontFactory.TIMES_BOLD, 12);

            table.HorizontalAlignment = Element.ALIGN_LEFT;
            table.WidthPercentage = 100;

            // En tête
            cell = new PdfPCell(new Phrase(String.Format("Prêts"), fontNormaleBold));
            cell.BackgroundColor = new iTextSharp.text.BaseColor(Color.FromArgb(150, 150, 150));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.FixedHeight = 20;
            table.AddCell(cell);
            doc.Add(table);

            // Titre des colonnes
            table = new PdfPTable(largeurColonnes);
            table.HorizontalAlignment = Element.ALIGN_LEFT;
            table.WidthPercentage = 100;
            for (int k = 0; k < 6; k++)
            {
                cell = new PdfPCell(new Phrase(String.Format(titresColonnes[k]), fontNormaleBold));
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.BackgroundColor = new iTextSharp.text.BaseColor(Color.FromArgb(220, 220, 220));
                cell.FixedHeight = 20;
                table.AddCell(cell);
            }
            doc.Add(table);

            // Remplissage
            for (int k = 0; k < listePrets.Count; k += 6)
            {
                table = new PdfPTable(largeurColonnes);
                table.HorizontalAlignment = Element.ALIGN_LEFT;
                table.WidthPercentage = 100;

                // Repère, hauteur, charge et classe
                for (int m = 0; m < 4; m++)
                {
                    cell = new PdfPCell(new Phrase(String.Format((string)listePrets[k + m]), fontNormaleBold));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.FixedHeight = 20;
                    table.AddCell(cell);
                }

                // Transport(s)
                iTextSharp.text.Paragraph p = new Paragraph();
                p.Font = fontNormale;
                string[] listeTransports = (string[])listePrets[k + 4];
                string ponctuation = "";
                for (int m = 0; m < listeTransports.Length; m++)
                {
                    ponctuation = (m == listeTransports.Length - 1) ? "" : ", ";
                    p.Add(listeTransports[m] + ponctuation);
                }
                table.AddCell(p);

                // Date fin
                cell = new PdfPCell(new Phrase(String.Format((string)listePrets[k + 5]), fontNormaleBold));
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.FixedHeight = 20;
                table.AddCell(cell);

                doc.Add(table);
                AfficherFooter(doc, writer);
            }

        }
        private void AfficherFooter(Document doc, PdfWriter writer)
        {
            if (writer.CurrentPageNumber > _dernierePageAvecFooter)
            {
                _dernierePageAvecFooter = writer.CurrentPageNumber;
                iTextSharp.text.Font font = FontFactory.GetFont(FontFactory.TIMES, 7);
                AfficherTexteFooter(writer, "Tout exemplaire papier est un document de travail, seul le document de la base e.doc est mis à jour et fait foi.", 15, -10, font, 0, 0, 0);
                AfficherTexteFooter(writer, "This document and the information contained therein are the exclusive property of STX FRANCE, and shall not be used, communicated,", 15, -17, font, 0, 0, 0);
                AfficherTexteFooter(writer, "reproduced, copied or otherwise disposed of, directly or indirectly, for furnishing information to others except by prior written consent of STX FRANCE", 15, -24, font, 0, 0, 0);
                // todo : modifier lien image logo STX
                iTextSharp.text.Image jpg = iTextSharp.text.Image.GetInstance(@"C:\Users\lfm\Desktop\DossierImages\stx.png");
                jpg.Alignment = iTextSharp.text.Image.ALIGN_LEFT | iTextSharp.text.Image.TEXTWRAP;
                jpg.ScaleToFit(70, 120);
                jpg.SetAbsolutePosition(505, 7);
                doc.Add(jpg);
            }
        }
        private void AfficherTexteFooter(PdfWriter writer, string texte, int posX, int posY, iTextSharp.text.Font font, int r, int g, int b)
        {
            PdfContentByte cb = writer.DirectContent;
            cb.SetLineWidth(12);
            // cb.SetRGBColorFill(255,255,255);
            cb.SetRGBColorFill(r, g, b);
            cb.FillStroke();
            ColumnText ct = new ColumnText(cb);
            Phrase myText = new Phrase(texte, font);
            ct.SetSimpleColumn(myText, posX, posY, posX + 500, posY + 45, 15, Element.ALIGN_LEFT);
            ct.Go();
        }
        private void AfficherTexte(PdfWriter writer, string texte, int posX, int posY, iTextSharp.text.Font font, int r, int g, int b)
        {
            PdfContentByte cb = writer.DirectContent;
            cb.SetLineWidth(15);
            // cb.SetRGBColorFill(255,255,255);
            cb.SetRGBColorFill(r, g, b);
            cb.FillStroke();
            ColumnText ct = new ColumnText(cb);
            Phrase myText = new Phrase(texte, font);
            ct.SetSimpleColumn(myText, posX, posY, posX + 130, posY + 130, 15, Element.ALIGN_LEFT);
            ct.Go();
        }
        private void AfficherRectangle(PdfWriter writer, int posX, int posY, int largeur, int hauteur, int r, int g, int b)
        {
            PdfContentByte cb = writer.DirectContent;
            //cb.SetRGBColorStroke(r, g, b);
            // cb.SetLineWidth(0);
            cb.SetRGBColorFill(r, g, b);
            cb.Rectangle(posX, posY, largeur, hauteur);          // 50 / 350
            cb.FillStroke();
        }
        private void AfficherRectangleArrondi(PdfWriter writer, int posX, int posY, int largeur, int hauteur, int radius, int lineWidth, int r, int g, int b)
        {
            PdfContentByte cb = writer.DirectContent;
            cb.SetLineWidth(lineWidth);
            cb.SetRGBColorFill(r, g, b);
            cb.SetRGBColorStroke(20, 20, 20);
            cb.RoundRectangle(posX, posY, largeur, hauteur, radius);
            cb.FillStroke();
        }


    }
}
