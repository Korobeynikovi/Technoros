using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using RKSManager.BLL;
using RKSManager.BLL.Devices;
using RKSManager.BLL.Port;
using RKSManager.Funcs;
using RKSManager.ParentControls;
using System.Media;
using RKSManager.BLL.Collections;
using RKSManager.BLL.Enums;

namespace RKSManager.UserControls
{
    public partial class UserControlStatus : UserControlParent
    {
        #region Private Fields

        private SoundPlayer player;
        private int positionCounter;
        private bool lightIndicatorAlarmed;
        private bool localNotFoundAlarmed;
        private bool serverNotFoundAlarmed;
        private bool protectionSensorAlarmed = false;
        private bool inputSensorsErrorAlarmed = false;
        private bool outputSensorsErrorAlarmed;
        private bool rksOFFAlarmed;
        private bool rentgenOFFAlarmed;
        private bool? localNotFound;
        private bool? serverNotFound;
        private int localNotFoundCounter;                                                                                                                           
        private int serverNotFoundCounter;
        private FormWarning formWarning;
        private bool inputSensorsError = false;
        private bool outputSensorsError = false;
        private bool oldRKSOFF = true;
        private bool useOzu;
        private bool useSpeedSensor;
        private int speedSensorCount = 0;					//счетчик для датчика скорости
        private bool zeroSpeedCount = false;				//счетчик нулевой скорости датчика скорости
		private double srSpeed = 0;                         //средняя скорость
		private int srSpeedCount = 0;                       //счетчик для подсчета средней скорости 
		private bool inSpeed = false;						//указатель вывода средней скорости
        private bool zeroSpeedWarning;                       //флаг, указывающий, была нулевая скорость

        #endregion

        #region Constructor

        public UserControlStatus()
        {
            bool.TryParse(ConfigSettings.ReadSetting("UseOzu"), out useOzu);
            bool.TryParse(ConfigSettings.ReadSetting("UseSpeedSensor"), out useSpeedSensor);
            this.player = new SoundPlayer(Application.StartupPath + "\\Sounds\\warning.wav");
            this.InitializeComponent();
            this.formWarning = new FormWarning();
            this.DisableAlarms();
            if ((ConfigSettings.ReadSetting("UseOzu") != "True") && (ConfigSettings.ReadSetting("UseSpeedSensor") != "True"))
            {
                this.groupBoxOZU.Visible = false;
                this.splitterMain.Visible = false;
                this.groupBoxRKS.Dock = DockStyle.Fill;
                this.listViewDebug.Items[7].Remove();
            }
        }

        #endregion

        #region Public Properties

        public bool ServerNotFound
        {
            get { return this.serverNotFound == true; }
        }

        public bool LocalNotFound
        {
            get { return this.localNotFound == true; }
        }

        #endregion

        #region Public Methods

        public void RefreshStatus()
        {
            try
            {   
                //bool rksOFF = Detector.Status.IsDeviceON == false && Rentgen.Status.IsDeviceON == false;
                bool rksOFF = Detector.Status.IsDeviceON == false || Rentgen.Status.IsDeviceON == false;
                if (this.oldRKSOFF == true && rksOFF == true)
                    PortManager.Beep();
                this.oldRKSOFF = rksOFF;
                bool ozuError = false;
                this.listViewStatus.Items[0].SubItems[1].Text = rksOFF == false ? "Да" : "Нет";
                this.listViewStatus.Items[0].ForeColor = rksOFF == false ? Color.Black : Color.Red;
                if (rksOFF == false)
                {
                    this.listViewStatus.Items[1].SubItems[1].Text = this.localNotFound != null ? (this.localNotFound == false ? "Подключен" : "Отключен") : "";
                    this.listViewStatus.Items[1].ForeColor = this.localNotFound == true ? Color.Red : Color.Black;
                    this.listViewStatus.Items[2].SubItems[1].Text = this.serverNotFound != null ? (this.serverNotFound == false ? "Подключен" : "Отключен") : "";
                    this.listViewStatus.Items[2].ForeColor = this.serverNotFound == true ? Color.Red : Color.Black;
                    if (Rentgen.Status.DeviceStatus.IsRadiationON != null)
                    {
                        this.listViewStatus.Items[3].SubItems[1].Text = (bool)Rentgen.Status.DeviceStatus.IsRadiationON ? "Включен" : "Выключен";
                        this.listViewStatus.Items[3].ForeColor = Rentgen.Status.DeviceStatus.IsRadiationON == false ? Color.Red : Color.Black;
                    }
                    if (LightIndicator.Enabled != null || Rentgen.Status.DeviceStatus.IsRadiationON == false)
                    {
                        this.listViewStatus.Items[4].SubItems[1].Text = Rentgen.Status.DeviceStatus.IsRadiationON == true ? ((bool)LightIndicator.Enabled ? "Отключена" : "Включена") : "";
                        this.listViewStatus.Items[4].ForeColor = LightIndicator.Enabled == true ? Color.Red : Color.Black;
                    }
                    if (PositionSensor.Enabled != null)
                    {
                        this.listViewStatus.Items[5].SubItems[1].Text = (bool)PositionSensor.Enabled ? "Не рабочее" : "Рабочее";
                        this.listViewStatus.Items[5].ForeColor = PositionSensor.Enabled == true ? Color.Red : Color.Black;
                    }
                    if (ProtectionSensor.Enabled != null)
                    {
                        this.listViewStatus.Items[6].SubItems[1].Text = (bool)ProtectionSensor.Enabled ? "Не рабочее" : "Рабочее";
                        this.listViewStatus.Items[6].ForeColor = ProtectionSensor.Enabled == true ? Color.Red : Color.Black;
                    }
                    this.listViewStatus.Items[7].SubItems[1].Text = SharedFinctions.IsBeltStopped() == true ? "Да" : "Нет";
                    this.listViewStatus.Items[7].ForeColor = SharedFinctions.IsBeltStopped() == true ? Color.Red : Color.Black;
                    if (DistanceIndicator.EmptyBelt != null)
                    {
                        this.listViewStatus.Items[8].SubItems[1].Text = (bool)DistanceIndicator.EmptyBelt ? "Да" : "Нет";
                        this.listViewStatus.Items[8].ForeColor = DistanceIndicator.EmptyBelt == true ? Color.Red : Color.Black;
                    }
                }
                else
                {
                    for (int i = 1; i < 9; i++)
                    {
                        this.listViewStatus.Items[i].SubItems[1].Text = "";
                        this.listViewStatus.Items[i].ForeColor = Color.Black;
                    }
                }

                if (useSpeedSensor)
                {
                    if (OZU.Speed > 0)
                    {
                        srSpeedCount += 1;
                        srSpeed += (double)OZU.Speed;
                        srSpeed /= srSpeedCount;

                        if (srSpeedCount == 5)
						{
                            srSpeedCount = 0;
                            srSpeed = 0;
						}
                    }

                    if (OZU.Speed == 0)
                    {
						if (zeroSpeedWarning == false)
                        {
                            PortManager.Beep();
                            player.Play();
                            this.timerWarning.Start();
                            DialogResult res = this.formWarning.ShowDialog();
                            if (res == DialogResult.OK || res == DialogResult.Cancel)
                            {
                                this.stopWarning();
                            }

                            zeroSpeedWarning = true;
                        }

                        if (zeroSpeedCount == false)
                        {
                            string[] strings = new string[2];
                            strings[0] = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                            strings[1] = "Лента остановилась";
                            ListViewItem item = new ListViewItem(strings);
                            item.BackColor = Color.Red;
                            this.listViewOZUStatus.Items.Insert(0, item);

							zeroSpeedCount = true;
						}
                    }

                    if (OZU.Speed != 0)
                    {
                        if (zeroSpeedCount == true)
                        {
                            string[] strings = new string[2];
                            strings[0] = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                            strings[1] = "Движение ленты возобновилось";
                            ListViewItem item = new ListViewItem(strings);
                            item.BackColor = Color.LightGreen;
                            this.listViewOZUStatus.Items.Insert(0, item);

                            zeroSpeedCount = false;
                        }

                        zeroSpeedWarning = false;
                    }
                }

                if (useOzu)
                {
                    if (!OZU.IsWorking)
                    {
                        string[] strings = new string[2];
                        strings[0] = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                        strings[1] = "ОЗУ не отвечает";
                        ListViewItem item = new ListViewItem(strings);
                        item.BackColor = Color.Red;
                        this.listViewOZUStatus.Items.Insert(0, item);

                        PortManager.Beep();
                        player.Play();
                        this.timerWarning.Start();
                        DialogResult res = this.formWarning.ShowDialog(this.ParentForm);
                        if (res == DialogResult.OK || res == DialogResult.Cancel)
                        {
                            this.stopWarning();
                        }
                    }
                    for (int i = 0; i < 19; i++)
                    {
                        if ((OZU.Status & (int) Math.Pow(2, i)) == Math.Pow(2, i))
                        {
                            string[] strings = new string[2];
                            strings[0] = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                            strings[1] = OZU.GetError(i);
                            ListViewItem item = new ListViewItem(strings);
                            if (i <= 5)
                            {
                                item.BackColor = Color.Pink;
//                                ozuError = true;
                            }
                            if (i == 6 || i >= 15)
                            {
                                item.BackColor = Color.Red;
                                ozuError = true;
                            }
                            this.listViewOZUStatus.Items.Insert(0, item);
                        }
                    }
                    if ((OZU.Status & 1) == 1 && (OZU.Status & 2) == 2)
                    {
                        string[] strings = new string[2];
                        strings[0] = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                        strings[1] = "Вышли из строя оба входных датчика";
                        ListViewItem item = new ListViewItem(strings);
                        item.BackColor = Color.Red;
                        this.listViewOZUStatus.Items.Insert(0, item);
                        inputSensorsError = true;
                        ozuError = true;
                    }
                    if ((OZU.Status & 4) == 4 && (OZU.Status & 8) == 8)
                    {
                        string[] strings = new string[2];
                        strings[0] = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                        strings[1] = "Вышли из строя оба выходных датчика";
                        ListViewItem item = new ListViewItem(strings);
                        item.BackColor = Color.Red;
                        this.listViewOZUStatus.Items.Insert(0, item);
                        outputSensorsError = true;
                        ozuError = true;
                    }
                }
                OZU.SkipStatus();
                bool controlMode = (int)FormCurOreKind.CurOreKind + 1 == 9 || (int)FormCurOreKind.CurOreKind + 1 == 10;
                if (PositionSensor.Enabled == true && Rentgen.Status.DeviceStatus.IsRadiationON == true && controlMode == false)
                    this.positionCounter++;
                else
                    if (PositionSensor.Enabled == false || Rentgen.Status.DeviceStatus.IsRadiationON == false)
                        this.positionCounter = 0;
                if (
                     (
                        ((LightIndicator.Enabled == true && this.lightIndicatorAlarmed == false)
                            || this.positionCounter > 20 
                            || (ProtectionSensor.Enabled == true 
                            && this.protectionSensorAlarmed == false 
                            && controlMode == false))
                        && Rentgen.Status.DeviceStatus.IsRadiationON == true
                        || (rksOFF == true && this.rksOFFAlarmed == false) 
                        || (Rentgen.Status.DeviceStatus.IsRadiationON == false 
                            && PortManager.RentgenMustBeOn == true 
                            && Rentgen.Status.TurnedON == true 
                            && this.rentgenOFFAlarmed == false)
//                        || (ozuError == true)
                        || (inputSensorsError == true && this.inputSensorsErrorAlarmed == false)
                        || (outputSensorsError == true && this.outputSensorsErrorAlarmed == false)
                     )
                     && this.timerWarning.Enabled == false
                   )
				{
					if (ProtectionSensor.Enabled == true || inputSensorsError == true || outputSensorsError == true)
					{
						PortManager.RentgenOFF();
						PortManager.Beep();
						PortManager.Beep();
					}
					if (this.positionCounter > 20)
						PortManager.RentgenOFF();
					player.Play();
					this.timerWarning.Start();
					DialogResult result = this.formWarning.ShowDialog(this.ParentForm);
					if (result == DialogResult.OK || result == DialogResult.Cancel)
					{
						if (this.localNotFound == true)
							this.localNotFoundAlarmed = true;
						if (this.serverNotFound == true)
							this.serverNotFoundAlarmed = true;
						if (LightIndicator.Enabled == true)
							this.lightIndicatorAlarmed = true;
						if (rksOFF == true)
						{
							this.rksOFFAlarmed = true;
							PortManager.RentgenMustBeOn = false;
						}
						if (Rentgen.Status.DeviceStatus.IsRadiationON == false && PortManager.RentgenMustBeOn == true && Rentgen.Status.TurnedON == true && this.rentgenOFFAlarmed == false)
							this.rentgenOFFAlarmed = true;
						if (ProtectionSensor.Enabled == true)
							this.protectionSensorAlarmed = true;
						if (inputSensorsError == true)
							this.inputSensorsErrorAlarmed = true;
						if (outputSensorsError == true)
							this.outputSensorsErrorAlarmed = true;
						if (this.positionCounter > 20)
							this.positionCounter = 0;
						this.stopWarning();
					}
				}

				if ((LightIndicator.Enabled == false || Rentgen.Status.DeviceStatus.IsRadiationON == false) 
                    && PositionSensor.Enabled == false && ProtectionSensor.Enabled == false && rksOFF == false
                    && (Rentgen.Status.DeviceStatus.IsRadiationON == true || PortManager.RentgenMustBeOn == false || Rentgen.Status.TurnedON == false)
                    && ozuError == false && inputSensorsError == false && outputSensorsError == false)
                {
                    this.formWarning.DialogResult = DialogResult.Ignore;
                    this.formWarning.Hide();
                    stopWarning();
                }
                this.listViewDebug.Items[0].SubItems[1].Text = Rentgen.Status.Temperature != null ? Rentgen.Status.Temperature.ToString() + " °C" : "";
                this.listViewDebug.Items[1].SubItems[1].Text = Detector.Status.Temperature != null ? Detector.Status.Temperature.ToString() + " °C" : "";
                this.listViewDebug.Items[2].SubItems[1].Text = Detector.Status.UnitIsConfigured != null ? ((bool)Detector.Status.UnitIsConfigured ? "Да" : "Нет") : "";
                this.listViewDebug.Items[2].ForeColor = Detector.Status.UnitIsConfigured == false ? Color.Red : Color.Black;
                this.listViewDebug.Items[3].SubItems[1].Text = Detector.Status.AccumulationTime != null ? Detector.Status.AccumulationTime.ToString() + " с" : "";
                this.listViewDebug.Items[4].SubItems[1].Text = DistanceIndicator.Length != null ? (Math.Round((double)DistanceIndicator.Length, 2)).ToString() + " мм" : "";
                long integral = 0;
                if (Detector.Data != null && Detector.Data.Length > 0 && Detector.Status.AccumulationTime != null &&
                    Detector.Status.AccumulationTime != 0)
                {
                    long[] spectrumData = (long[])Detector.Data.Clone();
                    for (int j = 0; j < spectrumData.Length; j++)
                        spectrumData[j] = (long)(spectrumData[j] * (30.0 / (double)Detector.Status.AccumulationTime));
                    integral = RegionsCollection.GetRegion(Regions.I).GetImpulsesAmount(spectrumData);
                }
                this.listViewDebug.Items[5].SubItems[1].Text = integral.ToString();
                if (FormMain.CurOreKind != null)
                    this.listViewDebug.Items[6].SubItems[1].Text = FormMain.CurOreKind.Name;
				if (this.listViewDebug.Items.Count > 7)
				{
					if (useSpeedSensor == true && srSpeed != 0)
					{
						this.listViewDebug.Items[7].SubItems[1].Text = OZU.Speed != null ? (Math.Round((double)OZU.Speed,3)).ToString() + " м/с," + " (" + (Math.Round(srSpeed,3)) + ")": "";
						this.listViewDebug.Items[7].ForeColor = OZU.Speed == 0 ? Color.Red : Color.Black;
					}
					if ((useSpeedSensor == true && srSpeed <= 0) || useOzu == true)
					{
						this.listViewDebug.Items[7].SubItems[1].Text = OZU.Speed != null ? (Math.Round((double)OZU.Speed, 3)).ToString() + " м/с" : "";
						this.listViewDebug.Items[7].ForeColor = OZU.Speed == 0 ? Color.Red : Color.Black;
					}
				}
				if (useOzu && ozuError)
                {
                    PortManager.Beep();
                    player.Play();
                    this.timerWarning.Start();
                    DialogResult res = this.formWarning.ShowDialog(this.ParentForm);
                    if (res == DialogResult.OK || res == DialogResult.Cancel)
                    {
                        this.stopWarning();
                    }
                }
            }
            catch (Exception ex)
            {
//                this.showException(ex);
            }
        }

        public void DisableAlarms()
        {
            this.lightIndicatorAlarmed = false;
            this.localNotFoundAlarmed = false;
            this.serverNotFoundAlarmed = false;
            this.protectionSensorAlarmed = false;
            this.rksOFFAlarmed = false;
            this.rentgenOFFAlarmed = false;
            this.localNotFound = null;
            this.serverNotFound = null;
            this.positionCounter = 0;
            this.localNotFoundCounter = 0;
            this.serverNotFoundCounter = 0;
        }

        public void SetLocalNotFound()
        {
            this.localNotFoundCounter++;
            if (this.localNotFoundCounter > 2)
                this.localNotFound = true;
        }

        public void SetLocalFound()
        {
            this.localNotFound = false;
            this.localNotFoundCounter = 0;
        }

        public void SetServerNotFound()
        {
            this.serverNotFoundCounter++;
            if (this.serverNotFoundCounter > 2)
                this.serverNotFound = true;
        }

        public void SetServerFound()
        {
            this.serverNotFound = false;
            this.serverNotFoundCounter = 0;
        }

        public void SetWorkMode()
        {
            this.listViewDebug.Visible = false;
        }

        #endregion

        #region Private Methods

        private void timerWarning_Tick(object sender, EventArgs e)
        {
            player.Play();
        }

        private void stopWarning()
        {
            this.timerWarning.Stop();
            this.player.Stop();
        }

        #endregion
    }
}
