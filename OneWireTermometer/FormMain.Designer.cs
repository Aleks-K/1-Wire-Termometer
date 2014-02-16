namespace OneWireTermometer
{
    partial class FormMain
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.labelTemperature = new System.Windows.Forms.Label();
            this.timerUpdateT = new System.Windows.Forms.Timer(this.components);
            this.oneWire1 = new OneWire.OneWire();
            this.SuspendLayout();
            // 
            // labelTemperature
            // 
            this.labelTemperature.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTemperature.Font = new System.Drawing.Font("Verdana", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelTemperature.Location = new System.Drawing.Point(12, 9);
            this.labelTemperature.Name = "labelTemperature";
            this.labelTemperature.Size = new System.Drawing.Size(874, 406);
            this.labelTemperature.TabIndex = 0;
            this.labelTemperature.Text = "UNKNOWN";
            this.labelTemperature.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // timerUpdateT
            // 
            this.timerUpdateT.Interval = 5000;
            this.timerUpdateT.Tick += new System.EventHandler(this.timerUpdateT_Tick);
            // 
            // oneWire1
            // 
            this.oneWire1.PortName = "COM1";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(898, 424);
            this.Controls.Add(this.labelTemperature);
            this.Name = "FormMain";
            this.Text = "1-Wire termometer";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelTemperature;
        private System.Windows.Forms.Timer timerUpdateT;
        private OneWire.OneWire oneWire1;
    }
}

