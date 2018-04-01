namespace RET
{
    partial class Form_AffichageImage
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pb_Schema = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pb_Schema)).BeginInit();
            this.SuspendLayout();
            // 
            // pb_Schema
            // 
            this.pb_Schema.Location = new System.Drawing.Point(0, 0);
            this.pb_Schema.Name = "pb_Schema";
            this.pb_Schema.Size = new System.Drawing.Size(1731, 969);
            this.pb_Schema.TabIndex = 0;
            this.pb_Schema.TabStop = false;
            // 
            // Form_AffichageImage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1727, 970);
            this.Controls.Add(this.pb_Schema);
            this.Name = "Form_AffichageImage";
            this.Text = "Form_AffichageImage";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form_AffichageImage_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.pb_Schema)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pb_Schema;
    }
}