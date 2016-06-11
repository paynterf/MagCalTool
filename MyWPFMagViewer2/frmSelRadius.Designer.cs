namespace MagManager
{
    partial class frm_SelRadius
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
            this.nud_Radius = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbl_NumPtsSel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Radius)).BeginInit();
            this.SuspendLayout();
            // 
            // nud_Radius
            // 
            this.nud_Radius.Location = new System.Drawing.Point(22, 49);
            this.nud_Radius.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nud_Radius.Name = "nud_Radius";
            this.nud_Radius.Size = new System.Drawing.Size(120, 20);
            this.nud_Radius.TabIndex = 0;
            this.nud_Radius.ValueChanged += new System.EventHandler(this.nud_Radius_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(145, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Selection Radius (local Units)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Points Selected:";
            // 
            // lbl_NumPtsSel
            // 
            this.lbl_NumPtsSel.AutoSize = true;
            this.lbl_NumPtsSel.Location = new System.Drawing.Point(121, 76);
            this.lbl_NumPtsSel.Name = "lbl_NumPtsSel";
            this.lbl_NumPtsSel.Size = new System.Drawing.Size(35, 13);
            this.lbl_NumPtsSel.TabIndex = 2;
            this.lbl_NumPtsSel.Text = "label2";
            // 
            // frm_SelRadius
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 128);
            this.Controls.Add(this.lbl_NumPtsSel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nud_Radius);
            this.Name = "frm_SelRadius";
            this.Text = "Select Beyond Radius";
            this.Load += new System.EventHandler(this.frm_SelRadius_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nud_Radius)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown nud_Radius;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbl_NumPtsSel;
    }
}