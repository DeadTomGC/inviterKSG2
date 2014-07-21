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

namespace admnNotify
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
        static bool hold = false;
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


                ////////////////////////////////////////////                     <<<<<<<<----------------Read old accepts---------------->>>>>>>>
                try
                {
                    string temp;

                    StreamReader oldAccepts = new StreamReader("accepts.txt");
                    while (oldAccepts.EndOfStream == false)
                    {
                        temp = oldAccepts.ReadLine();
                        ips.Add(ulong.Parse(temp.Substring(0, temp.IndexOf(' '))), temp.Substring(temp.IndexOf(' ')).Trim());

                    }
                    oldAccepts.Close();
                    oldAccepts = null;
                    sw.WriteLine("closed file");
                    sw.Flush();
                    Console.WriteLine("closed file");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Failed to load past accepts");
                }
                //////////////////////////////////////////////////////      <<<<<<<<----------------End Read old accepts---------------->>>>>>>>

                /////////////////////////////////////////////////////       <<<<<<<<----------------Read old tsays---------------->>>>>>>>
                try
                {
                    StreamReader oldtsay = new StreamReader("tsay.txt");
                    while (oldtsay.EndOfStream == false)
                    {
                        black.Add(new SteamID(ulong.Parse(oldtsay.ReadLine().Trim())), 1);

                    }
                    oldtsay.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to load past tsays");
                }
                ///////////////////////////////////////////////              <<<<<<<<---------------- End Read old tsays---------------->>>>>>>>
                

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
                                    SteamID tempid;
                                    string s = reader.DownloadString("http://steamcommunity.com/groups/ksadmins/memberslistxml/?xml=1");
                                    int count = 0;
                                    bool run = true;
                                    while (run)
                                    {
                                        run = false;
                                        if (s.Contains("<steamID64>"))
                                        {
                                            run = true;
                                            string p = s.Substring(s.IndexOf("<steamID64>") + 11, 17);
                                            s = s.Substring(s.IndexOf("<steamID64>") + 28);


                                            Console.WriteLine("sent invite: " + p);
                                            sw.WriteLine("sent invite: " + p);
                                            sw.Flush();
                                            steamFriends.AddFriend(tempid = new SteamID(ulong.Parse(p)));

                                            if (ips.ContainsKey(tempid.ConvertToUInt64()) == false)
                                            {
                                                ips.Add(tempid.ConvertToUInt64(), "0 null 0");
                                            }


                                        }
                                        count++;
                                    }
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

                        });

                        callback.Handle<SteamFriends.FriendMsgCallback>(c =>            //  <<<<<<<<----------------MOST IMPORTANT CALLBACK---------------->>>>>>>> 
                        {
                            Console.WriteLine("recieved: " + c.Message);
                            sw.WriteLine("recieved: " + c.Message);
                            sw.Flush();
                            Checker.logThem("recieved: " + c.Message);



                            if (c.Message.ToLower().Contains("/accept"))  //             <<<<<<<<----------------Beginning of command checker---------------->>>>>>>> 
                            {
                                if (Checker.task)
                                {
                                    string tmpN, tmpS;
                                    Checker.temp.setFirst();
                                    tmpN = Checker.temp.getCurrent()[0];
                                    tmpS = Checker.temp.getCurrent()[3];
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Thanks for responding.  Here is the server link: steam://connect/" + Checker.temp.getCurrent()[1]);
                                    if (ips.ContainsKey(c.Sender.ConvertToUInt64()) == false)
                                    {
                                        if (tmpN.Contains("http://steamcommunity.com/profiles/") == false)
                                        {
                                            ips.Add(c.Sender.ConvertToUInt64(), Checker.temp.getCurrent()[1] + " " + Checker.temp.getCurrent()[3] + " 1");
                                        }
                                        else
                                        {
                                            ips.Add(c.Sender.ConvertToUInt64(), Checker.temp.getCurrent()[1] + " " + Checker.temp.getCurrent()[3] + " 0");
                                        }
                                    }
                                    else
                                    {
                                        if (tmpN.Contains("http://steamcommunity.com/profiles/") == false)
                                        {
                                            ips[c.Sender.ConvertToUInt64()] = Checker.temp.getCurrent()[1] + " " + Checker.temp.getCurrent()[3] + " " + string.Format("{0}", (int.Parse(ips[c.Sender.ConvertToUInt64()].Substring(ips[c.Sender.ConvertToUInt64()].LastIndexOf(' ') + 1)) + 1));
                                        }
                                        else
                                        {
                                            ips[c.Sender.ConvertToUInt64()] = Checker.temp.getCurrent()[1] + " " + Checker.temp.getCurrent()[3] + " " + string.Format("{0}", int.Parse(ips[c.Sender.ConvertToUInt64()].Substring(ips[c.Sender.ConvertToUInt64()].LastIndexOf(' ') + 1)));
                                        }
                                    }
                                    Console.WriteLine(ips[c.Sender.ConvertToUInt64()]);
                                    sw.WriteLine(ips[c.Sender.ConvertToUInt64()]);

                                    Checker.logThem(ips[c.Sender.ConvertToUInt64()]);

                                    Checker.temp.deleteFirst();
                                    Checker.task = false;

                                    var vals = ips.Values.ToArray();
                                    var keys = ips.Keys.ToArray();
                                    try
                                    {
                                        StreamWriter accepts = new StreamWriter("accepts.txt");
                                        for (int i = 0; i < keys.Length; i++)
                                        {

                                            accepts.WriteLine(string.Format("{0}", keys[i]) + " " + vals[i]);

                                        }
                                        accepts.Close();
                                        accepts = null;
                                        sw.WriteLine("closed file");
                                        sw.Flush();
                                    }
                                    catch (Exception ex)
                                    {
                                        sw.WriteLine(ex);
                                        sw.Flush();
                                    }
                                    for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                    {
                                        if (steamFriends.GetFriendByIndex(i) != c.Sender)
                                        {
                                            if (steamFriends.GetFriendPersonaState(steamFriends.GetFriendByIndex(i)) != EPersonaState.Offline && steamFriends.GetFriendPersonaState(steamFriends.GetFriendByIndex(i)) != EPersonaState.Snooze)
                                            {
                                                if (i >= 0)
                                                    steamFriends.SendChatMessage(steamFriends.GetFriendByIndex(i), EChatEntryType.ChatMsg, steamFriends.GetFriendPersonaName(c.Sender) + " has accepted the task of reponding to " + tmpN + "'s request on " + tmpS + ".");
                                            }
                                        }

                                    }

                                }
                            }
                            else if (c.Message.ToLower().Contains("/backup") && (hold == false || admin.Equals(c.Sender)))
                            {
                                if (ips.ContainsKey(c.Sender.ConvertToUInt64()) == false)
                                {
                                    ips.Add(c.Sender.ConvertToUInt64(), "0 null 0");
                                }
                                Checker.temp.AddEnd();
                                Checker.temp.getCurrent()[0] = steamFriends.GetFriendPersonaName(c.Sender) + System.Environment.NewLine + "http://steamcommunity.com/profiles/" + c.Sender.ConvertToUInt64() + System.Environment.NewLine;
                                using (WebClient reader = new WebClient())
                                {
                                    string s = reader.DownloadString("http://steamcommunity.com/profiles/" + c.Sender.ConvertToUInt64() + "/?xml=1");
                                    if (s.Contains("steam://connect/"))
                                    {
                                        int length = 0, index = s.IndexOf("steam://connect/") + 16;
                                        while (s.Substring(index + length, 1) != '"' + "")
                                            length++;
                                        Checker.temp.getCurrent()[1] = s.Substring(index, length);
                                    }
                                    else
                                    {
                                        Checker.temp.getCurrent()[1] = ips[c.Sender.ConvertToUInt64()].Substring(0, ips[c.Sender.ConvertToUInt64()].Trim().IndexOf(' ')).Trim();
                                    }
                                }
                                //Checker.temp.getCurrent()[1] = ips[c.Sender.ConvertToUInt64()].Substring(0, ips[c.Sender.ConvertToUInt64()].Trim().IndexOf(" ")).Trim();
                                Checker.temp.getCurrent()[2] = c.Message.Substring(c.Message.ToLower().IndexOf("/backup") + 7);
                                Checker.temp.getCurrent()[3] = ips[c.Sender.ConvertToUInt64()].Substring(ips[c.Sender.ConvertToUInt64()].IndexOf(" "), ips[c.Sender.ConvertToUInt64()].LastIndexOf(' ') - ips[c.Sender.ConvertToUInt64()].IndexOf(' ')).Trim();
                                Console.WriteLine(Checker.temp.getCurrent()[0] + " " + Checker.temp.getCurrent()[1] + "  " + Checker.temp.getCurrent()[2] + " " + Checker.temp.getCurrent()[3]);
                                sw.WriteLine(Checker.temp.getCurrent()[0] + " " + Checker.temp.getCurrent()[1] + "  " + Checker.temp.getCurrent()[2] + " " + Checker.temp.getCurrent()[3]);
                                sw.Flush();

                                Checker.logThem(Checker.temp.getCurrent()[0] + " " + Checker.temp.getCurrent()[1] + "  " + Checker.temp.getCurrent()[2] + " " + Checker.temp.getCurrent()[3]);

                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Help request will be processed after all other current requests are handled");

                            }
                            else if (c.Message.Contains("/shutdown1337"))
                            {
                                steamUser.LogOff();
                                Checker.end = true;
                            }
                            else if (c.Message.Contains("/get friends"))
                            {
                                using (WebClient reader = new WebClient())
                                {
                                    SteamID tempid;
                                    string s = reader.DownloadString("http://steamcommunity.com/groups/ksadmins/memberslistxml/?xml=1");
                                    int count = 0;
                                    bool run = true;
                                    while (run)
                                    {
                                        run = false;
                                        if (s.Contains("<steamID64>"))
                                        {
                                            run = true;
                                            string p = s.Substring(s.IndexOf("<steamID64>") + 11, 17);
                                            s = s.Substring(s.IndexOf("<steamID64>") + 28);
                                            if (c.Message.Contains(" p"))
                                            {
                                                if (c.Message.Substring(c.Message.IndexOf(" p") + 2).Trim().Equals(p))
                                                {
                                                    Console.WriteLine("sent invite: " + p);
                                                    sw.WriteLine("sent invite: " + p);
                                                    sw.Flush();
                                                    steamFriends.AddFriend(tempid = new SteamID(ulong.Parse(p)));

                                                    if (ips.ContainsKey(tempid.ConvertToUInt64()) == false)
                                                    {
                                                        ips.Add(tempid.ConvertToUInt64(), "0 null 0");
                                                    }
                                                    run = false;

                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("sent invite: " + p);
                                                sw.WriteLine("sent invite: " + p);
                                                sw.Flush();
                                                steamFriends.AddFriend(tempid = new SteamID(ulong.Parse(p)));

                                                if (ips.ContainsKey(tempid.ConvertToUInt64()) == false)
                                                {
                                                    ips.Add(tempid.ConvertToUInt64(), "0 null 0");
                                                }
                                            }

                                        }
                                        count++;
                                        if (c.Message.Contains(" n"))
                                        {
                                            if (Int32.Parse(c.Message.Substring(c.Message.IndexOf(" n") + 2).Trim()) <= count)
                                                run = false;
                                        }
                                        Thread.Sleep(100);
                                    }
                                    for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                    {
                                        if (steamFriends.GetFriendRelationship(steamFriends.GetFriendByIndex(i)) != EFriendRelationship.Friend && steamFriends.GetFriendRelationship(steamFriends.GetFriendByIndex(i)) != EFriendRelationship.RequestInitiator)
                                        {
                                            // this user has added us, NOPE!!
                                            steamFriends.RemoveFriend(steamFriends.GetFriendByIndex(i));
                                        }
                                    }
                                    Console.WriteLine("DONE");
                                    sw.WriteLine("DONE");
                                    sw.Flush();
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "DONE with " + steamFriends.GetFriendCount() + "friends.");
                                }
                            }
                            else if (c.Message.Contains("/get page") && hold == false)
                            {
                                using (WebClient reader = new WebClient())
                                {
                                    string s = reader.DownloadString("http://steamcommunity.com/groups/ksadmins/memberslistxml/?xml=1");
                                    Console.WriteLine(s);
                                    sw.WriteLine(s);
                                    sw.Flush();
                                }
                            }
                            else if (c.Message.ToLower().Contains("/say "))
                            {
                                if (black.ContainsKey(c.Sender) == false)
                                {
                                    if (hold == false || admin.Equals(c.Sender))
                                    {
                                        string message = c.Message.Substring(c.Message.IndexOf("/say") + 5);

                                        for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                        {
                                            if (black.ContainsKey(steamFriends.GetFriendByIndex(i)) == false)
                                            {
                                                if (steamFriends.GetFriendPersonaState(steamFriends.GetFriendByIndex(i)) != EPersonaState.Offline && steamFriends.GetFriendPersonaState(steamFriends.GetFriendByIndex(i)) != EPersonaState.Snooze)
                                                {
                                                    if (i >= 0)
                                                        steamFriends.SendChatMessage(steamFriends.GetFriendByIndex(i), EChatEntryType.ChatMsg, steamFriends.GetFriendPersonaName(c.Sender) + " says: " + message);
                                                }
                                            }

                                        }
                                    }
                                    else
                                    {
                                        steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "The ADMIN has disabled chat.");
                                    }
                                }
                                else
                                {
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "You may not use this command now. /tsay");
                                }
                            }
                            else if (c.Message.Contains("/hold1337"))
                            {
                                if (hold)
                                {
                                    hold = false;
                                }
                                else
                                {
                                    hold = true;
                                }
                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "All have been stopped from chatting but you.");
                                admin = c.Sender;
                            }
                            else if (c.Message.ToLower().Contains("/tsay"))
                            {
                                if (black.ContainsKey(c.Sender))
                                {
                                    black.Remove(c.Sender);
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "You will now recieve say messages.");

                                }
                                else
                                {
                                    black.Add(c.Sender, 1);
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "You will not recieve anymore messages unless they are sent by an admin.");
                                }
                                StreamWriter tsay = new StreamWriter("tsay.txt");

                                var keys = black.Keys.ToArray();
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    tsay.WriteLine(string.Format("{0}", keys[i].ConvertToUInt64()));
                                }
                                tsay.Close();


                            }
                            else if (c.Message.Contains("/say1337 "))
                            {
                                string message = c.Message.Substring(c.Message.IndexOf("/say1337") + 8);

                                for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                {
                                    if (black.ContainsKey(steamFriends.GetFriendByIndex(i)) == false)
                                    {
                                        if (i >= 0)
                                            steamFriends.SendChatMessage(steamFriends.GetFriendByIndex(i), EChatEntryType.ChatMsg, steamFriends.GetFriendPersonaName(c.Sender) + "(ADMIN) says: " + message);
                                    }

                                }

                            }
                            else if (c.Message.ToLower().Contains("/help"))
                            {
                                if (c.Message.ToLower().Contains("1337"))
                                {
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, System.Environment.NewLine + "'/get friends' adds all friends in the admin group" + System.Environment.NewLine + "/hold1337 stops others from sending backups or says" + System.Environment.NewLine + "/thread? checks to see if the SQL thread is on. (Note that it still might not be working even if this returns true.)" + System.Environment.NewLine + "/clearaccepts1337  wipes the accepts records" + System.Environment.NewLine + "/togglesql1337  turns the sql reading thread on and off" + System.Environment.NewLine + "/getlog  it will start sending you the log" + System.Environment.NewLine + "/polishaccepts1337  cleans up the scores to make them easier to read" + System.Environment.NewLine + "/listadmins1337  lists all admins int64SteamID's next to their names." + System.Environment.NewLine + "/removeadmin1337 'int64SteamID' removes an admin (and his records) based on the int64SteamID provided (Also sends them a notification of this)" + System.Environment.NewLine + "/removefriends1337  removes all friends but the issuer (Be careful with this)" + System.Environment.NewLine + "/testreport1337 manually adds a test report to the sql server.");

                                }
                                else
                                {
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, System.Environment.NewLine + "'/accept' to respond to backup and admin requests " + System.Environment.NewLine + "'/backup your_message' to request backup on your current server" + System.Environment.NewLine + " '/say your_message' to send your_message to all admins" + System.Environment.NewLine + " '/tsay' to stop recieving say messages (You will still recieve notifies and bot ADMIN messages)" + System.Environment.NewLine + " '/notify message' the same thing as /say except tsay does not apply. Only use for important admin related stuff." + System.Environment.NewLine + " '/scores' See how many accepts people have." + System.Environment.NewLine + " '/listfriends' See who is an admin!" + System.Environment.NewLine + " '/thread?' or '/status' lets you know if the sql is on and connected");
                                }
                            }
                            else if (c.Message.ToLower().Contains("/scores"))
                            {
                                string message = "";
                                int max = 0;
                                string maxname = "";
                                bool tie = false;
                                var vals = ips.Values.ToArray();
                                var keys = ips.Keys.ToArray();
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    message = message + System.Environment.NewLine + steamFriends.GetFriendPersonaName(new SteamID(keys[i])) + " has " + vals[i].Substring(vals[i].LastIndexOf(' ') + 1);
                                    if (int.Parse(vals[i].Substring(vals[i].LastIndexOf(' ') + 1)) > max)
                                    {
                                        maxname = steamFriends.GetFriendPersonaName(new SteamID(keys[i])) + System.Environment.NewLine + "http://steamcommunity.com/profiles/" + string.Format("{0}", keys[i]);
                                        max = int.Parse(vals[i].Substring(vals[i].LastIndexOf(' ') + 1).Trim());
                                        tie = false;
                                    }
                                    else if (int.Parse(vals[i].Substring(vals[i].LastIndexOf(' ') + 1)) == max)
                                    {
                                        tie = true;
                                    }
                                }
                                if (tie)
                                {
                                    message = message + System.Environment.NewLine + "There was a tie for " + string.Format("{0}", max) + " accepts!";
                                }
                                else
                                {
                                    message = message + System.Environment.NewLine + "Player with the most accepts is:" + maxname + System.Environment.NewLine + "with " + string.Format("{0}", max) + " accepts";
                                }
                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, message);
                            }
                            else if (c.Message.ToLower().Contains("/thread?") || c.Message.ToLower().Contains("/status"))
                            {
                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, System.Environment.NewLine + (checker.IsAlive && (Checker.discon==false)));

                            }
                            else if (c.Message.Contains("/clearaccepts1337"))
                            {

                                var keys = ips.Keys.ToArray();

                                StreamWriter accepts = new StreamWriter("accepts.txt");
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    ips[keys[i]] = ips[keys[i]].Substring(0, ips[keys[i]].Trim().LastIndexOf(" ")).Trim() + " 0";
                                    accepts.WriteLine(string.Format("{0}", keys[i]) + " " + "0");

                                }
                                accepts.Close();

                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "DONE");
                            }
                            else if (c.Message.ToLower().Contains("/togglesql1337"))
                            {
                                databackup = Checker.dataconv;
                                if (checker.IsAlive)
                                {
                                    Checker.end = true;
                                    checker.Abort();
                                    while (checker.IsAlive)
                                    {
                                        Thread.Sleep(100);
                                    }
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "SQL thread is now off!");
                                }
                                else
                                {

                                    checker = new Thread(Checker.run);
                                    checker.Start(databackup);
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Done restarting the SQL thread");
                                }
                            }
                            else if (c.Message.ToLower().Contains("/getlog"))
                            {

                                if (logs.ContainsKey(c.Sender))
                                {
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "You will not recieve anymore logs.");

                                    logs.Remove(c.Sender);
                                    Checker.logs = logs;

                                }
                                else
                                {
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "You will now recieve the log.");
                                    logs.Add(c.Sender, 1);
                                    Checker.logs = logs;
                                }


                            }
                            else if (c.Message.ToLower().Contains("/polishaccepts1337"))
                            {


                                var keys = ips.Keys.ToArray();

                                for (int i = 0; i < keys.Length; i++)
                                {
                                    if (int.Parse(ips[keys[i]].Substring(ips[keys[i]].LastIndexOf(' ')).Trim()) > 0 && steamFriends.GetFriendRelationship(new SteamID(keys[i])) == EFriendRelationship.Friend)
                                    {

                                    }
                                    else
                                    {
                                        if (steamFriends.GetFriendRelationship(new SteamID(keys[i])) != EFriendRelationship.Friend && steamFriends.GetFriendRelationship(new SteamID(keys[i])) != EFriendRelationship.RequestRecipient)
                                        {
                                            steamFriends.RemoveFriend(new SteamID(keys[i]));
                                        }
                                        ips.Remove(keys[i]);
                                        keys = ips.Keys.ToArray();
                                    }

                                }



                                var vals = ips.Values.ToArray();
                                keys = ips.Keys.ToArray();


                                StreamWriter accepts = new StreamWriter("accepts.txt");
                                for (int i = 0; i < keys.Length; i++)
                                {

                                    accepts.WriteLine(string.Format("{0}", keys[i]) + " " + vals[i]);


                                }
                                accepts.Close();
                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Done, view accepts using /scores.");
                            }
                            else if (c.Message.ToLower().Contains("/notify "))
                            {

                                if (hold == false || admin.Equals(c.Sender))
                                {
                                    string message = c.Message.Substring(c.Message.IndexOf("/notify") + 8);

                                    for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                    {
                                        if (black.ContainsKey(steamFriends.GetFriendByIndex(i)) == false)
                                        {
                                            if (steamFriends.GetFriendPersonaState(steamFriends.GetFriendByIndex(i)) != EPersonaState.Offline && steamFriends.GetFriendPersonaState(steamFriends.GetFriendByIndex(i)) != EPersonaState.Snooze)
                                            {
                                                if (i >= 0)
                                                    steamFriends.SendChatMessage(steamFriends.GetFriendByIndex(i), EChatEntryType.ChatMsg, steamFriends.GetFriendPersonaName(c.Sender) + " notifies: " + message);
                                            }
                                        }

                                    }
                                }
                                else
                                {
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "The ADMIN has disabled chat.");
                                }


                            }
                            else if (c.Message.Contains("/removefriends1337"))
                            {
                                for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                {
                                    if (steamFriends.GetFriendByIndex(i).Equals(c.Sender) == false)
                                    {
                                        steamFriends.RemoveFriend(steamFriends.GetFriendByIndex(i));
                                    }

                                }
                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "DONE: It is recommended that you run /get friends");
                            }
                            else if (c.Message.Contains("/listfriends"))
                            {
                                string message = "";

                                for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                {

                                    message = message + System.Environment.NewLine + steamFriends.GetFriendPersonaName(steamFriends.GetFriendByIndex(i)) + " " + steamFriends.GetFriendPersonaState(steamFriends.GetFriendByIndex(i)).ToString();


                                }
                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, message);
                            }
                            else if (c.Message.Contains("/recon"))
                            {
                                recon = true;
                            }
                            else if (c.Message.ToLower().Contains("/listadmins1337"))
                            {
                                string message = "";
                                //var keys = ips.Keys.ToArray();
                                for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                {
                                    message = message + System.Environment.NewLine + steamFriends.GetFriendByIndex(i).ConvertToUInt64() + " " + steamFriends.GetFriendPersonaName(steamFriends.GetFriendByIndex(i));

                                }
                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, message);

                            }
                            else if (c.Message.ToLower().Contains("/removeadmin1337 "))
                            {
                                try
                                {
                                    ulong id = ulong.Parse(c.Message.Substring(c.Message.IndexOf("/removeadmin1337 ")+18).Trim());
                                    if (ips.ContainsKey(id))
                                    {
                                        steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Admin " + steamFriends.GetFriendPersonaName(new SteamID(id)) + " will be removed.");
                                        steamFriends.SendChatMessage(new SteamID(id), EChatEntryType.ChatMsg, "Your Admin status is being removed. (nothing personal... I hope.)");
                                        steamFriends.RemoveFriend(new SteamID(id));
                                        ips.Remove(id);

                                        var vals = ips.Values.ToArray();
                                        var keys = ips.Keys.ToArray();

                                        StreamWriter accepts = new StreamWriter("accepts.txt");
                                        for (int i = 0; i < keys.Length; i++)
                                        {

                                            accepts.WriteLine(string.Format("{0}", keys[i]) + " " + vals[i]);

                                        }
                                        accepts.Close();

                                        steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Removed.");
                                    }
                                    else
                                    {
                                        steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Failed to find that admin's id.");
                                    }
                                }
                                catch (Exception e)
                                {
                                    steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Failed to find that admin's id.");
                                }
                            }
                            else if (c.Message.ToLower().Contains("/testreport1337"))
                            {
                                Checker.test = true;
                                steamFriends.SendChatMessage(c.Sender, EChatEntryType.ChatMsg, "Sending test report soon.");

                            }
                            else if (c.Message.ToLower().Contains("/rename1337 "))
                            {
                                string message = c.Message.Substring(c.Message.IndexOf("/rename1337 ") + 12);
                                steamFriends.SetPersonaName(message);

                            }
                            else if (false)   ///replace false with something like c.Message.Contains("/listfriends") to look for the command /listfriends
                            {
                                //put how the command should be handled here

                                //copy this entire else if block and paste after this end "}" to add another command

                            }//                                                  <<<<<<<<----------------ADD COMMANDS HERE---------------->>>>>>>> 

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