using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using RKSManager.BLL.Devices.Enums.Commands;
using RKSManager.BLL.Devices.Exceptions;
using System.IO;
using System.Threading;

namespace RKSManager.BLL.Devices
{
    public static class SpeedIndicator
    {
        public static bool IsWorking = false;
        public static long Status = 0;
        public static bool WorkState = true;
        public static DateTime LastNotWorkStateTime;
        public static double? Speed = null;

    #region Public Static Methods

    public static string GetViewCommand(ViewCommands viewCommand)
        {
            string textCommand = null;
            switch (viewCommand)
            {
                case ViewCommands.GetSpeedIndicatorSpeed:
                    textCommand = "#061\r";
                    break;
            }
            if (textCommand == null)
                throw new CommandNotFoundException();
            return textCommand;
        }

        #endregion

        public static int HexToDec(string hex)
        {
            int dec = 0;
            for (int i = 0, j = hex.Length - 1; i < hex.Length; i++, j--)
            {
                if (hex[i] == 'A') { dec += 10 * (int)Math.Pow(16, j); }
                else if (hex[i] == 'B') { dec += 11 * (int)Math.Pow(16, j); }
                else if (hex[i] == 'C') { dec += 12 * (int)Math.Pow(16, j); }
                else if (hex[i] == 'D') { dec += 13 * (int)Math.Pow(16, j); }
                else if (hex[i] == 'E') { dec += 14 * (int)Math.Pow(16, j); }
                else if (hex[i] == 'F') { dec += 15 * (int)Math.Pow(16, j); }
                else { dec += (hex[i] - '0') * (int)Math.Pow(16, j); }

            }
            return dec;
        }



        public static void RefreshSpeed(string answer)
        {
            //         int num = 0;
            //         // устанавливаем метод обратного вызова
            //         TimerCallback tm = new TimerCallback(Count);
            //// создаем таймер
            //System.Threading.Timer timer = new System.Threading.Timer(tm, null, 0, 2000);

            //         if (answer != "")
            //         {
            //             Speed = double.Parse(answer.Substring(6, 2));

            //             double value = HexToDec(Convert.ToString(Speed));


            if (answer != "" )
            {
                Speed = Convert.ToDouble(HexToDec(answer.Substring(6, 2)));
            }

        }
    }
}
