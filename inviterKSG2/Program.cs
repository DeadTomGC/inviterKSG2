using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SteamKit2;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Net;
using System.Diagnostics;

namespace inviterKSG2
{
    class Program
    {
        static public System.Net.Sockets.ProtocolType type = System.Net.Sockets.ProtocolType.Tcp;
        static public SteamKit2.SteamClient client = new SteamClient(type);
        static public SteamUser steamUser;
        static public SteamFriends steamFriends;
        static public Stopwatch stw = new Stopwatch();
        static public bool discon = false;
        static public bool recon = false;
        static public bool isRunning = true;
        static public StreamWriter sw = null;
        static public bool ready = false;
        static public Thread main = new Thread(Program.run);
        static Thread checker = null;
        static Thread keepOnline = null;

        static void Main(string[] args)
        {
            
            main.Start();

        }



        public static void run()
        {
            ready = false;
            if (sw == null)
            {
                sw = new StreamWriter("log.txt");
            }
            Dictionary<UInt64, String> ips = new Dictionary<UInt64, String>();
            Dictionary<SteamID, int> black = new Dictionary<SteamID, int>();
            Dictionary<SteamID, int> logs = new Dictionary<SteamID, int>();
            Args databackup;
            
            //SteamID[] keystmp;
            try
            {
                Console.WriteLine("*yawn* time to get to work...");
                Console.WriteLine("VERSION 1.7.1 (Now with enough threads for the program and you cat!)");                 //                      <<<<<<<<----------------Version statement---------------->>>>>>>>
                sw.WriteLine("*yawn* time to get to work...");
                sw.Flush();


                Console.WriteLine("Connecting...");
                sw.WriteLine("Connecting...");
                sw.Flush();
                ///////////////////////////////////////////////////////////     <<<<<<<<----------------Gather info---------------->>>>>>>> 
                string user = "deadtomgchost", pass = "pass";
                
                //////////////////////////////////////////////////////////////////

                try
                {
                    StreamReader deets = new StreamReader("logindeets.txt");
                    user = deets.ReadLine().Trim();
                    pass = deets.ReadLine().Trim();
                    deets.Close();
                }
                catch (Exception e)
                {
                    StreamWriter deets = new StreamWriter("logindeets.txt");
                    Console.WriteLine("Failed to load login details.");
                    Console.WriteLine("Enter username and password");
                    Console.Write("Username: ");
                    user = Console.ReadLine().Trim();
                    Console.Write("Password: ");
                    pass = Console.ReadLine().Trim();
                    deets.WriteLine(user);
                    deets.WriteLine(pass);
                    deets.Close();
                }


                ////////////////////////////////////////////////////////////////
                StreamReader sr = new StreamReader("ip.txt");
                String dbhost = sr.ReadLine().Trim();
                String database = sr.ReadLine().Trim();
                String dbuser = sr.ReadLine().Trim();
                String dbpass = sr.ReadLine().Trim();
                sr.Close();
                sr = null;

                if (checker == null)
                {
                    checker = new Thread(Checker.run);
                    checker.Start(new Args(dbhost, database, dbuser, dbpass));
                }                                                           //     <<<<<<<<----------------Start SQL thread---------------->>>>>>>> 


               
                

                ///////////////////////////////////////////////              <<<<<<<<---------------- Connect for the first time---------------->>>>>>>>
                client.Connect();
                
                SteamID admin = new SteamID(76561197990973056);
                

                steamUser = client.GetHandler<SteamUser>();
                steamFriends = client.GetHandler<SteamFriends>();
                discon = false;
                

                stw.Reset();
                if (keepOnline == null)
                {
                    keepOnline = new Thread(KeepOnline.run);
                    keepOnline.Start();
                }
                ready = true;
                /////////////////////////////////////////////////////////###############################################################################################///////////////////////////////
                while (isRunning)
                {
                    // wait for a callback to be posted

                    
                        
                        var callback = client.WaitForCallback(true);

                        
                        // handle the callback
                        // the Handle function will only call the passed in handler
                        // if the callback type matches the generic type
                        callback.Handle<SteamClient.ConnectedCallback>(c =>
                        {
                            if (c.Result != EResult.OK)
                            {
                                Console.WriteLine("Unable to connect to Steam: {0}", c.Result);
                                sw.WriteLine("Unable to connect to Steam: {0}", c.Result);
                                sw.Flush();

                                //isRunning = false;
                                return;
                            }

                            Console.WriteLine("Connected to Steam! Logging in '{0}'...", user);
                            sw.WriteLine("Connected to Steam! Logging in '{0}'...", user);
                            sw.Flush();
                            discon = false;
                            steamUser.LogOn(new SteamUser.LogOnDetails
                            {
                                Username = user,
                                Password = pass,
                            });
                        });

                        callback.Handle<SteamClient.DisconnectedCallback>(c =>
                        {
                            Console.WriteLine("Disconnected from Steam");
                            sw.WriteLine("Disconnected from Steam");
                            sw.Flush();

                            discon = true;
                            if (Checker.end)
                                isRunning = false;
                        });

                        callback.Handle<SteamUser.LoggedOnCallback>(c =>
                        {
                            if (c.Result != EResult.OK)
                            {
                                Console.WriteLine("Unable to logon to Steam: {0} / {1}", c.Result, c.ExtendedResult);
                                sw.WriteLine("Unable to logon to Steam: {0} / {1}", c.Result, c.ExtendedResult);
                                sw.Flush();
                                discon = true;
                                return;
                            }
                            else
                            {

                                Console.WriteLine("Successfully logged on!");
                                sw.WriteLine("Successfully logged on!");
                                sw.Flush();


                                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                using (WebClient reader = new WebClient())
                                {
                                    string s = reader.DownloadString("http://steamcommunity.com/groups/ksadmins/memberslistxml/?xml=1");
                                }
                            }
                            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


                        });
                        //####################################################################
                        callback.Handle<SteamUser.AccountInfoCallback>(c =>
                        {
                            steamFriends.SetPersonaState(EPersonaState.Online);
                        });

                        //######################################################################

                        callback.Handle<SteamFriends.FriendsListCallback>(c =>
                        {
                            int friendCount = steamFriends.GetFriendCount();

                            Console.WriteLine("We have {0} friends", friendCount);
                            sw.WriteLine("We have {0} friends", friendCount);
                            sw.Flush();
                            for (int x = 0; x < friendCount; x++)
                            {
                                // steamids identify objects that exist on the steam network, such as friends, as an example
                                SteamID steamIdFriend = steamFriends.GetFriendByIndex(x);

                                // we'll just display the STEAM_ rendered version
                                Console.WriteLine("Friend: {0}", steamIdFriend.Render());
                                sw.WriteLine("Friend: {0}", steamIdFriend.Render());
                                sw.Flush();
                            }

                            // we can also iterate over our friendslist to accept or decline any pending invites

                            foreach (var friend in c.FriendList)
                            {
                                if (friend.Relationship == EFriendRelationship.RequestRecipient)
                                {
                                    // this user has added us, NOPE!!
                                    steamFriends.RemoveFriend(friend.SteamID);
                                }
                            }
                            for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                            {
                                if (ips.ContainsKey(steamFriends.GetFriendByIndex(i)) == false)
                                {
                                    ips.Add(steamFriends.GetFriendByIndex(i), "0 null 0");
                                }

                            }
                            Checker.friends = steamFriends;
                            Checker.client = client;

                        });

                        callback.Handle<SteamFriends.FriendMsgCallback>(c =>            //  <<<<<<<<----------------LEAST IMPORTANT CALLBACK---------------->>>>>>>> 
                        {
                            Console.WriteLine("recieved: " + c.Message);
                        });


                        //###############################################################################################
                        callback.Handle<SteamUser.LoggedOffCallback>(c =>
                        {
                            Console.WriteLine("Logged off of Steam: {0}", c.Result);
                            sw.WriteLine("Logged off of Steam: {0}", c.Result);

                            sw.Flush();
                            discon = true;
                            if (Checker.end)
                                isRunning = false;
                        });

                        
                    

                    

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                sw.WriteLine(e);
                sw.Flush();

            }

            
        }
    }



    class KeepOnline
    {
        public static bool restarting = false;
        public static void run()
        {
            while (Program.isRunning)
            {
                if (((Program.discon || Program.steamFriends.GetPersonaState() == EPersonaState.Offline) && Program.isRunning) || Program.recon)
                {
                    Console.WriteLine("Currently Offline.");
                    Program.sw.WriteLine("Currently Offline.");
                    Program.sw.Flush();
                    if (Program.stw.IsRunning)
                    {
                        if (Program.stw.ElapsedMilliseconds >= 30000)
                        {
                            restarting = true;
                            Console.WriteLine("restart desired");
                            Program.sw.WriteLine("restart desired");
                            Program.sw.Flush();
                            Program.recon = false;
                            while (Program.ready == false)
                            {
                                Thread.Sleep(1000);
                            }
                            Console.WriteLine("Attempting restart.");
                            Program.sw.WriteLine("Attempting restart.");
                            Program.sw.Flush();
                            Program.main.Abort();
                            Thread.Sleep(1000);
                            Program.client = new SteamClient(Program.type);
                            Program.main = new Thread(Program.run);
                            Program.main.Start();
                            Program.stw.Stop();
                            Program.stw.Reset();
                            restarting = false;
                        }
                    }
                    else
                    {
                        Program.stw.Start();
                    }
                }
                else
                {
                    Program.stw.Stop();
                    Program.stw.Reset();
                }
                Thread.Sleep(1000);
            }

        }

    }

}