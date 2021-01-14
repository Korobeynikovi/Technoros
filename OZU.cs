using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using RKSManager.BLL.Devices.Enums.Commands;
using RKSManager.BLL.Devices.Exceptions;
using System.IO;

namespace RKSManager.BLL.Devices
{
    public static class OZU
    {
        public static bool IsWorking = false;
        public static byte Sensitive;
        public static long Status = 0;
        public static bool WorkState = true;
        public static DateTime LastNotWorkStateTime;
        public static double? Speed = null;
        private static bool withoutSpeedIndicator;

        #region Public Static Methods

        public static bool WithoutSpeedIndicator
        {
            get { return withoutSpeedIndicator; }
            set { withoutSpeedIndicator = value; }
        }

        public static string GetControlCommand(ControlCommands controlCommand)
        {
            string textCommand = null;
            switch (controlCommand)
            {
                /*case ControlCommands.SetOZUSensitive:
                    textCommand = string.Format("88sen{0}99", Sensitive);
                    break;
                case ControlCommands.SetOZUStopBeltTrue:
                    textCommand = "88bel199";
                    break;
                case ControlCommands.SetOZUStopBeltFalse:
                    textCommand = "88bel099";
                    break;*/
            }
            if (textCommand == null)
                throw new CommandNotFoundException();
            return textCommand;
        }

        public static string GetViewCommand(ViewCommands viewCommand)
        {
            string textCommand = null;
            switch (viewCommand)
            {
                case ViewCommands.GetOZUStatus:
                    {
                        if (withoutSpeedIndicator == true)
                        {
                            break;
                        }
                        else
                        {
                            textCommand = "88stat99";
                            break;
                        }
                    }
                case ViewCommands.GetOZUSpeed:
                    {
                        if (withoutSpeedIndicator == true)
                        {
                            textCommand = "#061\r";
                        }
                        else
                        {
                            textCommand = "88sped99";
                        }
                        break;
                    }
            }
            if (textCommand == null)
                throw new CommandNotFoundException();
            return textCommand;
        }

        #endregion

        public static void SkipStatus()
        {
            Status = 0;
            //Speed = null;
        }

        public static void DeleteSpeed()
		{
            Speed = 0;
		}

        public static void RefreshStatus(string answer)
        {
            Status = 0;
            if (withoutSpeedIndicator == false)
            {
                if (answer != "" && answer.Substring(0, 3) == "sts")
                {
                    Status = Int64.Parse(answer.Substring(3, 10));
                    if ((Status & 16) == 16 || (Status & 512) == 512)
                    {
                        WorkState = false;
                        LastNotWorkStateTime = DateTime.Now;
                    }
                    if ((Status & 32) == 32 || (Status & 1024) == 1024)
                    {
                        WorkState = true;
                    }
                }
                IsWorking = true;
            }
			else
			{
			    WorkState = true;
                IsWorking = true;
			}
            
            //MessageBox.Show(answer.Substring(0, 1) + " - " + answer.Substring(1, 1) + " - " + answer.Substring(2, 1) + " - " + answer.Substring(3, 1));
        }

        public static void RefreshSpeed(string answer)
        {
            if (withoutSpeedIndicator == true)
            {
                if (answer != "")
                {
                    Speed = (double)Convert.ToInt32(answer.Substring(7, 2), 16) * 0.027617;
                }
            }
            else
            {
                if (answer != "" && answer.Substring(0, 3) == "spd")
                {
                    Speed = double.Parse(answer.Substring(3, 3)) / 100;
                }
            }
            //            MessageBox.Show(answer.Substring(0, 1) + " - " + answer.Substring(1, 1) + " - " + answer.Substring(2, 1) + " - " + answer.Substring(3, 1));
        }

        public static string GetError(int index)
        {
            string error = "";
            if (withoutSpeedIndicator == false)
            {
                switch (index)
                {
                    case 0:
                        error = "Неисправен входной датчик №1";
                        break;
                    case 1:
                        error = "Неисправен входной датчик №2";
                        break;
                    case 2:
                        error = "Неисправен выходной датчик №1";
                        break;
                    case 3:
                        error = "Неисправен выходной датчик №2";
                        break;
                    case 4:
                        error = "Включился верхний механический концевик";
                        break;
                    case 5:
                        error = "Включился нижний механический концевик";
                        break;
                    case 6:
                        error = "Включилось защитное термореле";
                        break;
                    case 7:
                        error = "Включился датчик входной №1";
                        break;
                    case 8:
                        error = "Включился датчик выходной №1";
                        break;
                    case 9:
                        error = "Сработал датчик положения верхний";
                        break;
                    case 10:
                        error = "Сработал датчик положения нижний";
                        break;
                    case 11:
                        error = "Включился датчик входной №2";
                        break;
                    case 12:
                        error = "Включился датчик выходной №2";
                        break;
                    case 13:
                        error = "Ручной режим ЗУ";
                        break;
                    case 14:
                        error = "Переход в автоматический режим ЗУ";
                        break;
                    case 15:
                        error = "Подъем не включился";
                        break;
                    case 16:
                        error = "Опускание своевременно не выключилось";
                        break;
                    case 17:
                        error = "Подъем своевременно не выключился";
                        break;
                    case 18:
                        error = "Опускание не включилось";
                        break;
                }
                if (error != "")
                {
                    StreamWriter file = File.AppendText(Application.StartupPath + "\\Logs\\ozu_log_" + DateTime.Now.ToShortDateString() + ".txt");
                    file.WriteLine(DateTime.Now.ToShortDateString() + "-" + DateTime.Now.ToLongTimeString() + " --> " + error);
                    file.Close();
                }
            }
            return error;
        }
    }
}
