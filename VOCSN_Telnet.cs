/* VOCSN_Telnet.cs
 * 
 * C# class for communication with VOCSN device
 * 
 * Author: Taylor Rogers
 * Date: 1/29/2020
 */

using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace VLS
{
    enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    enum Options
    {
        SGA = 3
    }
    public class VLS_Tlm
    {
        readonly int cmd_port = 5000;
        readonly int qnx_port = 23;
        readonly int tlm_port = 5001;

        TcpClient cmd_shell;
        TcpClient qnx_shell;
        TcpClient tlm_shell;

        public bool Connected = false;

        int TimeOutMs = 50;

        public string _ip_address;

        public Dictionary<string, int> TLMChannels = new Dictionary<string, int>();


        public VLS_Tlm(string _ip_address)
        {
            this._ip_address = _ip_address;
            this.Connected = false;
        }

        public VLS_Tlm()
        {
        }

        /********************************************************************************************************************************
         * Public Connect Methods
         * 
         * 
         * ******************************************************************************************************************************/
        /********************************************************************************************************************************
         * Connect() - Connects the object to the QNX shell on port 23, the CMD shell on port 5000 and the telemetry stream on port 5001
         * 
         * Returns: bool success - Returns true if all three were successful
         *                         Returns false if any of the three were unsuccessful
         * 
         * ******************************************************************************************************************************/
        public bool Connect(string ip = null, string initialcmd = null, bool getTLM = true)
        {
            bool success;
            bool cmd_connect;
            bool qnx_connect;
            bool tlm_connect;

            if(ip != null)
            {
                this._ip_address = ip;
            }
            try
            {

                cmd_connect = this.CMD_Connect();
                tlm_connect = this.TLM_Connect();
                qnx_connect = this.QNX_Connect();
                
                if(cmd_connect && tlm_connect && qnx_connect)
                {
                    success = true;
                    this.Connected = true;
                    if(initialcmd != null)
                    {
                        this.CMD_Write(initialcmd);
                    }
                }
                else
                {
                    success = false;
                    this.Connected = false;
                }

            }
            catch
            {
                success = false;
                this.Connected = false;
            }

            //This will obtain the telemetry table in a dictionary.
            if (this.Connected & getTLM)
            {
                
                if (!this.GetTlmTable()) {
                    success = false;
                }
            }
            return success;
        }


        //Gets the TLM table from the device. Each new version of software
        public bool GetTlmTable()
        {
            bool success=false;
            string table;

            if (this.Connected)
            {
                table = CMD_Write("get vcm table telemetry", "$vserver> "); //This command takes a long time
                table = table.Replace("\r", "");
                
                //Parse the output and create the dictionary
                string[] tableArray = table.Split('\n');

                for (int i = 0; i < tableArray.Length - 1; i++)
                {
                    int channelNum = int.Parse(tableArray[i].Substring(1, 3)); ;
                    string channelName = tableArray[i].Substring(5);


                    if (!this.TLMChannels.ContainsKey(channelName))
                    {
                        this.TLMChannels.Add(channelName, channelNum);
                    }
                    


                }
                success = true;

            }
            else
            {
                success = false;
            }

            return success;
        }

        
        /********************************************************************************************************************************
         * CMD_Connect() - Connects the object to the CMD shell on port 5000 
         * 
         * Returns: bool success - Returns true if connection was successful
         *                         Returns false if connection was unsuccessful
         * 
         * ******************************************************************************************************************************/
        public bool CMD_Connect()
        {
            bool success;
            try
            {
                this.cmd_shell = new TcpClient();

                var result = this.cmd_shell.BeginConnect(this._ip_address, this.cmd_port, null, null);
                var connect = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(0.5));


                if (connect)
                {
                    this.CMD_Login();
                    success = true;
                }
                else
                {
                    success = false;
                    //this.Disconnect();
                }


            }
            catch
            {
                success = false;

            }
            return success;
        }

        public bool QNX_Connect()
        {
            bool success;
            try
            {

                this.qnx_shell = new TcpClient();


                var result = qnx_shell.BeginConnect(this._ip_address, this.qnx_port, null, null);
                var connect = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(0.5));

                if (connect)
                {
                    this.QNX_Login("root", "", 500);
                    success = true;

                }
                else
                {
                    success = false;
                    //this.Disconnect();
                }

            }
            catch
            {
                success = false;

            }
            return success;
        }
        public bool TLM_Connect()
        {
            bool success;
            try
            {

                this.tlm_shell = new TcpClient();


                var result = tlm_shell.BeginConnect(this._ip_address, this.tlm_port, null, null);
                var connect = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(0.5));

                if (connect)
                {
                    Thread.Sleep(500);
                    this.TLM_Login();
                    success = true;

                }
                else
                {
                    success = false;
                    //this.Disconnect();
                }

            }
            catch
            {
                success = false;

            }
            return success;
        }
        public void Disconnect()
        {
            try
            {
                //this.qnx_shell.Close();
                this.cmd_shell.Close();
                //this.tlm_shell.Close();
            }
            catch
            {

            }
        }

        /********************************************************************************************************************************
         * CMD Shell Methods
         * 
         * 
         * ******************************************************************************************************************************/
        public bool CMD_Login()
        {

            string s = CMD_Read();
            s += CMD_Read();
            if (!s.TrimEnd().EndsWith("$vserver>"))
                throw new Exception("Failed to connect");
            else
            {
                this.Connected = true;
            }

            return this.cmd_shell.Connected;
        }


        /* Writes a command to the cmd port and returns the response
         * 
         */
        public string CMD_Write(string cmd)
        {
            string output = "";
            if (this.cmd_shell.Connected)
            {
                byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
                cmd_shell.GetStream().Write(buf, 0, buf.Length);
                var cnt = 0;
                while ((!output.EndsWith("$vserver> ")))
                {   
                    output += this.CMD_Read();
                    
                    cnt++;
                }
                
            }
            return output;
        }
        /* Writes a command and reads until the specified end string is present, then returns the output.
         * 
         */

        public string CMD_Write(string cmd, string end)
        {
            string output = "";
            if (this.cmd_shell.Connected)
            {
                byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
                cmd_shell.GetStream().Write(buf, 0, buf.Length);
                int cnt = 0;
                while (!(cmd_shell.Available > 0) && (cnt<TimeOutMs))
                {
                    Thread.Sleep(1);
                    cnt++;
                }
                while (!output.EndsWith(end))
                {
                    output += this.CMD_Read();
                }
                
            }
            return output;
        }

        private string CMD_Read()
        {
            if (!cmd_shell.Connected) return null;
            StringBuilder sb = new StringBuilder();
            do
            {
                CMD_ParseTelnet(sb);
                System.Threading.Thread.Sleep(TimeOutMs);
            } while (cmd_shell.Available > 0);
            string str = sb.ToString();
            return str;
        }

        public bool CMD_IsConnected()
        {
            return cmd_shell.Connected;
        }

        void CMD_ParseTelnet(StringBuilder sb)
        {
            while (cmd_shell.Available > 0)
            {
                int input = cmd_shell.GetStream().ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        int inputverb = cmd_shell.GetStream().ReadByte();
                        if (inputverb == -1) break;
                        switch (inputverb)
                        {
                            case (int)Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int)Verbs.DO:
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = cmd_shell.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                cmd_shell.GetStream().WriteByte((byte)Verbs.IAC);
                                if (inputoption == (int)Options.SGA)
                                    cmd_shell.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                else
                                    cmd_shell.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                cmd_shell.GetStream().WriteByte((byte)inputoption);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        sb.Append((char)input);
                        break;
                }
            }
        }
        /********************************************************************************************************************************
         * TLM Shell Methods
         * 
         * 
         * ******************************************************************************************************************************/
        public bool TLM_Login()
        {

            string s = TLM_Read();
            s += TLM_Read();
            if (!s.TrimEnd().EndsWith("$status>"))
                throw new Exception("Failed to connect");
            else
            {
                this.Connected = true;
            }

            return this.tlm_shell.Connected;
        }



        public string TLM_Write(string cmd)
        {
            string output = "";
            if (this.Connected)
            {
                byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
                tlm_shell.GetStream().Write(buf, 0, buf.Length);

                output = this.TLM_Read();
            }
            return output;
        }


        //TODO: Update this method to read only a set number of lines --> This method will read continuously as it is now if the stream command is specified with "-1" samples
        //TODO: TODO: Create an event handler that will store all TLM data in buffer to be read

            //NOTE: DO NOT USE COMMAND WITHOUT FIXING PROBLEMS FIRST --> Instead, use CMD_Write("get vcm telemetry 'n'")
        public string TLM_Read()
        {
            if (!tlm_shell.Connected) return null;
            StringBuilder sb = new StringBuilder();
            do
            {
                TLM_ParseTelnet(sb);
                System.Threading.Thread.Sleep(TimeOutMs);
            } while (tlm_shell.Available > 0);
            string str = sb.ToString();
            return str;
        }

        public bool TLM_IsConnected()
        {
            return tlm_shell.Connected;
        }

        void TLM_ParseTelnet(StringBuilder sb)
        {
            while (tlm_shell.Available > 0)
            {
                int input = tlm_shell.GetStream().ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        int inputverb = tlm_shell.GetStream().ReadByte();
                        if (inputverb == -1) break;
                        switch (inputverb)
                        {
                            case (int)Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int)Verbs.DO:
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = tlm_shell.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                tlm_shell.GetStream().WriteByte((byte)Verbs.IAC);
                                if (inputoption == (int)Options.SGA)
                                    tlm_shell.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                else
                                    tlm_shell.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                tlm_shell.GetStream().WriteByte((byte)inputoption);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        sb.Append((char)input);
                        break;
                }
            }
        }

        /********************************************************************************************************************************
         * QNX Shell Methods
         * 
         * 
         * ******************************************************************************************************************************/
        public string QNX_Login(string Username, string Password, int LoginTimeOutMs)
        {
            int oldTimeOutMs = TimeOutMs;
            TimeOutMs = LoginTimeOutMs;
            string s = QNX_Read();
            s += QNX_Read();
            if (!s.TrimEnd().EndsWith(":"))
                throw new Exception("Failed to connect : no login prompt");
            s = QNX_Write(Username);

            
            if (!s.TrimEnd().EndsWith("#"))
                throw new Exception("Failed to connect : no password prompt");
            else
            {
                this.Connected = true;
            }


            s += QNX_Read();
            TimeOutMs = oldTimeOutMs;
            return s;
        }

        public string QNX_WriteLine(string cmd)
        {
            var response = QNX_Write(cmd + "\n");
            return response;
        }

         
        public string QNX_Write(string cmd)
        {
            string output = "";
            cmd += "\n";
            if (qnx_shell.Connected)
            {
                byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
                qnx_shell.GetStream().Write(buf, 0, buf.Length);
                var cnt = 0;
                while ((!output.EndsWith("# ")) && (cnt < 50))
                {
                    output += this.QNX_Read();

                    cnt++;
                }

            }
            return output;
        }

        public string QNX_Read()
        {
            if (!qnx_shell.Connected) return null;
            StringBuilder sb = new StringBuilder();
            do
            {
                QNX_ParseTelnet(sb);
                System.Threading.Thread.Sleep(TimeOutMs);
            } while (qnx_shell.Available > 0);
            string str = sb.ToString();
            return str;
        }

        public bool QNX_IsConnected()
        {
            return qnx_shell.Connected;
        }

        void QNX_ParseTelnet(StringBuilder sb)
        {
            while (qnx_shell.Available > 0)
            {
                int input = qnx_shell.GetStream().ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        int inputverb = qnx_shell.GetStream().ReadByte();
                        if (inputverb == -1) break;
                        switch (inputverb)
                        {
                            case (int)Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int)Verbs.DO:
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = qnx_shell.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                qnx_shell.GetStream().WriteByte((byte)Verbs.IAC);
                                if (inputoption == (int)Options.SGA)
                                    qnx_shell.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                else
                                    qnx_shell.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                qnx_shell.GetStream().WriteByte((byte)inputoption);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        sb.Append((char)input);
                        break;
                }
            }
        }



    }

}