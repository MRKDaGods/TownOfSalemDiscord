using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TownOfSalemDiscord.Roles;

namespace TownOfSalemDiscord
{
    public enum NightResult
    {
        None = 0,
        Jailed = 1,
        Framed = 2,
        Controlled = 4,
        Transported = 8,
        Hexed = 16,
        AttackedByMafia = 32,
        AttackedBySerialKiller = 64,
        MauledByWerewolf = 128,
        IncineratedByArsonist = 256,
        Bugged = 512,
        Guarded = 1024,
        Healed = 2048
    }

    public enum GameState
    {
        InLobby,
        Nickname,
        Roles,
        Prepare,
        Started,
        Ended
    }

    public enum GameMode
    {
        None,
        Classic,
        RankedPractice,
        Custom1,
        EscortFestival,
        RolesTest
    }

    public class PlayerInfo
    {
        public string OldName;
        public string Name;
        public DiscordMember Member;
        public Role ChosenRole;
        public Role Role;
        public BaseRole RoleObject;
        public DiscordChannel House;
        public int Index;
        public int VotedIndex = -1;
        public NightResult NightRes;
        public NightInfo NightInfo;
    }

    public enum NightFlags
    {
        None = 0,
        Jailed = 1,
        Roleblocked = 2,
        Cleaned = 4,
        Forged = 8
    }

    public class NightInfo
    {
        NightFlags m_Flags;
        public List<PlayerInfo> Visitors = new List<PlayerInfo>();
        public List<PlayerInfo> DoctorsLeft = new List<PlayerInfo>();
        public List<PlayerInfo> BGsLeft = new List<PlayerInfo>();
        public List<PlayerInfo> AttackersLeft = new List<PlayerInfo>();

        public bool Jailed
        {
            get
            {
                return (m_Flags & NightFlags.Jailed) != 0;
            }
            set
            {
                if (value)
                {
                    if ((m_Flags & NightFlags.Jailed) == 0)
                        m_Flags |= NightFlags.Jailed;
                }
                else
                {
                    if ((m_Flags & NightFlags.Jailed) != 0)
                        m_Flags &= NightFlags.Jailed;
                }
            }
        }
        public bool Roleblocked
        {
            get
            {
                return (m_Flags & NightFlags.Roleblocked) != 0;
            }
            set
            {
                if (value)
                {
                    if ((m_Flags & NightFlags.Roleblocked) == 0)
                        m_Flags |= NightFlags.Roleblocked;
                }
                else
                {
                    if ((m_Flags & NightFlags.Roleblocked) != 0)
                        m_Flags &= NightFlags.Roleblocked;
                }
            }
        }
        public bool Cleaned
        {
            get
            {
                return (m_Flags & NightFlags.Cleaned) != 0;
            }
            set
            {
                if (value)
                {
                    if ((m_Flags & NightFlags.Cleaned) == 0)
                        m_Flags |= NightFlags.Cleaned;
                }
                else
                {
                    if ((m_Flags & NightFlags.Cleaned) != 0)
                        m_Flags &= NightFlags.Cleaned;
                }
            }
        }
        public bool Forged
        {
            get
            {
                return (m_Flags & NightFlags.Forged) != 0;
            }
            set
            {
                if (value)
                {
                    if ((m_Flags & NightFlags.Forged) == 0)
                        m_Flags |= NightFlags.Forged;
                }
                else
                {
                    if ((m_Flags & NightFlags.Forged) != 0)
                        m_Flags &= NightFlags.Forged;
                }
            }
        }
    }

    public enum VoteType
    {
        Abstain,
        Innocent,
        Guilty
    }

    public class DayInfo
    {
        public bool Blackmailed;
        public int VotesAgainst;
        public Dictionary<PlayerInfo, VoteType> Voters = new Dictionary<PlayerInfo, VoteType>();

        public void ResetInfo()
        {
            VotesAgainst = 0;
            Voters.Clear();
        }
    }

    public struct DeathInfo
    {
        public Role Attacker;
        public PlayerInfo Target;
        public DeathFlags Flags;
    }

    public enum Role
    {
        None,

        //Town
        BodyGuard,
        Doctor,
        Escort,
        Investigator,
        Jailor,
        Lookout,
        Mayor,
        Medium,
        Retributionist,
        Sheriff,
        Spy,
        Transporter,
        Veteran,
        Vigilante,
        VampireHunter,

        //Mafia
        Blackmailer,
        Consigliere,
        Consort,
        Disguiser,
        Framer,
        Forger,
        Godfather,
        Janitor,
        Mafioso,

        //Neutral
        Amnesiac,
        Arsonist,
        Executioner,
        Jester,
        SerialKiller,
        Survivor,
        Witch,
        Werewolf,
        Vampire,

        //Coven
        CovenLeader,
        Poisoner,
        PotionMaster,
        HexMaster,
        Necromancer,
        Medusa,

        //Unknown
        TownKilling,
        RandomTown,

        //Misc
        Cleaned,
        Stoned,
        Trial,

        //Custom
        Dragon,
        MRK
    }

    public enum VoteState
    {
        None,
        Primary,
        Secondary
    }

    public enum DayState
    {
        Announcing,
        Discussion,
        Voting,
        Night
    }

    public class TownOfSalem
    {
        const ulong DAY_CHANNEL = 483003487278596097;
        const ulong MAFIA_CHANNEL = 483003669785214976;
        const ulong COVEN_CHANNEL = 483003693617381386;
        const ulong VAMPIRE_CHANNEL = 483003756519096332;
        const ulong JAIL_CHANNEL = 483003831051878422;
        const ulong GRAVEYARD_CHANNEL = 491845960545075210;
        const string NA = "N/A";

        static ulong[] HOUSE_CHANNELS = new ulong[15] { 483005362266439691, 483005376841908245, 483005521029496863, 483005548208586763, 483005567632408576,
            483005588045955073, 483005603808018442, 483005624767086608, 483005641347170304, 483005665057439744, 483005683965624320,
            483005705083682816, 483005722930577410, 483005741796425728, 483005762722070541 };
        static string[] DEFAULT_NAMES = new string [15] { "Giles Corey", "Jonathan Corwin", "Deodat Lawson", "Betty Parris", "Martha Corey", "Abigail Hobbs",
            "Sarah Bishop", "John Hathorne", "MRK", "MRK's FRIEND", "Joe", "Britt", "Giles or Deodat", "Deodat is better", "Giles is better" };
            public static string[] VOTE_TARGETS = { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve",
                "thirteen", "fourteen", "fifteen" };
        static Role[] CLASSIC_LIST = { Role.Sheriff, Role.Doctor, Role.Investigator, Role.Jailor, Role.Medium, Role.Godfather, Role.Framer,
            Role.Executioner, Role.Escort, Role.Mafioso, Role.Lookout, Role.SerialKiller, Role.TownKilling, Role.RandomTown };
        static Role[] TOWN_ROLES = { Role.BodyGuard, Role.Doctor, Role.Escort, Role.Investigator, Role.Jailor, Role.Lookout, Role.Mayor,
            Role.Medium, Role.Retributionist, Role.Sheriff, Role.Spy, Role.Transporter, Role.VampireHunter, Role.Veteran, Role.Vigilante };
        static Role[] WEIRD_LIST = { Role.CovenLeader, Role.Jailor, Role.RandomTown, Role.Godfather, Role.HexMaster, Role.Disguiser, Role.SerialKiller };
        static Role[] ROLES_DBG_LIST = new Role[15] { Role.Godfather, Role.Janitor, Role.BodyGuard, Role.Mayor, Role.Mafioso, Role.Janitor, Role.Escort, Role.Mayor, Role.Escort, Role.Mafioso, Role.Janitor, Role.Mayor, Role.Janitor, Role.Jailor, Role.BodyGuard };
        static Role[] ESCORT_FEST_LIST = { Role.Escort, Role.Escort, Role.Escort, Role.Escort, Role.Escort, Role.Escort, Role.Escort, Role.Escort, Role.Escort,
            Role.Escort, Role.Escort, Role.Escort, Role.Escort, Role.Escort, Role.Escort };

        GameState m_State;
        GameMode m_Mode;
        List<DiscordUser> m_Players;
        int m_MinimumPlayerCount;
        List<PlayerInfo> m_RuntimePlayers;
        List<string> m_NamesLeft;
        List<Role> m_RoleList;
        Random m_Rand;
        int m_TimeOffset;
        List<PlayerInfo> m_AlivePlayers;
        List<PlayerInfo> m_DeadPlayers;
        List<PlayerInfo> m_MafiaMembers;
        List<PlayerInfo> m_CovenMembers;
        List<PlayerInfo> m_TownMembers;
        DiscordMessage m_VoteMessage;
        DiscordMessage m_SecondaryVoteMessage;
        Dictionary<string, PlayerInfo> m_VoteTargets;
        Dictionary<PlayerInfo, DayInfo> m_DayInfo;
        VoteState m_VoteState;
        DayState m_DayState;
        bool m_PrimaryVotesDirty = true;
        bool m_SecondaryVotesDirty;
        int m_VotedIndex;
        PlayerInfo m_LastVoter;
        DiscordMessage m_GraveyardMessage;
        List<DeathInfo> m_QueuedDeaths;
        List<PlayerInfo> m_NightRolePriorities;
        Thread m_GameThread;

        static DiscordChannel ms_DayChannel;
        static DiscordChannel ms_MafiaChannel;
        static DiscordChannel ms_CovenChannel;
        static DiscordChannel ms_VampireChannel;
        static DiscordChannel ms_JailChannel;
        static DiscordChannel ms_GraveyardChannel;
        static DiscordChannel[] ms_HouseChannels;
        static DiscordRole[] ms_Roles;

        public List<DiscordUser> Players { get { return m_Players; } }
        public int MinimumPlayerCount { get { return m_MinimumPlayerCount; } }
        public List<Role> RoleList { get { return m_RoleList; } }
        public bool CanAddPlayer { get { return m_Players.Count < 15; } }
        public GameState State { get { return m_State; } }
        public List<PlayerInfo> AlivePlayers { get { return m_AlivePlayers; } }
        public List<PlayerInfo> RuntimePlayers { get { return m_RuntimePlayers; } }
        public GameMode Mode { get { return m_Mode; } }
        public DayState DayState { get { return m_DayState; } }
        public static DiscordChannel DayChannel { get { return ms_DayChannel; } }

        static void StaticInitChannels(DiscordGuild guild)
        {
            ms_DayChannel = guild.GetChannel(DAY_CHANNEL);
            ms_MafiaChannel = guild.GetChannel(MAFIA_CHANNEL);
            ms_CovenChannel = guild.GetChannel(COVEN_CHANNEL);
            ms_VampireChannel = guild.GetChannel(VAMPIRE_CHANNEL);
            ms_JailChannel = guild.GetChannel(JAIL_CHANNEL);
            ms_GraveyardChannel = guild.GetChannel(GRAVEYARD_CHANNEL);
            ms_HouseChannels = new DiscordChannel[15];
            for (int i = 0; i < 15; i++)
                ms_HouseChannels[i] = guild.GetChannel(HOUSE_CHANNELS[i]);
        }
        public static void StaticInit(DiscordGuild guild)
        {
            StaticInitChannels(guild);
            ms_Roles = new DiscordRole[15];
            foreach (DiscordRole role in guild.Roles)
            {
                if (role.Name.IndexOf("#") == 0)
                    ms_Roles[int.Parse(role.Name.Substring(1)) - 1] = role;
            }
        }

        public TownOfSalem(GameMode mode)
        {
            m_State = GameState.InLobby;
            m_Players = new List<DiscordUser>();
            m_RuntimePlayers = new List<PlayerInfo>();
            m_MafiaMembers = new List<PlayerInfo>();
            m_CovenMembers = new List<PlayerInfo>();
            m_AlivePlayers = new List<PlayerInfo>();
            m_TownMembers = new List<PlayerInfo>();
            m_DeadPlayers = new List<PlayerInfo>();
            m_QueuedDeaths = new List<DeathInfo>();
            m_NightRolePriorities = new List<PlayerInfo>();
            m_NamesLeft = new List<string>();
            m_VoteTargets = new Dictionary<string, PlayerInfo>();
            m_DayInfo = new Dictionary<PlayerInfo, DayInfo>();
            foreach (string nm in DEFAULT_NAMES)
                m_NamesLeft.Add(nm);
            m_Mode = mode;
            m_Rand = new Random();
            switch (m_Mode)
            {
                case GameMode.RankedPractice:
                    m_MinimumPlayerCount = 15;
                    break;
                case GameMode.Classic:
                    m_RoleList = CLASSIC_LIST.ToList();
                    m_MinimumPlayerCount = 3;
                    break;
                case GameMode.Custom1:
                    m_RoleList = ROLES_DBG_LIST.ToList();
                    m_MinimumPlayerCount = 5;
                    break;
                case GameMode.RolesTest:
                    m_RoleList = ROLES_DBG_LIST.ToList();
                    m_MinimumPlayerCount = 14;
                    break;
                case GameMode.EscortFestival:
                    m_RoleList = ESCORT_FEST_LIST.ToList();
                    m_MinimumPlayerCount = 2;
                    break;
                default:
                    m_MinimumPlayerCount = 2;
                    break;
            }
        }
        async Task SendVoteMsg(string voteMsg)
        {
            m_VoteMessage = await ms_DayChannel.SendMessageAsync(voteMsg);
            m_VoteState = VoteState.Primary;
            foreach (PlayerInfo info in m_AlivePlayers)
                await m_VoteMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, $":{VOTE_TARGETS[info.Index]}:"));
        }
        async Task UpdateGame()
        {
            m_GraveyardMessage = await ms_GraveyardChannel.SendMessageAsync("__**GRAVEYARD**__");
            string voteMsg;
            while (m_State != GameState.Ended)
            {
                foreach (PlayerInfo info in m_RuntimePlayers)
                    m_DayInfo[info] = new DayInfo();
                m_TimeOffset++;
                m_DayState = DayState.Announcing;
                m_VoteState = VoteState.None;
                await ms_DayChannel.SendMessageAsync("The day is starting at the TownOfSalem");
                await Task.Delay(5000);
                foreach (DeathInfo dinfo in m_QueuedDeaths)
                {
                    await ms_DayChannel.SendMessageAsync($"We found **{dinfo.Target.Name}** dead in their home last night");
                    await Task.Delay(3000);
                    string dthMsg = "They were killed by an unknown role";
                    switch (dinfo.Attacker)
                    {
                        case Role.Godfather:
                        case Role.Mafioso:
                            dthMsg = "They were killed by **a member of the Mafia**.";
                            break;
                        case Role.Arsonist:
                            dthMsg = "They were incinerated by an **Arsonist**.";
                            break;
                        case Role.SerialKiller:
                            dthMsg = "They were stabbed by a **Serial Killer**.";
                            break;
                        case Role.Werewolf:
                            dthMsg = "They were mauled by a **Werewolf**.";
                            break;
                        case Role.BodyGuard:
                            dthMsg = dinfo.Target.Role == Role.BodyGuard ? "They died guarding someone." :
                                "They were killed by a **BodyGuard**.";
                            break;
                        case Role.CovenLeader:
                            dthMsg = "They were drained by the **Coven Leader**.";
                            break;
                        case Role.HexMaster:
                            dthMsg = "They were hexed by the **Hex Master**";
                            break;
                        case Role.Jailor:
                            dthMsg = "They were executed by the **Jailor**.";
                            break;
                        case Role.Medusa:
                            dthMsg = "They were turned into stone by the **Medusa**.";
                            break;
                        case Role.Veteran:
                        case Role.Vigilante:
                            dthMsg = $"They were shot by a **{dinfo.Attacker}**.";
                            break;
                        case Role.Dragon:
                            dthMsg = "They were torn by a **Dragon**.";
                            break;
                    }
                    await ms_DayChannel.SendMessageAsync(dthMsg);
                    await Task.Delay(3000);
                    await ms_DayChannel.SendMessageAsync("We could not find a last will.");
                    await Task.Delay(3000);
                    Role role = dinfo.Target.Role;
                    if ((dinfo.Flags & DeathFlags.Cleaned) != 0)
                        role = Role.Cleaned;
                    else if ((dinfo.Flags & DeathFlags.Stoned) != 0)
                        role = Role.Stoned;
                    string msg = $"**{dinfo.Target.Name}**'s role was **{dinfo.Target.Role}**";
                    if (role == Role.Cleaned || role == Role.Stoned)
                        msg = $"We could not determine **{dinfo.Target.Name}**'s role.";
                    await ms_DayChannel.SendMessageAsync(msg);
                    await HandlePlayerDeath(dinfo.Target);
                    await Task.Delay(3000);
                }
                if (m_QueuedDeaths.Count > 0)
                    await Task.Delay(5000);
                m_QueuedDeaths.Clear();
                if (m_State == GameState.Ended)
                    break;
                m_DayState = DayState.Discussion;
                foreach (PlayerInfo info in m_AlivePlayers)
                    await info.RoleObject.OnDayStart();
                int dmillsLeft = m_TimeOffset > 1 ? 60000 : 10000;
                string nxtEv = m_TimeOffset > 1 ? "voting" : "night";
                await ms_DayChannel.SendMessageAsync($"**Day** _{m_TimeOffset}_\n__**Discussion**__ Phase\n**{dmillsLeft / 1000}** seconds remaining till **{nxtEv}** starts!");
                while (dmillsLeft > 0)
                {
                    foreach (PlayerInfo info in m_AlivePlayers)
                        await info.RoleObject.OnDayUpdate();
                    if (dmillsLeft % 1000 == 0)
                    {
                        switch (dmillsLeft)
                        {
                            case 15000:
                            case 10000:
                            case 5000:
                                await ms_DayChannel.SendMessageAsync($"**{dmillsLeft / 1000}** seconds till **{nxtEv}** starts!");
                                break;
                        }
                    }
                    dmillsLeft -= 1000;
                    await Task.Delay(1000);
                }
                if (m_TimeOffset > 1)
                {
                    m_DayState = DayState.Voting;
                    m_VoteState = VoteState.Primary;
                    await ms_DayChannel.SendMessageAsync($"__**Voting**__ Phase\n**30** seconds remaining till voting ends!");
                    int votesNeeded = (int)Math.Floor(((decimal)m_AlivePlayers.Count) / 2) + 1;
                    voteMsg = $"{votesNeeded} votes are needed to lynch someone.\nClick to **vote**!";
                    await SendVoteMsg(voteMsg);
                    int voteMillsEnd = 30000;
                    while (voteMillsEnd > 0)
                    {
                        foreach (PlayerInfo info in m_AlivePlayers)
                            await info.RoleObject.OnDayUpdate();
                        if (m_PrimaryVotesDirty)
                        {
                            Console.WriteLine("V dirty");
                            m_PrimaryVotesDirty = false;
                            string msg = "";
                            foreach (PlayerInfo info in m_AlivePlayers)
                            {
                                string xms = info.VotedIndex == -1 ? NA : m_RuntimePlayers[info.VotedIndex].Name;
                                msg += $"**{info.Name}** has voted against **{xms}**.\n";
                            }
                            msg += voteMsg;
                            await m_VoteMessage.ModifyAsync(msg);
                        }
                        bool voteEnd = false;
                        while (!voteEnd)
                        {
                            int pIdx = 0;
                            string baseSecondMsg = "";
                            foreach (KeyValuePair<PlayerInfo, DayInfo> infpair in m_DayInfo)
                            {
                                if (infpair.Value.VotesAgainst >= votesNeeded)
                                {
                                    m_VoteState = VoteState.Secondary;
                                    m_VotedIndex = infpair.Key.Index;
                                    await ms_DayChannel.SendMessageAsync($"The town has decided to put **{infpair.Key.Name}** on trial\n**What is your Defence?**");
                                    int milsLeft = 15000;
                                    DiscordMessage timeLeftMsg = await ms_DayChannel.SendMessageAsync($"**15** seconds left!");
                                    while (milsLeft > 0)
                                    {
                                        foreach (PlayerInfo info in m_AlivePlayers)
                                            await info.RoleObject.OnDayUpdate();
                                        await timeLeftMsg.ModifyAsync($"**{milsLeft / 1000}** seconds left!");
                                        await Task.Delay(1000);
                                        milsLeft -= 1000;
                                    }
                                    m_SecondaryVoteMessage = await ms_DayChannel.SendMessageAsync("Click to vote!");
                                    await m_SecondaryVoteMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, ":negative_squared_cross_mark:"));
                                    await m_SecondaryVoteMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, ":white_check_mark:"));
                                    milsLeft = 15000;
                                    while (milsLeft > 0)
                                    {
                                        foreach (PlayerInfo info in m_AlivePlayers)
                                            await info.RoleObject.OnDayUpdate();
                                        if (m_SecondaryVotesDirty)
                                        {
                                            m_SecondaryVotesDirty = false;
                                            baseSecondMsg += $"**{m_LastVoter.Name}** has voted.\n";
                                            await m_SecondaryVoteMessage.ModifyAsync(baseSecondMsg + "Click to vote!");
                                        }
                                        await timeLeftMsg.ModifyAsync($"**{milsLeft / 1000}** seconds left!");
                                        await Task.Delay(1000);
                                        milsLeft -= 1000;
                                    }
                                    List<KeyValuePair<PlayerInfo, VoteType>> innos = new List<KeyValuePair<PlayerInfo, VoteType>>();
                                    List<KeyValuePair<PlayerInfo, VoteType>> abstains = new List<KeyValuePair<PlayerInfo, VoteType>>();
                                    List<KeyValuePair<PlayerInfo, VoteType>> guilties = new List<KeyValuePair<PlayerInfo, VoteType>>();
                                    string voteResMsg = "";
                                    if (infpair.Value.Voters == null)
                                        infpair.Value.Voters = new Dictionary<PlayerInfo, VoteType>();
                                    int innoOffset = 0, guiltyOffset = 0;
                                    foreach (KeyValuePair<PlayerInfo, VoteType> votePair in infpair.Value.Voters)
                                    {
                                        bool mayor = votePair.Key.Role == Role.Mayor && (votePair.Key.RoleObject as Mayor).Revealed;
                                        string votem = "";
                                        switch (votePair.Value)
                                        {
                                            case VoteType.Abstain:
                                                votem = "abstained";
                                                abstains.Add(votePair);
                                                break;
                                            case VoteType.Guilty:
                                                votem = "voted guilty";
                                                if (mayor)
                                                    guiltyOffset += 2;
                                                guilties.Add(votePair);
                                                break;
                                            case VoteType.Innocent:
                                                votem = "voted innocent";
                                                if (mayor)
                                                    innoOffset += 2;
                                                innos.Add(votePair);
                                                break;
                                        }
                                        voteResMsg += $"**{votePair.Key.Name}** has __**{votem}**__.\n";
                                    }
                                    voteResMsg += $"Votes have came to **{guilties.Count + guiltyOffset}** : **{innos.Count + innoOffset}**";
                                    await ms_DayChannel.SendMessageAsync(voteResMsg);
                                    if (guilties.Count + guiltyOffset > innos.Count + innoOffset)
                                    {
                                        await ms_DayChannel.SendMessageAsync($"**{infpair.Key.Name}** any last words?");
                                        milsLeft = 10000;
                                        while (milsLeft > 0)
                                        {
                                            foreach (PlayerInfo info in m_AlivePlayers)
                                                await info.RoleObject.OnDayUpdate();
                                            await Task.Delay(1000);
                                            milsLeft -= 1000;
                                        }
                                        await ms_DayChannel.SendMessageAsync($"May god have mercy on your soul, **{infpair.Key.Name}**");
                                        await Task.Delay(5000);
                                        await ms_DayChannel.SendMessageAsync($"We could not find a last will.\n**{infpair.Key.Name}** role was {infpair.Key.Role.ToString()}");
                                        await HandlePlayerDeath(infpair.Key, true);
                                        voteEnd = true;
                                        infpair.Value.ResetInfo();
                                    }
                                    else
                                    {
                                        infpair.Value.ResetInfo();
                                        infpair.Key.VotedIndex = -1;
                                        await ms_DayChannel.SendMessageAsync("The town may vote against another person now");
                                        await SendVoteMsg(voteMsg);
                                        m_PrimaryVotesDirty = true;
                                    }
                                }
                                else
                                {
                                    if (pIdx == m_AlivePlayers.Count - 1)
                                        voteEnd = true;
                                }
                                pIdx++;
                            }
                        }
                        await Task.Delay(500);
                        voteMillsEnd -= 500;
                        if (voteMillsEnd % 1000 == 0)
                        {
                            switch (voteMillsEnd)
                            {
                                case 15000:
                                case 10000:
                                case 5000:
                                    await ms_DayChannel.SendMessageAsync($"**{voteMillsEnd / 1000}** seconds till night starts!");
                                    break;
                            }
                        }
                    }
                }
                foreach (PlayerInfo info in m_AlivePlayers)
                    await info.RoleObject.OnDayEnd();
                m_DayState = DayState.Night;
                await ms_DayChannel.SendMessageAsync("The night is falling on the TownOfSalem.");
                await Task.Delay(5000);
                await ms_DayChannel.SendMessageAsync($"**Night** _{m_TimeOffset}_\n**30** seconds remaining till day!");
                foreach (PlayerInfo info in m_AlivePlayers)
                {
                    info.NightInfo = new NightInfo();
                }
                int jailorIdx = m_AlivePlayers.FindIndex(x => x.Role == Role.Jailor);
                if (jailorIdx != -1)
                    await m_AlivePlayers[jailorIdx].RoleObject.OnNightStart();
                int _pIdx = 0;
                foreach (PlayerInfo info in m_AlivePlayers)
                {
                    if (info.RoleObject != null && _pIdx != jailorIdx)
                        await info.RoleObject.OnNightStart();
                    _pIdx++;
                }
                int nmillsLeft = 30000;
                while (nmillsLeft > 0)
                {
                    foreach (PlayerInfo info in m_AlivePlayers)
                    {
                        if (info.RoleObject != null)
                            await info.RoleObject.OnNightUpdate();
                    }
                    if (nmillsLeft % 1000 == 0)
                    {
                        switch (nmillsLeft)
                        {
                            case 15000:
                            case 10000:
                            case 5000:
                                await ms_DayChannel.SendMessageAsync($"**{nmillsLeft / 1000}** seconds till day starts!");
                                break;
                        }
                    }
                    nmillsLeft -= 500;
                    await Task.Delay(500);
                    if (nmillsLeft == 0)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            foreach (PlayerInfo info in m_AlivePlayers)
                            {
                                switch (i)
                                {
                                    case 0:
                                        if (info.RoleObject != null)
                                            info.RoleObject.RegisterVisits();
                                        break;
                                    case 1:
                                        info.NightInfo.Visitors.Sort((x, y) =>
                                        {
                                            if (x.RoleObject == null || y.RoleObject == null)
                                                return -1;
                                            return x.RoleObject.RoleExecutionPriority.CompareTo(y.RoleObject.RoleExecutionPriority);
                                        });
                                        break;
                                }
                            }
                        }
                        m_AlivePlayers.CopyTo(m_NightRolePriorities);
                        m_NightRolePriorities.Sort((x, y) =>
                        {
                            if (x.RoleObject == null || y.RoleObject == null)
                                return -1;
                            return x.RoleObject.RoleExecutionPriority.CompareTo(y.RoleObject.RoleExecutionPriority);
                        });
                        foreach (PlayerInfo info in m_NightRolePriorities)
                        {
                            if (info.RoleObject != null)
                                await info.RoleObject.OnNightEnd();
                        }
                    }
                }
            }
        }
        public void QueueDeath(Role atk, PlayerInfo target, DeathFlags flags)
        {
            m_QueuedDeaths.Add(new DeathInfo { Attacker = atk, Target = target, Flags = flags });
        }
        async Task HandlePlayerDeath(PlayerInfo info, bool forceWin = false)
        {
            m_AlivePlayers.Remove(info);
            m_DeadPlayers.Add(info);
            string msg = "";
            foreach (PlayerInfo inf in m_DeadPlayers)
                msg += $"**{inf.Name}** (**{inf.Role}**)\n";
            msg += "__**GRAVEYARD**__";
            if ((m_QueuedDeaths.Count > 0 && info.Index == m_QueuedDeaths.Last().Target.Index) || forceWin)
            {
                List<PlayerInfo> playersWinning = new List<PlayerInfo>();
                //win conditions
                if (AllPlayersAreMafia())
                {
                    m_MafiaMembers.CopyTo(playersWinning);
                    await ms_DayChannel.SendMessageAsync("**MAFIA WINS**");
                }
                else if (AllPlayersAreCoven())
                {
                    m_CovenMembers.CopyTo(playersWinning);
                    await ms_DayChannel.SendMessageAsync("**COVEN WINS**");
                }
                else if (TownAdvantage())
                {
                    m_TownMembers.CopyTo(playersWinning);
                    await ms_DayChannel.SendMessageAsync("**TOWN WINS**");
                }
                else if (m_AlivePlayers.Count == 2)
                {
                    //2 players alive
                    //first, lets do a coven/maf/nk comparision with a townie(Jailor)
                    bool otherFactionWins = true;
                    PlayerInfo[] townies = GetAliveTownies();
                    PlayerInfo townie = null;
                    if (townies.Length == 1)
                    {
                        townie = townies[0];
                        if (townie.Role == Role.Jailor)
                        {
                            Jailor jailor = townie.RoleObject as Jailor;
                            if (jailor.ExecutionsLeft > 0 && !jailor.ExecutedTown)
                                otherFactionWins = false;
                        }
                        else if (townie.Role == Role.Transporter || townie.Role == Role.Veteran || townie.Role == Role.Mayor || townie.Role == Role.Vigilante)
                            otherFactionWins = false;
                    }
                    if (otherFactionWins)
                    {
                        if (townie != null)
                        {
                            PlayerInfo tinfo = m_AlivePlayers.Where(x => x.Index != townie.Index).ToArray()[0];
                            if (IsMafia(tinfo))
                            {
                                m_MafiaMembers.CopyTo(playersWinning);
                                await ms_DayChannel.SendMessageAsync("**MAFIA WINS**");
                            }
                            else if (IsCoven(tinfo))
                            {
                                m_CovenMembers.CopyTo(playersWinning);
                                await ms_DayChannel.SendMessageAsync("**COVEN WINS**");
                            }
                            else if (IsNK(tinfo))
                            {
                                playersWinning.Add(info);
                                await ms_DayChannel.SendMessageAsync($"**{tinfo.Role.ToString().ToUpper()} WINS**");
                            }
                        }
                        else
                        {
                            if (AllPlayersAreNK())
                                return;
                            Role[] winPattern = { Role.Werewolf, Role.Arsonist, Role.SerialKiller, Role.Dragon };
                            PlayerInfo winner = FindPlayerWithRole(Role.Werewolf);
                            int patternIdx = 1;
                            while (winner == null)
                            {
                                if (patternIdx == winPattern.Length)
                                    break;
                                winner = FindPlayerWithRole(winPattern[patternIdx]);
                                patternIdx++;
                            }
                            if (winner != null)
                            {
                                playersWinning.Add(winner);
                                await ms_DayChannel.SendMessageAsync($"**{winner.Role.ToString().ToUpper()} WINS**");
                            }
                        }
                    }
                }
                else if (m_AlivePlayers.Count == 1)
                {
                    PlayerInfo last = m_AlivePlayers[0];
                    if (IsNK(last))
                    {
                        playersWinning.Add(last);
                        await ms_DayChannel.SendMessageAsync($"**{last.Role.ToString().ToUpper()} WINS**");
                    }
                }
                if (playersWinning.Count > 0)
                {
                    string wmsg = "";
                    for (int i = 0; i < playersWinning.Count; i++)
                    {
                        wmsg += $"**{playersWinning[i].Name}**";
                        if (i < playersWinning.Count - 1)
                            wmsg += ", ";
                    }
                    await ms_DayChannel.SendMessageAsync(wmsg + " have won!");
                    await SwitchToState(GameState.Ended);
                }
            }
            await m_GraveyardMessage.ModifyAsync(msg);
        }
        bool TownAdvantage()
        {
            foreach (PlayerInfo info in m_AlivePlayers)
                if (IsMafia(info) || IsCoven(info) || (info.Role == Role.Witch && FindPlayersWithRole(Role.Vigilante).Length > 0) ||
                    IsNK(info) || info.Role == Role.Vampire)
                    return false;
            return GetAliveTownies().Length > 0;
        }
        PlayerInfo[] GetAliveTownies()
        {
            List<PlayerInfo> pl = new List<PlayerInfo>();
            foreach (PlayerInfo info in m_AlivePlayers)
                if (IsTown(info))
                    pl.Add(info);
            return pl.ToArray();
        }
        PlayerInfo FindPlayerWithRole(Role role)
        {
            return m_AlivePlayers.Find(x => x.Role == role);
        }
        PlayerInfo[] FindPlayersWithRole(Role role)
        {
            List<PlayerInfo> pl = new List<PlayerInfo>();
            foreach (PlayerInfo info in m_AlivePlayers)
                if (info.Role == role)
                    pl.Add(info);
            return pl.ToArray();
        }
        bool AllPlayersAreMafia()
        {
            foreach (PlayerInfo info in m_AlivePlayers)
                if (!IsMafia(info))
                    return false;
            return true;
        }
        bool AllPlayersAreCoven()
        {
            foreach (PlayerInfo info in m_AlivePlayers)
                if (!IsCoven(info))
                    return false;
            return true;
        }
        bool AllPlayersAreNK()
        {
            foreach (PlayerInfo info in m_AlivePlayers)
                if (!IsNK(info))
                    return false;
            return true;
        }
        bool IsNK(PlayerInfo info)
        {
            return info.RoleObject.Alignment == RoleAlignment.NeutralKilling;
        }
        public async Task FAKE_HandlePlayerDeath(Role role, string name)
        {
            m_DeadPlayers.Add(new PlayerInfo { Role = role, Name = name });
            string msg = "";
            foreach (PlayerInfo inf in m_DeadPlayers)
                msg += $"**{inf.Name}** (**{inf.Role}**)\n";
            msg += "__**GRAVEYARD**__";
            await m_GraveyardMessage.ModifyAsync(msg);
        }
        public async Task OnReactionAdded(MessageReactionAddEventArgs evt)
        {
            if (evt.User.IsBot || m_State != GameState.Started)
                return;
            Console.WriteLine("R added");
            switch (m_DayState)
            {
                case DayState.Voting:
                    switch (m_VoteState)
                    {
                        case VoteState.Primary:
                            if (evt.Message.Id != m_VoteMessage.Id)
                                return;
                            int idx = -1;
                            Console.WriteLine(evt.Emoji.Name);
                            for (int i = 0; i < VOTE_TARGETS.Length; i++)
                            {
                                if (evt.Emoji.Name.Contains((i + 1).ToString()) || evt.Emoji.Name == VOTE_TARGETS[i])
                                {
                                    idx = i;
                                    break;
                                }
                            }
                            Console.WriteLine("Determined IDx" + idx);
                            if (idx != -1)
                            {
                                PlayerInfo sinfo = AsPlayerInfo(await Bot.Instance.AsMember(evt.User));
                                int voteValue = 1;
                                if (sinfo.Role == Role.Mayor && (sinfo.RoleObject as Mayor).Revealed)
                                    voteValue = 3;
                                if (sinfo.VotedIndex != -1)
                                    m_DayInfo[m_RuntimePlayers[sinfo.VotedIndex]].VotesAgainst -= voteValue;
                                sinfo.VotedIndex = idx;
                                m_DayInfo[m_RuntimePlayers[sinfo.VotedIndex]].VotesAgainst += voteValue;
                                m_PrimaryVotesDirty = true;
                            }
                            break;
                        case VoteState.Secondary:
                            if (m_SecondaryVoteMessage != null && evt.Message.Id != m_SecondaryVoteMessage.Id)
                                return;
                            VoteType type = VoteType.Abstain;
                            if (evt.Emoji.Name == "❎")
                                type = VoteType.Innocent;
                            if (evt.Emoji.Name == "✅")
                                type = VoteType.Guilty;
                            PlayerInfo info = AsPlayerInfo(await Bot.Instance.AsMember(evt.User));
                            m_DayInfo[m_RuntimePlayers[m_VotedIndex]].Voters[info] = type;
                            m_SecondaryVotesDirty = true;
                            m_LastVoter = info;
                            break;
                    }
                    break;
            }
        }
        public async Task OnReactionRemoved(MessageReactionRemoveEventArgs evt)
        {

        }
        PlayerInfo AsPlayerInfo(DiscordMember member)
        {
            foreach (PlayerInfo info in m_RuntimePlayers)
                if (info.Member.Id == member.Id)
                    return info;
            return null;
        }
        public void SetMinToCurrent()
        {
            m_MinimumPlayerCount = m_Players.Count;
        }
        public async Task SwitchToState(GameState state)
        {
            m_State = state;
            switch (m_State)
            {
                case GameState.Nickname:
                    Bot.Instance.SendQueueMessage("**Setting nicknames...**");
                    foreach (DiscordUser user in m_Players)
                    {
                        int idx = m_Rand.Next(0, m_NamesLeft.Count - 1);
                        DiscordMember member = await Bot.Instance.AsMember(user);
                        string name = m_NamesLeft[idx];
                        m_RuntimePlayers.Add(new PlayerInfo { OldName = member.DisplayName, Name = name, Member = member });
                        m_NamesLeft.RemoveAt(idx);
                        await Bot.Instance.PmUser(user, $"Your name is **{name}**!");
                        await member.ModifyAsync($"{name} [{m_RuntimePlayers.Count}]");
                        Console.WriteLine(idx + ", " + name);
                    }
                    break;
                case GameState.Ended:
                    if (m_GameThread != null)
                        m_GameThread.Abort();
                    await Bot.Instance.UpdateServer();
                    StaticInitChannels(Bot.Server);
                    for (int idx = 0; idx < m_RuntimePlayers.Count; idx++)
                    {
                        PlayerInfo info = m_RuntimePlayers[idx];
                        await info.Member.ModifyAsync(info.OldName);
                        await info.Member.RevokeRoleAsync(ms_Roles[idx]);
                    }
                    List<DiscordOverwrite> mafOvrs = new List<DiscordOverwrite>();
                    foreach (DiscordOverwrite ovr in ms_MafiaChannel.PermissionOverwrites)
                    {
                        if (ovr.CheckPermission(Permissions.AddReactions) == PermissionLevel.Allowed)
                            continue;
                        Console.WriteLine("added ovr");
                        mafOvrs.Add(ovr);
                    }
                    foreach (DiscordOverwrite mafOvr in mafOvrs)
                        await ms_MafiaChannel.DeleteOverwriteAsync(mafOvr);
                    List<DiscordOverwrite> covenOvrs = new List<DiscordOverwrite>();
                    foreach (DiscordOverwrite ovr in ms_CovenChannel.PermissionOverwrites)
                    {
                        if (ovr.CheckPermission(Permissions.AddReactions) == PermissionLevel.Allowed)
                            continue;
                        Console.WriteLine("added covr");
                        covenOvrs.Add(ovr);
                    }
                    foreach (DiscordOverwrite covOvr in covenOvrs)
                        await ms_CovenChannel.DeleteOverwriteAsync(covOvr);                
                    break;
                case GameState.Roles:
                    List<PlayerInfo> playersle = new List<PlayerInfo>();
                    foreach (PlayerInfo pl in m_RuntimePlayers)
                        playersle.Add(pl);
                    for (int i = 0; i < m_RuntimePlayers.Count; i++)
                    {
                        int pIdx = m_Rand.Next(0, playersle.Count);
                        Role r = m_RoleList[i];
                        PlayerInfo inf = playersle[pIdx];
                        inf.ChosenRole = r;
                        switch (r)
                        {
                            case Role.RandomTown:
                                r = GetSuitableRT();
                                break;
                        }
                        BaseRole roleObj = null;
                        switch (r)
                        {
                            case Role.BodyGuard:
                                roleObj = new BodyGuard(inf);
                                break;
                            case Role.Godfather:
                                roleObj = new Godfather(inf);
                                break;
                            case Role.Mafioso:
                                roleObj = new Mafioso(inf);
                                break;
                            case Role.Escort:
                                roleObj = new Escort(inf);
                                break;
                            case Role.Jailor:
                                roleObj = new Jailor(inf);
                                break;
                            case Role.Janitor:
                                roleObj = new Janitor(inf);
                                break;
                            case Role.Mayor:
                                roleObj = new Mayor(inf);
                                break;
                            case Role.Dragon:
                                roleObj = new Dragon(inf);
                                break;
                        }
                        inf.RoleObject = roleObj;
                        inf.Role = r;
                        playersle.RemoveAt(pIdx);
                        await Bot.Instance.PmUser(await Bot.Instance.AsUser(inf.Member), $"Your role is **{r.ToString()}**!");
                    }
                    break;
                case GameState.Prepare:
                    string mafMsg = "";
                    string covenMsg = "";
                    for (int i = 0; i < m_RuntimePlayers.Count; i++)
                    {
                        DiscordRole targetRole = ms_Roles[i];
                        PlayerInfo info = m_RuntimePlayers[i];
                        m_VoteTargets[VOTE_TARGETS[i]] = info;
                        info.Index = i;
                        info.House = ms_HouseChannels[i];
                        await info.Member.GrantRoleAsync(targetRole);
                        if (IsMafia(info))
                        {
                            await ms_MafiaChannel.AddOverwriteAsync(targetRole, Permissions.ReadMessageHistory | Permissions.SendMessages | Permissions.AccessChannels, Permissions.None);
                            m_MafiaMembers.Add(info);
                            mafMsg += $"**{info.Name}**(__**{info.Role.ToString()}**__)\n";
                        }
                        else if (IsCoven(info))
                        {
                            await ms_CovenChannel.AddOverwriteAsync(targetRole, Permissions.ReadMessageHistory | Permissions.SendMessages | Permissions.AccessChannels, Permissions.None);
                            m_CovenMembers.Add(info);
                            covenMsg += $"**{info.Name}**(__**{info.Role.ToString()}**__)\n";
                        }
                        else if (IsTown(info))
                            m_TownMembers.Add(info);
                        m_AlivePlayers.Add(info);
                        await info.House.SendMessageAsync(null, false, new DiscordEmbedBuilder()
                            .WithTitle(info.Role.ToString())
                            .WithColor(DiscordColor.Red)
                            .WithDescription($"**Attack** __**{info.RoleObject.Attack}**__ **Defence** __**{info.RoleObject.Defence}**__")
                            .AddField("Alignment", $"**{info.RoleObject.Alignment}**")
                            .AddField("Abilities", $"**{info.RoleObject.Abilities}**")
                            .AddField("Attributes", $"**{info.RoleObject.Attributes}**"));
                    }
                    mafMsg += "__**MAFIA MEMBERS**__";
                    covenMsg += "__**COVEN MEMBERS**__";
                    foreach (PlayerInfo info in m_MafiaMembers)
                        await info.House.SendMessageAsync(mafMsg);
                    foreach (PlayerInfo info in m_CovenMembers)
                        await info.House.SendMessageAsync(covenMsg);
                    break;
                case GameState.Started:
                    (m_GameThread = new Thread(() =>
                    {
                        UpdateGame().ConfigureAwait(false).GetAwaiter().GetResult();
                    })).Start();
                    break;
            }
        }
        bool PlayerHasRole(Role r)
        {
            foreach (PlayerInfo info in m_RuntimePlayers)
                if (info.Role == r)
                    return true;
            return false;
        }
        Role GetSuitableRT()
        {
            List<Role> potential = new List<Role>();
            foreach (Role r in TOWN_ROLES)
                potential.Add(r);
            if (m_RoleList.Contains(Role.Jailor) || PlayerHasRole(Role.Jailor))
                potential.Remove(Role.Jailor);
            if (m_RoleList.Contains(Role.Mayor) || PlayerHasRole(Role.Mayor))
                potential.Remove(Role.Mayor);
            if (m_RoleList.Contains(Role.Retributionist) || PlayerHasRole(Role.Retributionist))
                potential.Remove(Role.Retributionist);
            if (m_RoleList.Contains(Role.Veteran) || PlayerHasRole(Role.Veteran))
                potential.Remove(Role.Veteran);
            return potential[m_Rand.Next(0, potential.Count - 1)];
        }
        public static bool IsMafia(PlayerInfo info)
        {
            switch (info.Role)
            {
                case Role.Blackmailer:
                case Role.Consigliere:
                case Role.Consort:
                case Role.Disguiser:
                case Role.Forger:
                case Role.Framer:
                case Role.Godfather:
                case Role.Janitor:
                case Role.Mafioso:
                    return true;
            }
            return false;
        }
        public static bool IsTown(PlayerInfo info)
        {
            foreach (Role role in TOWN_ROLES)
                if (info.Role == role)
                    return true;
            return false;
        }
        public static bool IsCoven(PlayerInfo info)
        {
            switch (info.Role)
            {
                case Role.CovenLeader:
                case Role.HexMaster:
                case Role.Medusa:
                case Role.Necromancer:
                case Role.Poisoner:
                case Role.PotionMaster:
                    return true;
            }
            return false;
        }
        public async Task SendMafiaMessage(string message)
        {
            await ms_MafiaChannel.SendMessageAsync(message);
        }
    }
}
