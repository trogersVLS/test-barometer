using System;
using VLS;
using System.Text.RegularExpressions;
namespace test_barometer
{
    class Program
    {
        static void Main(string[] args)
        {   
            int Samples, PChannel, TChannel;
            if(args.Length != 0){
                (Samples, PChannel, TChannel) = GetArgs(args);
            }
            else{
                (Samples, PChannel, TChannel) = (100, 135, 136);
            }


            // Start new program
            Console.WriteLine("Control Board Barometer Test");
            Console.WriteLine("Press Ctrl+C to exit");

            while(true){
                TestBarometer(PChannel, TChannel, Samples);
                
            }            
        }
        public static (int, int, int) GetArgs(string[] args){
            int Samples, PChannel, TChannel;
            (Samples, PChannel, TChannel) = (100, 135, 136); //Default values

            
            foreach(string s in args){
                if(s == "-s"){
                    Samples = int.Parse(args[Array.IndexOf(args,s) + 1]);
                }
                else if(s == "-p"){
                    PChannel = int.Parse(args[Array.IndexOf(args,s) + 1]);

                }
                else if(s == "-t"){
                    TChannel = int.Parse(args[Array.IndexOf(args,s) + 1]);
                }
                else{

                }
            }
            

            return (Samples, PChannel, TChannel);
        }
        public static void TestBarometer(int PChannel, int TChannel, int Samples){
                //Get Ip Address
                Console.Write("Enter IP Address: ");
                string IpAddress = Console.ReadLine();
                //Get barometer values
                VLS_Tlm vent = new VLS_Tlm(IpAddress);
                vent.CMD_Connect();
                

                //Retrieve the pressure sensor values
                vent.CMD_Write(string.Format("set vcm telemetry {0} 0 0 0 0 0 0 0", PChannel));

                var response = vent.CMD_Write("get vcm telemetry " + Samples.ToString());
                //Console.WriteLine(response);
                var PressMatches = Regex.Matches(response, @"(?'channel'(?<=vcm\:)\s+" + PChannel.ToString() + @",)(?'counts'(\s+\d+|\s+-\d+))");
                
                
                double avePress = 0;
                int count = 0;
                foreach(Match m in PressMatches)
                {
                    avePress += double.Parse(m.Groups[3].Value);
                    count++;
                }
                avePress = (avePress / count) * 0.000980665;

                //Next get temperature
                vent.CMD_Write(string.Format("set vcm telemetry {0} 0 0 0 0 0 0 0", TChannel));
                response = vent.CMD_Write("get vcm telemetry " + Samples.ToString());

                var TempMatches = Regex.Matches(response, @"(?'channel'(?<=vcm\:)\s+" + TChannel.ToString() + @",)(?'counts'(\s+\d+|\s+-\d+))");
                double aveTemp = 0;
                count = 0;
                foreach(Match m in TempMatches)
                {
                    aveTemp += double.Parse(m.Groups[3].Value);
                    count++;
                }
                aveTemp = (aveTemp / count) / 100;


                Console.WriteLine(string.Format("Ambient Pres = {0}", avePress));
                Console.WriteLine(string.Format("Ambient Temp = {0}", aveTemp));


                
                
                //Disconnect
                // vent.CMD_Write("exit");
                vent.Disconnect();
                System.Threading.Thread.Sleep(1000);

        }
       
    }
}
