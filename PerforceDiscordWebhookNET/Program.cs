using System;
using System.Collections.Generic;
using Perforce.P4;
using Discord.Webhook;
using Discord;
using System.Linq;

namespace PerforceDiscordWebhookNET
{
    class Program
    {
        static void Main(string[] args)
        {
            Repository rep = LoginPerforce();

            IList<Changelist> changes = GetNewChangelists(rep);

            Changelist changeList = rep.GetChangelist(changes[0].Id);

            SendDiscordWebhook(changeList);

            Console.ReadLine();
        }

        static Repository LoginPerforce()
        {
            // https://www.perforce.com/manuals/p4api.net/p4api.net_reference/html/af6b5020-31c0-491f-b8c8-a803ae388198.htm

            // initialize the connection variables
            // note: this is a connection using a password
            string p4port = Environment.GetEnvironmentVariable("P4PORT");
            string p4user = Environment.GetEnvironmentVariable("P4USER");
            string p4client = Environment.GetEnvironmentVariable("P4CLIENT");
            string p4pass = Environment.GetEnvironmentVariable("P4PASSWORD");
            string p4host = Environment.GetEnvironmentVariable("P4HOST");

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

            return rep;
        }

        static IList<Changelist> GetNewChangelists(Repository rep)
        {
            string p4path = Environment.GetEnvironmentVariable("P4PATH");

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

            return changes;
        }

        static void SendDiscordWebhook(Changelist changeList)
        {
            // TODO: Fix this uglyness
            string authorStr = changeList.OwnerName;

            string user1 = Environment.GetEnvironmentVariable("USER1");
            string user2 = Environment.GetEnvironmentVariable("USER2");
            string user3 = Environment.GetEnvironmentVariable("USER3");
            string user4 = Environment.GetEnvironmentVariable("USER4");
            string user5 = Environment.GetEnvironmentVariable("USER5");

            string icon;

            if (authorStr == user1)
            {
                icon = Environment.GetEnvironmentVariable("U1ICON");
            }
            else if (authorStr == user2)
            {
                icon = Environment.GetEnvironmentVariable("U2ICON");
            }
            else if (authorStr == user3)
            {
                icon = Environment.GetEnvironmentVariable("U3ICON");
            }
            else if (authorStr == user4)
            {
                icon = Environment.GetEnvironmentVariable("U4ICON");
            }
            else if (authorStr == user5)
            {
                icon = Environment.GetEnvironmentVariable("U5ICON");
            }
            else
            {
                icon = "https://cdn.discordapp.com/embed/avatars/0.png";
            }

            // Discord.net.webhook
            ulong webhookId = Convert.ToUInt64(Environment.GetEnvironmentVariable("WEBHOOKID"));
            string webhookToken = Environment.GetEnvironmentVariable("WEBHOOKTOKEN");
            DiscordWebhookClient discordWebhookClient = new DiscordWebhookClient(webhookId, webhookToken);

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author
                .WithName(authorStr)
                .WithIconUrl(icon)
                .Build();

            EmbedFooterBuilder footer = new EmbedFooterBuilder();
            footer
                .WithIconUrl("https://i.imgur.com/qixMjRV.png")
                .WithText("Helix Core")
                .Build();

            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder
                .WithAuthor(author)
                .WithFooter(footer)
                .WithColor(Color.Blue)
                .WithTitle(changeList.Description)
                .WithDescription(changeList.ClientId)
                .WithUrl(Environment.GetEnvironmentVariable("EMBEDURL"))
                .WithTimestamp(changeList.ModifiedDate)
                .WithThumbnailUrl(Environment.GetEnvironmentVariable("EMBEDTHUMB"));

            foreach (var file in changeList.Files)
            {
                string title = file.Action.ToString() + ' ' + file.Type.ToString();
                string value = file.DepotPath.Path + '#' + file.HeadRev.ToString();
                embedBuilder.AddField(title, value);
            }

            Embed embed = embedBuilder.Build();

            string content = "Perforce change " + changeList.Id;
            IEnumerable<Embed> embeds = Enumerable.Repeat(embed, 1);
            discordWebhookClient.SendMessageAsync(content, false, embeds);
        }
    }
}
