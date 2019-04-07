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

            IList<Changelist> unsyncedChanges = GetNewChangelists(rep);
            if (unsyncedChanges.Count > 0)
            {
                IList<DepotFileDiff> fileDiffs = GetFileDiffs(rep, unsyncedChanges);
                SendDiscordWebhook(unsyncedChanges);
            }

            Console.WriteLine("Done!");
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

        static IList<Changelist> GetNewChangelists(Repository rep, int maxItems = 5, string clientName = "", string userName = "")
        {
            string p4path = Environment.GetEnvironmentVariable("P4PATH");

            ChangesCmdOptions options = new ChangesCmdOptions(ChangesCmdFlags.FullDescription | ChangesCmdFlags.IncludeTime,
                    clientName, maxItems, ChangeListStatus.Submitted, userName);

            PathSpec path = new DepotPath(p4path);
            FileSpec depotFile = new FileSpec(path, null);

            IList<Changelist> recentChanges = rep.GetChangelists(options, depotFile);

            string[] recentIds = new string[maxItems];
            int it = 0;
            foreach (var c in recentChanges)
            {
                recentIds[it] = c.Id.ToString();
                ++it;
            }

            string idFilePath = "RecentIds.txt";
            List<string> unsyncedIds = new List<string>(5);

            if (System.IO.File.Exists(idFilePath))
            {
                string[] recentIdsFile = System.IO.File.ReadAllLines(idFilePath);

                int equalIndex = recentIds.Length;

                for (int i = 0; i < recentIds.Length; ++i)
                {
                    for (int j = 0; j < recentIdsFile.Length; ++j)
                    {
                        if (Convert.ToInt32(recentIds[j]) > Convert.ToInt32(recentIdsFile[i]))
                        {
                            continue;
                        }  
                        else if (Convert.ToInt32(recentIds[j]) == Convert.ToInt32(recentIdsFile[i]))
                        {
                            equalIndex = j;
                            break;
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Equal not found, changes possibly missed > Consider increasing maxItems");
                        }
                    }
                    if (equalIndex < recentIds.Length)
                    {
                        break;
                    }
                }

                for (int i = 0; i < equalIndex; ++i)
                {
                    unsyncedIds.Add(recentIds[i]);
                }
            }
            else
            {
                unsyncedIds.AddRange(recentIds);
            }

            List<Changelist> unsyncedLists = new List<Changelist>(unsyncedIds.Count);
            if (unsyncedIds.Count > 0)
            {
                System.IO.File.WriteAllLines(idFilePath, unsyncedIds);

                foreach (string id in unsyncedIds)
                {
                    Changelist cl = rep.GetChangelist(Convert.ToInt32(id));
                    unsyncedLists.Add(cl);
                }

                unsyncedLists.Sort((x, y) => x.Id.CompareTo(y.Id));
            }

            return unsyncedLists;
        }

        static IList<DepotFileDiff> GetFileDiffs(Repository rep, IList<Changelist> changelists, GetDepotFileDiffsCmdFlags flags = GetDepotFileDiffsCmdFlags.None)
        {
            List<DepotFileDiff> fileDiffs = new List<DepotFileDiff>(changelists.Count);

            foreach (Changelist cl in changelists)
            {
                foreach (FileMetaData f in cl.Files)
                {
                    FileSpec oldRev = new FileSpec(f.DepotPath, new Revision(f.HeadRev - 1));
                    FileSpec newRev = new FileSpec(f.DepotPath, new Revision(f.HeadRev));

                    GetDepotFileDiffsCmdOptions opts = new GetDepotFileDiffsCmdOptions(flags, 0, 0, null, null, null);
                    IList<DepotFileDiff> diff = rep.GetDepotFileDiffs(oldRev.ToEscapedString(), newRev.ToEscapedString(), opts);

                    fileDiffs.AddRange(diff);
                }
            }

            return fileDiffs;
        }

        static void ParseFileDiffs(IList<DepotFileDiff> diffs)
        {

        }

        static void SendDiscordWebhook(IList<Changelist> changeListsToSend)
        {
            foreach (Changelist changeList in changeListsToSend)
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
}
