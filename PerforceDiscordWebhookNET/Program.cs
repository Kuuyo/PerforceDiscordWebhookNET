using System;
using System.Collections.Generic;
using Perforce.P4;
using Discord.Webhook;
using System.Runtime.InteropServices;

namespace PerforceDiscordWebhookNET
{
    class Program
    {
        static void Main(string[] args)
        {
            // https://www.perforce.com/manuals/p4api.net/p4api.net_reference/html/af6b5020-31c0-491f-b8c8-a803ae388198.htm

            // initialize the connection variables
            // note: this is a connection using a password
            string p4port = Environment.GetEnvironmentVariable("P4PORT");
            string p4user = Environment.GetEnvironmentVariable("P4USER");
            string p4client = Environment.GetEnvironmentVariable("P4CLIENT");
            string p4pass = Environment.GetEnvironmentVariable("P4PASSWORD");
            string p4host = Environment.GetEnvironmentVariable("P4HOST");
            string p4path = Environment.GetEnvironmentVariable("P4PATH");

            // define the server, repository and connection
            Server server = new Server(new ServerAddress(p4port));
            Repository rep = new Repository(server);
            Connection con = rep.Connection;


            // use the connection variables for this connection
            con.UserName = p4user;
            con.Client = new Client
            {
                Name = p4client,
                Host = p4host
            };


            // connect to the server
            con.Connect(null);


            // login to the server to get credential
            // (using null for options and user params)
            Credential cred = con.Login(p4pass, null, null);

            // set the options for the p4 changes command
            string clientName = "";
            int maxItems = 5;
            string userName = "";
            ChangesCmdOptions options = new ChangesCmdOptions(ChangesCmdFlags.FullDescription | ChangesCmdFlags.IncludeTime,
                    clientName, maxItems, ChangeListStatus.Submitted, userName);

            // create a FileSpec for //depot/test.txt
            // using new FileSpec(PathSpec path, VersionSpec version)
            PathSpec path = new DepotPath(p4path);
            FileSpec depotFile = new FileSpec(path, null);

            // run the command against the current repository
            IList<Changelist> changes = rep.GetChangelists(options, depotFile);

            foreach (var c in changes)
            {
                Changelist cl = rep.GetChangelist(c.Id);
                Console.WriteLine(cl);
                foreach (var f in cl.Files)
                {
                    FileSpec fileSpec = new FileSpec(f.DepotPath, new Revision(f.HeadRev));
                    Console.WriteLine(fileSpec.ToEscapedString());

                    FileSpec fileSpec2 = new FileSpec(f.DepotPath, new Revision(f.HeadRev - 1));
                    GetDepotFileDiffsCmdOptions opts =
                        new GetDepotFileDiffsCmdOptions(GetDepotFileDiffsCmdFlags.None,
                        0, 0, null, null, null);
                    IList<DepotFileDiff> diff = rep.GetDepotFileDiffs(fileSpec.ToEscapedString(), fileSpec2.ToEscapedString(), opts);

                    Console.WriteLine();
                    Console.WriteLine("Changes:");

                    foreach (var d in diff)
                    {
                        Console.WriteLine(d.Diff);
                        Console.WriteLine();
                    }
                }
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.ReadLine();
        }
    }
}
