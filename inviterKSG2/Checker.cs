using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.Threading;
using SteamKit2;

namespace admnNotify
{
    class Checker
    {
        public static String host;
        public static String database;
        public static String user;
        public static String pass;
        public static bool end = false,task = false,test=false;
        public static int WIDTH = 4;
        public static linkAR temp = new linkAR(WIDTH);
        public static SteamFriends friends;
        public static Args dataconv;
        public static Dictionary<SteamID, int> logs = new Dictionary<SteamID, int>(); 
        public static SteamID[] keystmp;
        public static bool discon = false;

        public static void run(object data)
        {
            end = false;
            task = false;
            if (temp.getLength() != 0)
            {
                temp.setFirst();
                if (temp.getCurrent() != null)
                {
                    //TODO: send messages
                    for (int i = 0; i < friends.GetFriendCount(); i++)
                    {
                        if (friends.GetFriendPersonaState(friends.GetFriendByIndex(i)) != EPersonaState.Offline && friends.GetFriendPersonaState(friends.GetFriendByIndex(i)) != EPersonaState.Snooze)
                            friends.SendChatMessage(friends.GetFriendByIndex(i), EChatEntryType.ChatMsg, temp.getCurrent()[0] + " has requested an admin on " + temp.getCurrent()[3] + ".  Message: " + temp.getCurrent()[2] + System.Environment.NewLine + "To respond, type: /accept");
                    }
                    //
                    task = true;

                }

            }
            bool running = true;
            try{

            
                dataconv = (Args)data;
                host = dataconv.host;
                database = dataconv.database;
                user = dataconv.user;
                pass = dataconv.pass;
                bool first = true;
                MySqlConnection conn = null;

                Console.WriteLine("got to loop");
                logThem("got to loop");
                MySqlCommand cmd = new MySqlCommand();
                int count = 0;
                while (running)
                {
                    Thread.Sleep(15000);
                    try
                    {

                        if (first)
                        {
                            conn = new MySqlConnection("host=" + host + ";database=" + database + ";username=" + user + ";password=" + pass + ";");
                            Console.WriteLine("connected");
                            logThem("connected");

                            first = false;
                        }
                        cmd.Connection = conn;
                        conn.Open();
                        Console.WriteLine("conn opened");
                        logThem("conn opened");
                        if (test)
                        {
                            test = false;
                            try
                            {
                                cmd.CommandText = "INSERT INTO reports (player,serverip,message,servername) VALUES('reportBot','this is not a legit ip','testing reportBot','not actually on a real server')";
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                logThem(e.ToString());
                            }
                            Thread.Sleep(1000);
                            
                        }

                        cmd.CommandText = "SELECT player, serverip, message, servername FROM reports";
                        MySqlDataReader rdr = cmd.ExecuteReader();
                        Console.WriteLine("reader created");
                        logThem("reader created");
                        while (rdr.Read())
                        {

                            temp.AddEnd(); //also sets current to last
                            Console.WriteLine("IN loop, created new end element");
                            logThem("IN loop, created new end element");
                            temp.setLast();

                            temp.getCurrent()[0] = (String)rdr[0];
                            temp.getCurrent()[1] = (String)rdr[1];
                            temp.getCurrent()[2] = (String)rdr[2];
                            if (((String)rdr[3]).Contains("kill-streak.net |"))
                            {
                                temp.getCurrent()[3] = ((String)rdr[3]).Substring(((String)rdr[3]).IndexOf("kill-streak.net |") + 17);
                            }
                            else
                            {
                                temp.getCurrent()[3] = (String)rdr[3];

                            }
                            Console.WriteLine(temp.getCurrent()[0] + "  " + temp.getCurrent()[1] + "  " + temp.getCurrent()[2] + "  " + temp.getCurrent()[3]);
                            logThem(temp.getCurrent()[0] + "  " + temp.getCurrent()[1] + "  " + temp.getCurrent()[2] + "  " + temp.getCurrent()[3]);

                        }
                        rdr.Close();
                        cmd.CommandText = "TRUNCATE reports";
                        cmd.ExecuteNonQuery();
                        conn.Close();
                        temp.setFirst();
                        if (temp.getCurrent() != null)
                        {
                            
                            //TODO: send messages
                            for (int i = 0; i < friends.GetFriendCount(); i++)
                            {
                                if (friends.GetFriendPersonaState(friends.GetFriendByIndex(i)) != EPersonaState.Offline && friends.GetFriendPersonaState(friends.GetFriendByIndex(i)) != EPersonaState.Snooze)
                                    friends.SendChatMessage(friends.GetFriendByIndex(i), EChatEntryType.ChatMsg, temp.getCurrent()[0] + " has requested an admin on " + temp.getCurrent()[3] + ".  Message: " + temp.getCurrent()[2] + System.Environment.NewLine + "To respond, type: /accept");
                            }
                            //
                            task = true;

                        }


                    }
                    catch (System.Threading.ThreadAbortException e)
                    {
                        Console.WriteLine("Thread Aborted");

                    }
                    catch (Exception e)
                    {
                        try
                        {
                            conn.Close();
                        }
                        catch (Exception exc) { };

                        Console.WriteLine("There was an error with the mySQL connection: " + e);
                        logThem("There was an error with the mySQL connection: " + e);
                        discon = true;
                        Console.WriteLine("Reconnecting...");
                        while (discon)
                        {
                            if (end) { throw new InvalidOperationException(); }
                            try
                            {

                                conn = new MySqlConnection("host=" + host + ";database=" + database + ";username=" + user + ";password=" + pass + ";");
                                conn.Open();
                                conn.Close();
                                discon = false;
                            }
                            catch (Exception ex)
                            {
                                discon = true;
                            }
                            Thread.Sleep(1000);
                            Console.WriteLine("Reconnecting...");
                            logThem("Reconnecting...");
                        }

                    }
                    if (end) { running = false; throw new InvalidOperationException(); }
                    
                    
                }

                Console.WriteLine("outside loop");
            }
            catch (Exception e) { Console.WriteLine(e); }


        }
        public static void logThem(string msg)
        {
            try
            {
                keystmp = logs.Keys.ToArray();
                for (int i = 0; i < keystmp.Length; i++)
                {
                    friends.SendChatMessage(keystmp[i], EChatEntryType.ChatMsg, msg);
                }
            }
            catch (Exception e) { };
        }
    }
    class Args
    {
        public String host;
        public String database;
        public String user;
        public String pass;


        public Args(String dbhost,
             String database,
             String dbuser,
             String dbpass)
        {
            host = dbhost;
            this.database = database;
            user = dbuser;
            pass = dbpass;

        }

    }
}
