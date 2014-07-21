Overview:

MySQLClient.cs is not used. I did find it to be a useful reference.

LinkAR.cs holds a helpful data stucture that I wrote for keeping track of the current requests for admins. 

Checker.cs holds the code for the thread that looks for the requests on the database.  It also sends out the messages that request an admin. 

Program.cs is the main program that handles callbacks and commands sent to it through chat. To add a command right now, go to line 725.

For compiling:

Well, you need a few libraries:

mysql.data
mysql.data.entity
mysql.visualstudio
mysql.web   

not sure if all these are usd atm....
Also:

SteamKit2
protobuf-net

Of course, these are on top of the usual C# libraries.  

What is easiest is just to use visual studio 2010 (Express will do) and open the projet you no doubt have.

I have just been using the build (not the release).  All I want is the exe.  

To run the exe, you need to have the dll's listed above in the same folder as the exe.  

Also, you must have a file called ip.txt present with the following info (and this info only(no comments)).

ip
databasename
dbuser
dbpassword

An example file is provided. 

Commands so far:

/accept
/backup
/shutdown1337
/get friends  // this has some variations, however they are obsolete. 
/get page  //obsolete
/say
/hold1337 //stops others from sending backups or says
/tsay
/say1337
/help
/scores
/thread?
/clearaccepts1337
/togglesql1337  //turns the sql reading thread on and off
/getlog  //it will start sending you the log
/polishaccepts1337  //cleans up the accepts to make them easier to read
/notify 
/removefriends1337  //removes all friends but the issuer (Be careful with this)
/listfriends  //just added and not tested.  It should list all the friends and their current states.  Not sure about the latter feature though...






