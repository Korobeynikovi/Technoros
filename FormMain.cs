using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using FormulaParser;
using RKSManager.BLL;
using RKSManager.BLL.Devices;
using RKSManager.BLL.Port;
using RKSManager.Funcs;
using RKSManager.ParentControls;
using RKSManager.BLL.Spectrum.Collections;
using RKSManager.BLL.Spectrum.Enums;
using RKSManager.BLL.Structs;
using RKSManager.BLL.Spectrum;
using RKSManager.BLL.Collections;
using RKSManager.Properties;
using System.Threading;
using System.Diagnostics;
using FunctionsEx;
using RKSManager.BLL.Devices.Enums.Rentgen;
using RKSManager.BLL.Devices.Enums.Detector;
using RKSManager.UserControls;
using System.IO;
using RKSManager.UI.Forms;

namespace RKSManager
{
    public partial class FormMain : FormParent
    {
        #region Private Fields

        private static Thread splashThread;
        private static SplashForm splashForm;

        private FormBounds formBounds;          //форма границ содержаний типов руд
        private FormCurOreKind formCurOreKind;  //форма текущего типа руды
        private FormAbout formAbout;            //форма информации о программе
        private List<Composition> compositions; //список содержаний
        private int rksID;                      //id ркс
        private int rksID1;                     //id ркс если идет отгрузка на склад
        private int rksIDInput1;                //id ркс при приеме руды с рудника Таймырский
        private int rksIDInput2;                //id ркс при приеме руды с рудника Октябрьский
        private int rksIDInput3;                //id ркс при приеме руды с рудника Скалистый
        private int counter;                    //счетчик количества обновлений
        private int disableRKSON;               //флаг, запрещающий включить режим излучения
        private double v;                       //скорость конвейера, м/с
        private double t;                       //время аккумулирования, с
        private bool exit = false;              //флаг выхода из программы
        public static OreKind CurOreKind;       //текущий тип руды
        private static FormMain instance;       //переменная для статического доступа к объекту главной формы
        private long ozuStatus = 0;             //состояние ОЗУ
        private bool useOpc;                    //использование OPC
        private string Vcorrection;             //формула коррекции объема
        private bool useSpeedIndicator;         //использование датчика скорости


        #endregion

        public static FormMain Instance
        {
            get 
            {
                return instance;
            }
        }

        #region Constructor

        public FormMain()
        {
            try
            {
                splashThread = new Thread(new ThreadStart(ShowSplash));
                splashThread.Start();

                if (!Directory.Exists(Application.StartupPath + "\\Logs"))
                    Directory.CreateDirectory(Application.StartupPath + "\\Logs");

                Detector.StartupPath = Application.StartupPath;
                ConfigSettings.StartupPath = Application.StartupPath;
                Vcorrection = ConfigSettings.ReadSetting("Vcorrection").ToLower();
                bool.TryParse(ConfigSettings.ReadSetting("UseOpc"), out useOpc);
                bool.TryParse(ConfigSettings.ReadSetting("UseSpeedSensor"), out useSpeedIndicator);
                OZU.WithoutSpeedIndicator = useSpeedIndicator;
                this.InitializeComponent();
                instance = this;
                Rentgen.LoadConfiguration(Application.StartupPath + "\\Settings\\rentgenConfig.rcf");
                DistanceIndicator.Height = double.Parse(ConfigurationManager.AppSettings["Height"]);
                DistanceIndicator.Width = double.Parse(ConfigurationManager.AppSettings["Width"]);
                DistanceIndicator.K = double.Parse(ConfigurationManager.AppSettings["K"]);
                DistanceIndicator.Tga = double.Parse(ConfigurationManager.AppSettings["Tga"]);
                DistanceIndicator.StopDelta = double.Parse(ConfigurationManager.AppSettings["StopDelta"]);
                DistanceIndicator.EmptyDelta = double.Parse(ConfigurationManager.AppSettings["EmptyDelta"]);
                DistanceIndicator.Count = int.Parse(ConfigurationManager.AppSettings["Count"]);
                this.v = double.Parse(ConfigurationManager.AppSettings["V"]);
                this.t = double.Parse(ConfigurationManager.AppSettings["RefreshTime"]);
                this.timerRefreshStatus.Interval = (int) (this.t*1000/8);
                Detector.LoadConfiguration(Application.StartupPath + "\\Settings\\detectorConfig.dcf");
                Detector.Configuration.AccumulationMode = AccumulationModes.Clear;
                RegionsCollection.LoadRegions(Application.StartupPath + "\\Settings\\regions.rgs");
                OreKindsCollection.LoadOreKindBounds(Application.StartupPath + "\\Settings\\oreKinds.oks");
                OreSeparationParameter.LoadSeparationFormula(Application.StartupPath +
                                                             "\\Settings\\separationFormula.osp");
                DistanceIndicator.LoadBounds(Application.StartupPath + "\\Settings\\distanceBounds.dbs");
                this.setDistanceIndicatorBounds();
                this.formBounds = new FormBounds();
                this.formAbout = new FormAbout();
                this.formBounds.DistanceIndicatorBoundsChanged +=
                    new EventHandler(formBounds_DistanceIndicatorBoundsChanged);
                this.formBounds.CurOreKindElementsBoundsChanged += new EventHandler(formCurOreKind_CurOreKindChanged);
                this.formCurOreKind = new FormCurOreKind();
                this.formCurOreKind.CurOreKindChanged += new EventHandler(formCurOreKind_CurOreKindChanged);
                this.formCurOreKind.LoadCurOreKind(Application.StartupPath + "\\Settings\\curOreKind.cok");
                this.formBounds.Fill();
                this.setCurOreKindBounds();
                this.compositions = new List<Composition>();
                this.rksID = int.Parse(ConfigurationManager.AppSettings["RKSID"]);
                this.rksID1 = int.Parse(ConfigurationManager.AppSettings["RKSID1"]);
                this.rksIDInput1 = int.Parse(ConfigurationManager.AppSettings["RKSIDInput1"]);
                this.rksIDInput2 = int.Parse(ConfigurationManager.AppSettings["RKSIDInput2"]);
                this.rksIDInput3 = int.Parse(ConfigurationManager.AppSettings["RKSIDInput3"]);
                this.radioButtonOutput1.Text = ConfigurationManager.AppSettings["RKSIDName"];
                this.radioButtonOutput2.Text = ConfigurationManager.AppSettings["RKSID1Name"];
                this.radioButtonInput1.Text = ConfigurationManager.AppSettings["RKSIDInput1Name"];
                this.radioButtonInput2.Text = ConfigurationManager.AppSettings["RKSIDInput2Name"];
                this.radioButtonInput3.Text = ConfigurationManager.AppSettings["RKSIDInput3Name"];
                this.groupBoxDirection.Visible = this.rksID1 != -1;
                this.groupBoxInput.Visible = this.rksIDInput1 != -1 && this.rksIDInput2 != -1 && this.rksIDInput3 != -1;
                this.loadSettings(Application.StartupPath + "\\Settings\\workSettings.stg");
                this.setMode(ConfigurationManager.AppSettings["Mode"]);
                PortManager.BaudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]);
                PortManager.InitPort(ConfigurationManager.AppSettings["PortName"]);
                PortManager.ConfigureDetector();
                PortManager.RefreshStatus(true);
                this.Text = ConfigurationManager.AppSettings["RKSName"];
                this.counter = -4;
                this.disableRKSON = -1;
                this.timerRefreshStatus.Enabled = true;
                if (this.rksIDInput1 != -1 && this.rksIDInput2 != -1 && this.rksIDInput3 != -1)
                    Contents.CheckLostRecords(this.rksIDInput1, this.rksIDInput2, this.rksIDInput3);
                else if (this.rksID != -1 && this.rksID1 != -1)
                    Contents.CheckLostRecords(this.rksID, this.rksID1, this.rksID1);
                else if (this.rksID != -1)
                    Contents.CheckLostRecords(this.rksID, this.rksID, this.rksID);
                this.buttonBounds.Enabled = (int) FormCurOreKind.CurOreKind < 4;

                bool tmp = false;
                bool tmp1 = false;
                bool.TryParse(ConfigSettings.ReadSetting("UseOzu"), out tmp);
                bool.TryParse(ConfigSettings.ReadSetting("UseSpeedSensor"), out tmp1);
                this.dSToolStripMenuItem.Checked = tmp1;
                this.OZUToolStripMenuItem.Checked = tmp;
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException)
                    this.showException(
                        new Exception(string.Format("Порт {0} занят. Возможно приложение уже запущено.",
                            ConfigurationManager.AppSettings["PortName"])));
                else
                    this.showException(ex);
                Process.GetCurrentProcess().Kill();
            }
            finally
            {
                splashForm.Stopped = true;
                Thread.Sleep(100);
                splashThread.Abort();
            }
        }

        private static void ShowSplash()
        {
            using (splashForm = new SplashForm())
            {
                splashForm.ShowDialog();
            }
        }

        private void setMode(string mode)
        {
            if (mode == "Work")
                this.setWorkMode();                
            else
                this.setControlMode();
        }

        private void setControlMode()
        {
            this.userControlStatus.Height = 363;
            int height = (this.ClientSize.Height - this.panelControl.Height - this.userControlStatus.Height - this.MainMenuStrip.Height - this.statusStripMain.Height
                - userControlComposition.Height - this.splitter1.Height - this.splitter2.Height - this.splitter3.Height) / 4;
            this.userControlGraphHeight.Height = height;
            this.userControlGraphCu.Height = height;
            this.userControlGraphNi.Height = height;
            this.userControlSpectrum.Height = height;
        }

        #endregion

        #region Private Fields

        private void loadSettings(string fileName)
        {
            WindowSettings settings = (WindowSettings)Functions.LoadObjectFromFile(typeof(WindowSettings), fileName);
            this.Left = settings.Left;
            this.Top = settings.Top;
            this.Width = settings.Width;
            this.Height = settings.Height;
            this.WindowState = settings.WindowState;
        }

        private void setWorkMode()
        {
            this.userControlStatus.SetWorkMode();
            this.userControlSpectrum.Visible = false;
            this.splitter3.Visible = false;
            this.userControlGraphCu.Dock = DockStyle.Fill;
            this.userControlStatus.Height = 177;
            int height = (this.ClientSize.Height - this.panelControl.Height - this.userControlStatus.Height - this.MainMenuStrip.Height - this.statusStripMain.Height
                - userControlComposition.Height - this.splitter1.Height - this.splitter2.Height) / 3;
            this.userControlGraphHeight.Height = height;
            this.userControlGraphCu.Height = height;
            this.userControlGraphNi.Height = height;
            this.buttonBounds.Enabled = false;
            this.buttonChangeCurOre.Enabled = false;
        }

        private void formBounds_DistanceIndicatorBoundsChanged(object sender, EventArgs e)
        {
            try
            {
                this.setDistanceIndicatorBounds();
            }
            catch (Exception ex)
            {
                this.showException(ex);
            }
        }

        private void formCurOreKind_CurOreKindChanged(object sender, EventArgs e)
        {
            try
            {
                this.setCurOreKindBounds();
            }
            catch (Exception ex)
            {
                this.showException(ex);
            }
        }

        private void setCurOreKindBounds()
        {
            OreKind curOreKind = OreKindsCollection.GetOreKind(FormCurOreKind.CurOreKind);
            this.userControlGraphNi.SetBounds(curOreKind.ElementsParams[(int)Elements.Ni].Bounds.LowBound,
                curOreKind.ElementsParams[(int)Elements.Ni].Bounds.HighBound);
            this.userControlGraphCu.SetBounds(curOreKind.ElementsParams[(int)Elements.Cu].Bounds.LowBound,
                curOreKind.ElementsParams[(int)Elements.Cu].Bounds.HighBound);
        }

        private void setDistanceIndicatorBounds()
        {
            this.userControlGraphHeight.SetBounds(DistanceIndicator.Bounds.LowBound, DistanceIndicator.Bounds.HighBound);
        }

        private void timerRefreshStatus_Tick(object sender, EventArgs e)
        {            
            try
            {
               if (this.counter % 8 == 0 && this.counter >= 0)
                {
                    double ni = 0, cu = 0, ni_, cu_, fe_, ns_, i_;                    
                    CurOreKind = OreKindsCollection.GetOreKind(FormCurOreKind.CurOreKind);
                    if (Rentgen.Status.DeviceStatus.IsRadiationON == true && Detector.Data != null && Detector.Data.Length > 0)
                    {
                        //обновить спектр
                        this.userControlSpectrum.Refresh(Detector.Data);
                        if (Detector.Status.AccumulationTime != null && Detector.Status.AccumulationTime != 0 && DistanceIndicator.Distance != null && Rentgen.Status.Temperature != null && Detector.Status.Temperature != null)
                        {
                            if (FormCurOreKind.CurOreKind == OreKinds.Auto) //автоматическое определние типа руды
                            {
                                //клонирование данных спектра
                                long[] spectrumData = (long[])Detector.Data.Clone();
                                //приведение спектра к 30 секундам
                                for (int j = 0; j < spectrumData.Length; j++)
                                    spectrumData[j] = (long)(spectrumData[j] * (30.0 / (double)Detector.Status.AccumulationTime));
                                //определение количества импульсов в областях
                                ni_ = RegionsCollection.GetRegion(RKSManager.BLL.Enums.Regions.Ni).GetImpulsesAmount(spectrumData);
                                cu_ = RegionsCollection.GetRegion(RKSManager.BLL.Enums.Regions.Cu).GetImpulsesAmount(spectrumData);
                                fe_ = RegionsCollection.GetRegion(RKSManager.BLL.Enums.Regions.Fe).GetImpulsesAmount(spectrumData);
                                ns_ = RegionsCollection.GetRegion(RKSManager.BLL.Enums.Regions.Ns).GetImpulsesAmount(spectrumData);
                                i_ = RegionsCollection.GetRegion(RKSManager.BLL.Enums.Regions.I).GetImpulsesAmount(spectrumData);
                                //определение типа текущей руды
                                CurOreKind = Contents.GetOreKind(ni_, cu_, fe_, ns_, i_, (double)DistanceIndicator.Distance);
                            }
                            //определение содержаний
                            ni = Math.Round(CurOreKind.ElementsParams[(int)Elements.Ni].GetComposition(Detector.Data, (float)Detector.Status.AccumulationTime, (double)DistanceIndicator.Distance, (int)Rentgen.Status.Temperature, (int)Detector.Status.Temperature), 2);
                            cu = Math.Round(CurOreKind.ElementsParams[(int)Elements.Cu].GetComposition(Detector.Data, (float)Detector.Status.AccumulationTime, (double)DistanceIndicator.Distance, (int)Rentgen.Status.Temperature, (int)Detector.Status.Temperature), 2);
                        }
                    }                    
                    if (ni == -1 || cu == -1)   //если обнаружен цинк
                    {
                        DistanceIndicator.EmptyBelt = true;                        
                    }
                    DistanceIndicator.Zink = ni == -1 || cu == -1;
                    if (ni < 0 || double.IsNaN(ni) || double.IsInfinity(ni) || DistanceIndicator.EmptyBelt == true)
                        ni = 0;
                    if (cu < 0 || double.IsNaN(cu) || double.IsInfinity(cu) || DistanceIndicator.EmptyBelt == true) 
                        cu = 0;
                    //double r = curOreKind.Density;              //плотность руды, т/куб. м.
                    double s = DistanceIndicator.GetSquare();   //площадь поперечного сечения руды на конвейере, квад. м
                    double totalMass = Math.Round(s * v * t, 3);

                    //коррекция объема
                    string str = Vcorrection;
                    str = Parser.ReplaceConstant(str, "v", totalMass);
                    totalMass = Parser.Calc(str);

                    //режим отладки если выбрана рабочая или поверочная проба или в файле конфигурации контрольный режим
                    bool controlMode = (int)FormCurOreKind.CurOreKind + 1 == 9 || (int)FormCurOreKind.CurOreKind + 1 == 10 ||
                        ConfigurationManager.AppSettings["Mode"] != "Work";


                    //if (totalMass < 0 || (SharedFinctions.IsBeltStopped() == true && controlMode == false)
                    //    || DistanceIndicator.EmptyBelt == true) 
                    //    totalMass = 0;
                    if (ni == 0 || cu == 0 || totalMass == 0)
                    {
                        ni = 0;
                        cu = 0;
                        totalMass = 0;
                    }
                    double length = DistanceIndicator.Length != null ? (double)DistanceIndicator.Length : 0;
                    int shortState = this.getShortState(controlMode);
                    int fullState = this.getFullState();
                    
                    //обновление графиков
                    this.userControlGraphNi.Refresh(new GraphPoint(ni, shortState));
                    this.userControlGraphCu.Refresh(new GraphPoint(cu, shortState));
                    this.userControlGraphHeight.Refresh(new GraphPoint(Math.Round(length, 0), shortState));
                    int id = this.rksID;
                    int[] emptyIDs = new int[] {-1, -1};
                    if(this.rksID1 != -1)
                    {
                        id = this.radioButtonOutput1.Checked == true ? this.rksID: this.rksID1;
                        emptyIDs[0] = this.radioButtonOutput1.Checked == true ? this.rksID1 : this.rksID;
                    }
                    else
                    if(this.rksIDInput1 != -1 && this.rksIDInput2 != -1 && this.rksIDInput3 != -1)
                    {
                        if (this.radioButtonInput1.Checked == true)
                        {
                            id = this.rksIDInput1;
                            emptyIDs[0] = this.rksIDInput2;
                            emptyIDs[1] = this.rksIDInput3;
                        }
                        else
                            if (this.radioButtonInput2.Checked == true)
                            {
                                id = this.rksIDInput2;
                                emptyIDs[0] = this.rksIDInput1;
                                emptyIDs[1] = this.rksIDInput3;
                            }
                            else
                                if (this.radioButtonInput3.Checked == true)
                                {
                                    id = this.rksIDInput3;
                                    emptyIDs[0] = this.rksIDInput1;
                                    emptyIDs[1] = this.rksIDInput2;
                                }
                    }
                int curOreKindID = (int)OreKindsCollection.FindOreKind(CurOreKind);
                    for (int i = 0; i < 2; i++)
                        if(emptyIDs[i] != -1)
                        {
                            Composition emptyComposition = new Composition(emptyIDs[i], DateTime.Now, 0, 0, 0,
                                curOreKindID, 2);
                            //добавление записи в СУБД на удаленном сервере
                            Contents.AddToServer(emptyComposition);
                            //добавление записи в локальную СУБД
                            Contents.AddToLocal(emptyComposition);
                        }

                    if (useOpc)
                    {
                        //запись файлов для OPC
                        TextWriter writer = new StreamWriter(Application.StartupPath + "\\s_tags.txt", true);
                        writer.Write("{0};", Rentgen.Status.DeviceStatus.IsRadiationON == true ? 1 : 0);
                        writer.Write("{0};", (int) Rentgen.Status.WorkMode);
                        writer.Write("{0};", Rentgen.Status.Temperature);
                        writer.Write("{0};", Rentgen.Status.WorkTime.Hours*60 + Rentgen.Status.WorkTime.Minutes);
                        writer.Write("{0};", Rentgen.Status.StartsCount);
                        writer.Write("{0};", Rentgen.Status.DeviceNumber);
                        writer.Write("{0};", Detector.Status.IsDeviceON == true ? 1 : 0);
                        writer.Write("{0};", Detector.Status.AccumulationTime.ToString().Replace(',', '.'));
                        writer.Write("{0};", Detector.Status.Temperature);
                        writer.Write("{0};", Detector.Status.SerialNumber);
                        writer.Write("{0};", DateTime.Now.ToShortDateString() + "-" + DateTime.Now.ToLongTimeString());
                        writer.Write("{0};", ni.ToString().Replace(',', '.'));
                        writer.Write("{0};", cu.ToString().Replace(',', '.'));
                        writer.Write("{0};", (int) totalMass);
                        writer.Write("{0};", curOreKindID);
                        writer.Write("{0};", fullState);
                        writer.Write("{0};", ozuStatus);
                        writer.Write("{0};", id);
                        writer.Write("{0};", PositionSensor.Enabled == true ? 1 : 0);
                        writer.Write("{0};", ProtectionSensor.Enabled == true ? 1 : 0);
                        writer.Write("{0};", LightIndicator.Enabled == true ? 1 : 0);
                        writer.Write("{0};", (int) ThermoCouple.Temperature);
                        writer.Write("{0};", (int) length);
                        writer.Write(Environment.NewLine);
                        writer.Close();

                        TextReader reader = new StreamReader(Application.StartupPath + "\\c_tags.txt");
                        int rentgen = int.Parse(reader.ReadLine());
                        reader.Close();

                        if (Rentgen.Status.DeviceStatus.IsRadiationON == false && rentgen == 1)
                            PortManager.RentgenON(Rentgen.Configuration.WorkMode);
                        //if (Rentgen.Status.DeviceStatus.IsRadiationON == true && rentgen == 0)
                            //PortManager.RentgenOFF();
                    }

                    ozuStatus = 0;

                    Composition shortComposition = new Composition(id, DateTime.Now, ni, cu, totalMass,
                        curOreKindID, shortState);
                    Composition fullComposition = new Composition(id, DateTime.Now, ni, cu, totalMass,
                        curOreKindID, fullState);
                    this.compositions.Add(shortComposition);
                    //добавление записи в СУБД на удаленном сервере
                    Contents.AddToServer(fullComposition);
                    //добавление записи в локальную СУБД
                    Contents.AddToLocal(fullComposition);
                    new Thread(new ThreadStart(this.saveCompositions)).Start();
                    if (this.compositions.Count == 20)
                        this.addMeasurement();                    
                }

                ozuStatus = ozuStatus | OZU.Status;
                if(this.counter > -1)
                    this.refresh();

                if (this.counter++ > 9999) this.counter = 8;
                //обновление информации через порт
                PortManager.RefreshStatus(this.counter % 8 == 0);
                OZU.DeleteSpeed();
                if(this.counter % 8 == 0)
                    DistanceIndicator.SkipDistances();
            }
            catch (Exception ex)
            {
                //this.showException(ex);
            }
        }

        private int getShortState(bool controlMode)
        {
            int state = 0;
            if (Rentgen.Status.IsDeviceON == false)
                state += 1;
            if (Rentgen.Status.DeviceStatus.IsRadiationON == false)
                state += 2;
            if (Detector.Status.IsDeviceON == false)
                state += 4;
            if (PositionSensor.Enabled == true && controlMode == false)
                state += 8;
            if (SharedFinctions.IsBeltStopped() == true && controlMode == false)
                state += 16;
            if (DistanceIndicator.EmptyBelt == true)
                state += 32;
            return state;
        }


        private int getFullState()
        {
            int state = getShortState(false);
            if (ProtectionSensor.Enabled == true)
                state += 64;
            if (LightIndicator.Enabled == true)
                state += 128;
            if (Detector.Status.UnitIsConfigured == false)
                state += 256;
            if (this.userControlStatus.LocalNotFound == true)
                state += 512;
            if (this.userControlStatus.ServerNotFound == true)
                state += 1024;
            return state;
        }

        private void addMeasurement()
        {
            double niAvg = 0, cuAvg = 0, mass = 0;
            int state = 1, count = 0, oreKindID = 0, max = 0;
            int [] oreKinds = new int[20];
            foreach (Composition cmp in this.compositions)
            {
                if (cmp.State == 0 && cmp.Ni > 0 && cmp.Cu > 0 && cmp.TotalMass > 0)
                {
                    state = 0;
                    niAvg += cmp.Ni * cmp.TotalMass;
                    cuAvg += cmp.Cu * cmp.TotalMass;
                    mass += cmp.TotalMass;
                    oreKinds[cmp.OreKindID]++;
                    count++;
                }
            }
            if (mass > 0)
            {
                niAvg /= mass;
                cuAvg /= mass;
            }
            for (int i = 0; i < 20; i++)
            {
                if (oreKinds[i] > max)
                {
                    max = oreKinds[i];
                    oreKindID = i;
                }
            }
            if (oreKindID > 0)
            {
                Composition composition = new Composition(this.compositions[19].RKSID, this.compositions[19].EndTime,
                    Math.Round(niAvg, 2), Math.Round(cuAvg, 2), Math.Round(mass, 2), oreKindID, state);
                this.userControlComposition.AddMeasurement(composition);
            }
            this.compositions.Clear(); 
        }

        private void refresh()
        {
            this.userControlStatus.RefreshStatus();            
            bool radiationON = Rentgen.Status.DeviceStatus.IsRadiationON == true;
            bool controlMode = (int)FormCurOreKind.CurOreKind + 1 == 9 || (int)FormCurOreKind.CurOreKind + 1 == 10;
            this.buttonRKSOn.Enabled = (ProtectionSensor.Enabled == false && PositionSensor.Enabled == false)
                || radiationON == true || controlMode == true;
            if (this.disableRKSON == 1 && Rentgen.Status.DeviceStatus.IsRadiationON != null)
            {
                this.buttonRKSOn.Tag = radiationON == true ? "OFF" : "ON";
                this.buttonRKSOn.Text = radiationON == true ? "Выключить РКС" : "Включить РКС";
                this.buttonRKSOn.BackColor = radiationON == true ? Color.Red : SystemColors.Control;
            }
            else
                if(this.disableRKSON < 1)
                    this.disableRKSON++;
            if (this.exit == true && Rentgen.Status.DeviceStatus.IsRadiationON == false)
                this.Close();
        }

        private void saveCompositions()
        {
            try
            {
                Contents.SaveToLocal();
                this.userControlStatus.SetLocalFound();
            }
            catch
            {
                this.userControlStatus.SetLocalNotFound();
            }
            try
            {
                Contents.SaveToServer();
                this.userControlStatus.SetServerFound();
            }
            catch
            {
                this.userControlStatus.SetServerNotFound();
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (this.exit == false && MessageBox.Show(this, "Вы уверены, что хотите выйти из программы?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    this.Cursor = Cursors.WaitCursor;
                    if (Rentgen.Status.DeviceStatus.IsRadiationON == true)
                    {
                        PortManager.RentgenOFF();
                        this.exit = true;
                        e.Cancel = true;
                        return;
                    }
                }
                else
                if(this.exit == false)
                {
                    e.Cancel = true;
                }
                if (e.Cancel == false)
                {
                    this.timerRefreshStatus.Stop();
                    PortManager.ClosePort();
                    if (ConfigurationManager.AppSettings["Mode"] != "Work")
                    {
                        OreKindsCollection.SaveOreKindBounds(Application.StartupPath + "\\Settings\\oreKinds.oks");
                        DistanceIndicator.SaveBounds(Application.StartupPath + "\\Settings\\distanceBounds.dbs");
                        this.formCurOreKind.SaveCurOreKind(Application.StartupPath + "\\Settings\\curOreKind.cok");
                        if (this.WindowState != FormWindowState.Minimized)
                            this.saveSettings(Application.StartupPath + "\\Settings\\workSettings.stg");
                    }
                }
            }
            catch (Exception ex)
            {
                this.showException(ex);
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void saveSettings(string fileName)
        {
            WindowSettings settings = new WindowSettings(this.Left, this.Top, this.Width, this.Height, this.WindowState);
            Functions.SaveObjectToFile(settings, fileName);
        }

        private void buttonRKSOn_Click(object sender, EventArgs e)
        {
            try
            {
                Button button = sender as Button;
                if ((sender as Button).Tag.ToString() == "ON")
                {
                    //PortManager.ConfigureDetector();
                    PortManager.RentgenON(Rentgen.Configuration.WorkMode);
                    button.Tag = "OFF";
                    button.Text = "Выключить РКС";
                    button.BackColor = Color.Red;
                    this.userControlStatus.DisableAlarms();
                    this.counter = -20;
                }
                else
                {
                    PortManager.RentgenOFF();
                    button.Tag = "ON";
                    button.Text = "Включить РКС";
                    button.BackColor = SystemColors.Control;
                }
                this.disableRKSON = -5;
            }
            catch (Exception ex)
            {
                this.showException(ex);
            }
        }

        private void buttonBounds_Click(object sender, EventArgs e)
        {
            try
            {
                this.formBounds.ShowDialog();
            }
            catch (Exception ex)
            {
                this.showException(ex);
            }
        }

        private void buttonChangeCurOreKind_Click(object sender, EventArgs e)
        {
            try
            {
                this.formCurOreKind.ShowDialog();
                this.buttonBounds.Enabled = (int)FormCurOreKind.CurOreKind < 4;
            }
            catch (Exception ex)
            {
                this.showException(ex);
            }
        }

        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripMenuItemAbout_Click(object sender, EventArgs e)
        {
            this.formAbout.ShowDialog();
        }

        private void dSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
            "Вы уверены, что хотите включить ОЗУ?",
            "Внимание!",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1,
            MessageBoxOptions.DefaultDesktopOnly);

            if (result == DialogResult.Yes)
            {
                MessageBox.Show("Датчик скорости включен, перезагрузите программу",
                "Внимание!",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification);
                ConfigSettings.WriteSetting("UseOzu", "False");
                ConfigSettings.WriteSetting("UseSpeedSensor", "True");
            }
        }

        private void OZUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
            "Вы уверены, что хотите включить ОЗУ?",
            "Внимание!",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1,
            MessageBoxOptions.DefaultDesktopOnly);

            if (result == DialogResult.Yes)
            {
                MessageBox.Show("ОЗУ включен, перезагрузите программу",
                "Внимание!",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification);
                ConfigSettings.WriteSetting("UseOzu", "True");
                ConfigSettings.WriteSetting("UseSpeedSensor", "False");
            }
        }

        private void dSToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (dSToolStripMenuItem.Checked == true)
            {
                OZUToolStripMenuItem.Checked = false;
            }
            else
            {
                OZUToolStripMenuItem.Checked = true;
            }
        }


        private void OZUToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (OZUToolStripMenuItem.Checked == true)
            {
                dSToolStripMenuItem.Checked = false;
            }
            else
            {
                dSToolStripMenuItem.Checked = true;
            }
        }

        #endregion
    }
}