//#define UPDATE_SRC_CODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Threading;
using System.Reflection;
using System.IO;

namespace TownOfSalemDiscord
{
    public enum QueueEventType
    {
        Join,
        Leave
    }

    public struct QueueEvent
    {
        public QueueEventType Type;
        public DiscordUser User;
    }

    public class Bot
    {
        const ulong MRKDAGODS = 211276352437878785;
        const ulong QUEUE_CHANNEL = 482679682723086338;

        DiscordGuild m_Server;
        DiscordChannel m_QueueChannel;
        DiscordClient m_Client;
        TownOfSalem m_CurrentGame;
        DiscordMessage m_QueueMessage;
        List<QueueEvent> m_QueueEvents = new List<QueueEvent>();
        int m_GameTimer = 10;
        Thread m_TimerThread;
        bool m_TimerRunning;
        string m_RoleListMsg;
        Dictionary<ulong, AsyncEventHandler<MessageReactionAddEventArgs>> m_ExplicitReactionAddHandlers;
        List<ulong> m_BannedUsers;

        public static Bot Instance { get; private set; }
        public static DiscordGuild Server { get { return Instance.m_Server; } }
        public static DiscordClient Client { get { return Instance.m_Client; } }
        public TownOfSalem CurrentGame { get { return m_CurrentGame; } }

        public Bot()
        {
            Instance = this;
            m_BannedUsers = new List<ulong>();
            m_ExplicitReactionAddHandlers = new Dictionary<ulong, AsyncEventHandler<MessageReactionAddEventArgs>>();
            m_TimerThread = new Thread(() =>
            {
                while (m_TimerRunning)
                {
                    if (m_GameTimer > 0)
                        m_GameTimer--;
                    if ((m_GameTimer > 5 && m_GameTimer % 2 == 0) || m_GameTimer < 5)
                        Task.Run(UpdatePlayers);
                    Thread.Sleep(1000);
                }
            });
        }

        public void RegisterReactionAddHandler(ulong instanceId, AsyncEventHandler<MessageReactionAddEventArgs> handler)
        {
            if (!m_ExplicitReactionAddHandlers.ContainsKey(instanceId))
                m_ExplicitReactionAddHandlers[instanceId] = null;
            m_ExplicitReactionAddHandlers[instanceId] += handler;
        }
        public void UnregisterReactionAddHandler(ulong instanceId)
        {
            if (!m_ExplicitReactionAddHandlers.ContainsKey(instanceId))
                return;
            m_ExplicitReactionAddHandlers[instanceId] = null;
        }
        public async Task Init()
        {
            m_Client = new DiscordClient(new DiscordConfiguration
            {
                TokenType = TokenType.Bot,
                Token = ""
            });
            m_Client.MessageCreated += OnMessageReceived;
            m_Client.MessageReactionAdded += OnReactionAdded;
            m_Client.MessageReactionRemoved += OnReactionRemoved;
            Console.WriteLine("Connecting...");
            await m_Client.ConnectAsync();
            Console.WriteLine("Connected");
            await UpdateServer();
            await m_Client.UpdateStatusAsync(new DiscordGame("Developed by MRKDaGods"));
            TownOfSalem.StaticInit(m_Server);
#if UPDATE_SRC_CODE
            //post src code
            DiscordChannel srcCodeCateg = m_Server.GetChannel(492037876109017090);
            foreach (string file in Directory.GetFiles($@"C:\Users\{Environment.UserName}\Documents\Visual Studio 2015\Projects\TownOfSalemDiscord\TownOfSalemDiscord",
                "*.cs*", SearchOption.AllDirectories))
            {
                int startIdx = file.LastIndexOf('\\') + 1;
                string fileName = file.Substring(startIdx, file.LastIndexOf('.') - startIdx);
                DiscordChannel chnl = FindChannelFromName(fileName);
                if (chnl == null)
                    chnl = await m_Server.CreateChannelAsync(fileName, ChannelType.Text, srcCodeCateg);
                IReadOnlyList<DiscordMessage> msgs;
                while ((msgs = await chnl.GetMessagesAsync()).Count > 0)
                    await chnl.DeleteMessagesAsync(msgs);
                string txt = File.ReadAllText(file);
                List<string> strMsgs = new List<string>();
                string buffer = "";
                for (int i = 0; i < txt.Length; i++)
                {
                    buffer += txt[i];
                    if ((i + 1) % 1500 == 0) //2000 char limit per msg
                    {
                        int x = buffer.Length;
                        strMsgs.Add(buffer);
                        buffer = "";
                    }
                }
                if (buffer.Length > 0)
                    strMsgs.Add(buffer);
                foreach (string smsg in strMsgs)
                    await chnl.SendMessageAsync($"```cs\n{smsg}```");
            }
#endif
            //await m_Server.RevokeRoleAsync(await m_Server.GetMemberAsync(474006080993886228), m_Server.Roles.Where(x => x.Name == "Administrator").ToArray()[0], "nah");
            //await m_Server.UpdateRoleAsync(m_Server.Roles.Where(x => x.Name == "Administrator").ToArray()[0], null, null, DiscordColor.None);
            /*await Task.Run(async () =>
            {
                DiscordRole admin = m_Server.Roles.Where(x => x.Name == "Flicker").ToArray()[0];
                Random rand = new Random();
                PropertyInfo[] info = typeof(DiscordColor).GetProperties(BindingFlags.Static | BindingFlags.Public);
                while (true)
                {
                    await m_Server.UpdateRoleAsync(admin, null, null, (DiscordColor)info[rand.Next(0, info.Length - 1)].GetGetMethod().Invoke(null, new object[0]));
                    await Task.Delay(10);
                }
            });*/
            await Task.Delay(-1);
        }

        DiscordChannel FindChannelFromName(string name)
        {
            foreach (DiscordChannel channel in m_Server.Channels)
            {
                if (channel.Name == name)
                    return channel;
            }
            return null;
        }

        public async Task UpdateServer()
        {
            m_Server = await m_Client.GetGuildAsync(482674671242969098);
            m_QueueChannel = m_Server.GetChannel(QUEUE_CHANNEL);
        }

        async Task UpdatePlayers()
        {
            string msg = $"Gamemode: **{m_CurrentGame.Mode.ToString()}**\n__**{m_RoleListMsg}**__\n";
            foreach (QueueEvent evt in m_QueueEvents)
            {
                string cond = evt.Type == QueueEventType.Join ? "joined" : "left";
                msg += $"**{evt.User.Username}** has {cond} the game.\n";
            }
            if (m_CurrentGame.Players.Count >= m_CurrentGame.MinimumPlayerCount)
            {
                msg += $"\nGame starts in {m_GameTimer} seconds.\n";
                if (!m_TimerRunning)
                {
                    m_TimerThread.Start();
                    m_TimerRunning = true;
                }
            }
            else
            {
                m_GameTimer = 10;
                if (m_TimerRunning)
                {
                    msg += "\nStartup cancelled due to a player leaving.";
                }
                m_TimerRunning = false;
            }
            msg += $"{m_CurrentGame.Players.Count}/15\nClick to join";
            await m_QueueMessage.ModifyAsync(msg);
            if (m_GameTimer <= 0)
            {
                m_TimerRunning = false;
                m_GameTimer = 10;
                await m_QueueMessage.RespondAsync("Game started.");
                await m_CurrentGame.SwitchToState(GameState.Nickname);
                await m_CurrentGame.SwitchToState(GameState.Roles);
                await m_CurrentGame.SwitchToState(GameState.Prepare);
                await m_CurrentGame.SwitchToState(GameState.Started);
            }
        }

        async Task OnReactionAdded(MessageReactionAddEventArgs evt)
        {
            if (evt.Message == m_QueueMessage && evt.Emoji.Name == "tos" && m_CurrentGame != null && m_CurrentGame.State == GameState.InLobby
                && m_CurrentGame.CanAddPlayer && !evt.User.IsBot)
            {
                if (m_BannedUsers.Contains(evt.User.Id))
                {
                    await evt.Message.RespondAsync($"{evt.User.Mention}, you are **banned** from the game!");
                    return;
                }
                m_CurrentGame.Players.Add(evt.User);
                m_QueueEvents.Add(new QueueEvent { Type = QueueEventType.Join, User = evt.User });
                await UpdatePlayers();
            }
            if (m_CurrentGame != null)
                await m_CurrentGame.OnReactionAdded(evt);
            foreach (AsyncEventHandler<MessageReactionAddEventArgs> evth in m_ExplicitReactionAddHandlers.Values)
                if (evth != null)
                    await evth(evt);
        }

        async Task FakeJoin(DiscordUser user)
        {
            if (!m_CurrentGame.CanAddPlayer)
                return;
            m_CurrentGame.Players.Add(user);
            m_QueueEvents.Add(new QueueEvent { Type = QueueEventType.Join, User = user });
            await UpdatePlayers();
        }

        async Task OnReactionRemoved(MessageReactionRemoveEventArgs evt)
        {
            if (evt.Message == m_QueueMessage && evt.Emoji.Name == "tos" && m_CurrentGame != null)
            {
                m_CurrentGame.Players.Remove(evt.User);
                m_QueueEvents.Add(new QueueEvent { Type = QueueEventType.Leave, User = evt.User });
                await UpdatePlayers();
            }
            if (m_CurrentGame != null)
                await m_CurrentGame.OnReactionRemoved(evt);
        }

        async Task OnMessageReceived(MessageCreateEventArgs evt)
        {
            Console.Write("Reaceived\n");
            int cmdIdx = evt.Message.Content.IndexOf("/tos");
            if (cmdIdx == 0)
            {
                if (evt.Message.Author.Id != MRKDAGODS)
                {
                    await evt.Message.RespondAsync("Only MRKDaGods can use the /tos command");
                }
                else
                {
                    string[] args = evt.Message.Content.Split(' ');
                    if (args.Length > 1)
                    {
                        switch (args[1])
                        {
                            case "start":
                                if (m_CurrentGame != null)
                                    await evt.Message.RespondAsync("There is a game currently running");
                                else
                                {
                                    if (args.Length != 3)
                                    {
                                        await evt.Message.RespondAsync("Expected 3 args");
                                        break;
                                    }
                                    GameMode mode;
                                    if (!Enum.TryParse(args[2], out mode))
                                    {
                                        await evt.Message.RespondAsync("Invalid gamemode");
                                        break;
                                    }
                                    m_CurrentGame = new TownOfSalem(mode);
                                    m_RoleListMsg = "";
                                    for (int rIdx = 0; rIdx < m_CurrentGame.RoleList.Count; rIdx++)
                                        m_RoleListMsg += m_CurrentGame.RoleList[rIdx].ToString() + (rIdx == m_CurrentGame.RoleList.Count - 1 ? "" : "\n");
                                    await m_Client.SendMessageAsync(m_QueueChannel, "@everyone\nA new game has started");
                                    m_QueueMessage = await m_Client.SendMessageAsync(m_QueueChannel, "0/15\nClick to join");
                                    await UpdatePlayers();
                                    await m_QueueMessage.CreateReactionAsync(DiscordEmoji.FromName(m_Client, ":tos:"));
                                    await evt.Message.RespondAsync("Started game");
                                }
                                break;
                            case "stop":
                                if (m_CurrentGame == null)
                                    await evt.Message.RespondAsync("There is no games running at the moment");
                                else
                                {
                                    await m_CurrentGame.SwitchToState(GameState.Ended);
                                    m_CurrentGame = null;
                                    m_QueueEvents.Clear();
                                    await evt.Message.RespondAsync("Force stopped current game");
                                }
                                break;
                            case "purge":
                                if (args.Length == 3)
                                {
                                    await evt.Channel.DeleteMessagesAsync(await evt.Channel.GetMessagesAsync(int.Parse(args[2])));
                                }
                                break;
                            case "force":
                                if (m_CurrentGame == null)
                                {
                                    await evt.Message.RespondAsync("There is no games running at the moment");
                                    break;
                                }
                                m_CurrentGame.SetMinToCurrent();
                                await UpdatePlayers();
                                break;
                            case "atk":
                                if (m_CurrentGame == null)
                                    await evt.Message.RespondAsync("There is no games running at the moment");
                                if (args.Length == 4)
                                {
                                    Roles.AttackType atkt;
                                    if (!Enum.TryParse(args[2], out atkt))
                                    {
                                        await evt.Message.RespondAsync("Invalid attacktype");
                                        break;
                                    }
                                    Role atkrRole;
                                    if (!Enum.TryParse(args[3], out atkrRole))
                                    {
                                        await evt.Message.RespondAsync("Invalid attacker role");
                                        break;
                                    }
                                    await m_CurrentGame.AlivePlayers.Where(x => x.Member.Id == MRKDAGODS).ToArray()[0].RoleObject.
                                        PerformAttack(atkt, new PlayerInfo { Role = atkrRole, House = evt.Channel, Member = await evt.Guild.GetMemberAsync(MRKDAGODS) });
                                }
                                break;
                            case "roles":
                                if (evt.Author.Id != 397157728457588736 && evt.Author.Id != MRKDAGODS)
                                {
                                    await evt.Message.RespondAsync("u must be mrk/stormm bud");
                                    break;
                                }
                                if (m_CurrentGame == null)
                                {
                                    await evt.Message.RespondAsync("There is no games running at the moment");
                                    break;
                                }
                                DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                                        .WithTitle("Roles")
                                        .WithColor(DiscordColor.Cyan);
                                foreach (PlayerInfo info in m_CurrentGame.RuntimePlayers)
                                {
                                    builder.AddField($"**{info.Name}:** ", info.Role.ToString(), true);
                                }
                                await evt.Message.RespondAsync(null, false, builder.Build());
                                break;
                            case "join":
                                if (m_CurrentGame == null)
                                {
                                    await evt.Message.RespondAsync("There is no games running at the moment");
                                    break;
                                }
                                /*foreach (DiscordMember member in await m_Server.GetAllMembersAsync())
                                {
                                    if (member.Id == m_Client.CurrentUser.Id || member.IsBot)
                                        continue;
                                    if (!m_CurrentGame.CanAddPlayer)
                                        break;
                                    await FakeJoin(await AsUser(member));
                                }*/
                                if (evt.MentionedUsers.Count == 0)
                                    break;
                                await FakeJoin(evt.MentionedUsers[0]);
                                break;
                            case "ban":
                                if (evt.MentionedUsers.Count == 0)
                                    break;
                                DiscordUser usr = evt.MentionedUsers[0];
                                if (m_BannedUsers.Contains(usr.Id))
                                    break;
                                await evt.Message.RespondAsync($"**{usr.Username}** has been banned!");
                                m_BannedUsers.Add(usr.Id);
                                break;
                            case "fake_death":
                                if (m_CurrentGame == null)
                                {
                                    await evt.Message.RespondAsync("There is no games running at the moment");
                                    break;
                                }
                                try
                                {
                                    await m_CurrentGame.FAKE_HandlePlayerDeath((Role)Enum.Parse(typeof(Role), args[2]), args[3]);
                                }
                                catch
                                {
                                    await evt.Message.RespondAsync("**Error**");
                                }
                                break;
                            default:
                                await evt.Message.RespondAsync("Unknown arg");
                                break;
                        }
                    }
                    else await evt.Message.RespondAsync("Unknown arg");
                }
                await evt.Message.DeleteAsync();
            }
        }

        public void SendQueueMessage(string msg)
        {
            Task.Run(async () => await m_Client.SendMessageAsync(m_QueueChannel, msg));
        }

        public async Task<DiscordMember> AsMember(DiscordUser user)
        {
            return await m_Server.GetMemberAsync(user.Id);
        }

        public async Task<DiscordUser> AsUser(DiscordMember member)
        {
            return await m_Client.GetUserAsync(member.Id);
        }

        public async Task PmUser(DiscordUser user, string msg)
        {
            await (await m_Client.CreateDmAsync(user)).SendMessageAsync(msg);
        }
    }
}
