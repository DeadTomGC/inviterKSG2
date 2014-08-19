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
           
            Dictionary<UInt64, String> ips = new Dictionary<UInt64, String>();
            Dictionary<SteamID, int> black = new Dictionary<SteamID, int>();
            Dictionary<SteamID, int> logs = new Dictionary<SteamID, int>();
            Args databackup;
            
            //SteamID[] keystmp;
            try
            {
                Console.WriteLine("*yawn* time to get to work...");
                Console.Error.WriteLine("*yawn* time to get to work...");
                Console.WriteLine("VERSION 1");                 //                      <<<<<<<<----------------Version statement---------------->>>>>>>>
                Console.Error.WriteLine("VERSION 1");  
                Console.WriteLine("Connecting...");
                Console.Error.WriteLine("Connecting...");
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
                    Console.Error.WriteLine("Failed to load login details.");
                    Console.WriteLine("Enter username and password");
                    Console.Error.WriteLine("Enter username and password");
                    Console.Write("Username: ");
                    Console.Error.Write("Username: ");
                    user = Console.ReadLine().Trim();
                    Console.Write("Password: ");
                    Console.Error.Write("Password: ");
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
                String greet = sr.ReadLine().Trim();
                SteamID admin = new SteamID(System.UInt64.Parse(sr.ReadLine().Trim()));
                sr.Close();
                sr = null;

                if (checker == null)
                {
                    checker = new Thread(Checker.run);
                    checker.Start(new Args(dbhost, database, dbuser, dbpass));
                }                                                           //     <<<<<<<<----------------Start SQL thread---------------->>>>>>>> 


               
                

                ///////////////////////////////////////////////              <<<<<<<<---------------- Connect for the first time---------------->>>>>>>>
                client.Connect();
                
                //SteamID admin = new SteamID((ulong)76561197990973056);
                

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
                                Console.Error.WriteLine("Unable to connect to Steam: {0}", c.Result);
                                //isRunning = false;
                                return;
                            }

                            Console.WriteLine("Connected to Steam! Logging in '{0}'...", user);
                            Console.Error.WriteLine("Connected to Steam! Logging in '{0}'...", user);
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
                            Console.Error.WriteLine("Disconnected from Steam");

                            discon = true;
                            if (Checker.end)
                                isRunning = false;
                        });

                        callback.Handle<SteamUser.LoggedOnCallback>(c =>
                        {
                            if (c.Result != EResult.OK)
                            {
                                Console.WriteLine("Unable to logon to Steam: {0} / {1}", c.Result, c.ExtendedResult);
                                Console.Error.WriteLine("Unable to logon to Steam: {0} / {1}", c.Result, c.ExtendedResult);
                                discon = true;
                                return;
                            }
                            else
                            {

                                Console.WriteLine("Successfully logged on!");
                                Console.Error.WriteLine("Successfully logged on!");

                                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                
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
                            Console.Error.WriteLine("We have {0} friends", friendCount);
                            for (int x = 0; x < friendCount; x++)
                            {
                                // steamids identify objects that exist on the steam network, such as friends, as an example
                                SteamID steamIdFriend = steamFriends.GetFriendByIndex(x);

                                // we'll just display the STEAM_ rendered version
                                Console.WriteLine("Friend: {0}", steamIdFriend.Render());
                                Console.Error.WriteLine("Friend: {0}", steamIdFriend.Render());
                            }

                            // we can also iterate over our friendslist to accept or decline any pending invites

                            foreach (var friend in c.FriendList)
                            {
                                if (friend.Relationship == EFriendRelationship.RequestRecipient)
                                {
                                    // this user has added us, NOPE!!
                                    steamFriends.RemoveFriend(friend.SteamID);
                                }
                                if (friend.Relationship == EFriendRelationship.Friend)
                                {
                                    if (friend.SteamID.ConvertToUInt64() != admin.ConvertToUInt64())
                                    {
                                        // this user has accepted our invite, sure!
                                        steamFriends.SendChatMessage(friend.SteamID, EChatEntryType.Typing, "");
                                        Thread.Sleep(2000);
                                        steamFriends.SendChatMessage(friend.SteamID, EChatEntryType.ChatMsg, greet);
                                        using (WebClient reader = new WebClient())
                                        {
                                            string s = reader.DownloadString("http://steamcommunity.com/actions/GroupInvite?type=groupInvite&inviter=" + client.SteamID.ConvertToUInt64() + "&invitee=" + friend.SteamID.ConvertToUInt64() + "&group=103582791432308455&sessionID=" + client.SessionID);
                                            Console.WriteLine("http://steamcommunity.com/actions/GroupInvite?type=groupInvite&inviter=" + client.SteamID.ConvertToUInt64() + "&invitee=" + friend.SteamID.ConvertToUInt64() + "&group=103582791432308455&sessionID=" + client.SessionID);
                                            Console.Error.WriteLine("http://steamcommunity.com/actions/GroupInvite?type=groupInvite&inviter=" + client.SteamID.ConvertToUInt64() + "&invitee=" + friend.SteamID.ConvertToUInt64() + "&group=103582791432308455&sessionID=" + client.SessionID);
                                            Console.WriteLine(s);
                                            Console.Error.WriteLine(s);
                                        }
                                        steamFriends.RemoveFriend(friend.SteamID);
                                    }
                                }
                            }
                            
                            Checker.friends = steamFriends;
                            Checker.client = client;

                        });

                        callback.Handle<SteamFriends.FriendMsgCallback>(c =>            //  <<<<<<<<----------------LEAST IMPORTANT CALLBACK---------------->>>>>>>> 
                        {
                            Console.WriteLine("recieved: " + c.Message);
                            Console.Error.WriteLine("recieved: " + c.Message);
                        });


                        //###############################################################################################
                        callback.Handle<SteamUser.LoggedOffCallback>(c =>
                        {
                            Console.WriteLine("Logged off of Steam: {0}", c.Result);
                            Console.Error.WriteLine("Logged off of Steam: {0}", c.Result);
                            discon = true;
                            if (Checker.end)
                                isRunning = false;
                        });

                        
                    

                    

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.Error.WriteLine(e);
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
                    Console.Error.WriteLine("Currently Offline.");
                    if (Program.stw.IsRunning)
                    {
                        if (Program.stw.ElapsedMilliseconds >= 30000)
                        {
                            restarting = true;
                            Console.WriteLine("restart desired");
                            Console.Error.WriteLine("restart desired");
                            Program.recon = false;
                            while (Program.ready == false)
                            {
                                Thread.Sleep(1000);
                            }
                            Console.WriteLine("Attempting restart.");
                            Console.Error.WriteLine("Attempting restart.");
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