using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.Threading;
using SteamKit2;
using System.Net;
using System.IO;

namespace inviterKSG2
{
    class Checker
    {
        public static String host;
        public static String database;
        public static String user;
        public static String pass;
        public static SteamFriends friends;
        public static SteamClient client;
        public static Args dataconv;
        public static bool discon = false,end = false;
        public static long lastpID = 0;
        public static StreamWriter lastIDW;
        public static StreamReader lastIDR;

        public static void run(object data)
        {
            bool running = true;
            try{

                try
                {
                    lastIDR = new StreamReader("id.txt");
                    lastpID = Convert.ToInt64(lastIDR.ReadLine().Trim());
                    lastIDR.Close();
                }
                catch (Exception e)
                {
                    lastpID = 0;
                }



                dataconv = (Args)data;
                host = dataconv.host;
                database = dataconv.database;
                user = dataconv.user;
                pass = dataconv.pass;
                bool first = true;
                MySqlConnection conn = null;

                Console.WriteLine("got to loop");
                Console.Error.WriteLine("got to loop");
                MySqlCommand cmd = new MySqlCommand();
                //int count = 0;
                while (running)
                {
                    Thread.Sleep(15000);
                    try
                    {

                        if (first)
                        {
                            conn = new MySqlConnection("host=" + host + ";database=" + database + ";username=" + user + ";password=" + pass + ";");
                            Console.WriteLine("connected to DB");
                            Console.Error.WriteLine("connected to DB");

                            first = false;
                        }
                        cmd.Connection = conn;
                        conn.Open();
                        Console.WriteLine("conn opened");
                        Console.Error.WriteLine("conn opened");

                        cmd.CommandText = "SELECT playerId,uniqueId FROM hlstats_PlayerUniqueIds WHERE uniqueId>'"+lastpID+"'";
                        MySqlDataReader rdr = cmd.ExecuteReader();
                        Console.WriteLine("reader created");
                        Console.Error.WriteLine("reader created");
                        long length = rdr.Depth;
                        while (rdr.Read())
                        {
                            lastpID = (long)rdr[1];
                            SteamID newfriend = new SteamID((ulong)rdr[0]);
                            friends.AddFriend(newfriend);
                            lastIDW = new StreamWriter("id.txt");
                            lastIDW.WriteLine(lastpID);
                            lastIDW.Close();
                            Thread.Sleep(15000);
                        }
                        rdr.Close();


                    }
                    catch (System.Threading.ThreadAbortException e)
                    {
                        Console.WriteLine("Thread Aborted");
                        Console.Error.WriteLine("Thread Aborted");
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            conn.Close();
                        }
                        catch (Exception exc) { };

                        Console.WriteLine("There was an error with the mySQL connection: " + e);
                        Console.Error.WriteLine("There was an error with the mySQL connection: " + e);
                        discon = true;
                        Console.WriteLine("Reconnecting...");
                        Console.Error.WriteLine("Reconnecting...");
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
                            Console.Error.WriteLine("Reconnecting...");
                        }

                    }
                    if (end) { running = false; throw new InvalidOperationException(); }
                    
                    
                }

                Console.WriteLine("outside loop");
                Console.Error.WriteLine("outside loop");
            }
            catch (Exception e) { Console.WriteLine(e);
            Console.Error.WriteLine(e);
            }


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
