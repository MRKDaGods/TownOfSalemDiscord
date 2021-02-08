using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TownOfSalemDiscord.TownOfSalem;

namespace TownOfSalemDiscord.Roles
{
    public class Escort : BaseRole
    {
        DiscordMessage m_UpdateMessage;
        string m_EscortQueue;
        bool m_Dirty;

        public override int RoleExecutionPriority
        {
            get
            {
                return (int)RoleExecution.Roleblock;
            }
        }
        protected override string NightText
        {
            get
            {
                return "";
            }
        }
        public override DefenceType Defence
        {
            get
            {
                return DefenceType.None;
            }
        }
        public override string Abilities
        {
            get
            {
                return "Distract someone each night.";
            }
        }
        public override string Attributes
        {
            get
            {
                return "Distraction blocks your target from using their role's night ability.\n"
                    + "You are immune to role blocks.\n"
                    + "If you target a Serial Killer, they will attack you.";
            }
        }
        public override RoleAlignment Alignment
        {
            get
            {
                return RoleAlignment.TownSupport;
            }
        }
        protected override RoleEventFlags EventFlags
        {
            get
            {
                return RoleEventFlags.NightStart | RoleEventFlags.NightUpdate | RoleEventFlags.NightEnd;
            }
        }

        public Escort(PlayerInfo player) : base(player, typeof(Escort))
        {
        }
        new async Task OnNightStart()
        {
            m_Target = null;
            m_EscortQueue = "";
            m_Dirty = true;
            BotInstance.RegisterReactionAddHandler(this.InstanceId(), OnReactionAdded);
            m_UpdateMessage = await m_Player.House.SendMessageAsync("Click to choose a target!");
            if (!m_Player.NightInfo.Jailed)
                foreach (PlayerInfo info in BotInstance.CurrentGame.AlivePlayers)
                    await m_UpdateMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, $":{VOTE_TARGETS[info.Index]}:"));
        }
        new async Task OnNightUpdate()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                await m_UpdateMessage.ModifyAsync($"{m_EscortQueue}Click to choose a target!");
            }
        }
        new async Task OnNightEnd()
        {
            BotInstance.UnregisterReactionAddHandler(this.InstanceId());
            if (m_Target != null)
            {
                if (m_Target.NightInfo.Jailed)
                {
                    await m_Player.House.SendMessageAsync("**Your ability failed because your target was jailed!");
                    await m_Target.House.SendMessageAsync("**Someone tried to roleblock you but you were jailed!**");
                }
                else
                {
                    m_Target.NightInfo.Roleblocked = true;
                    if (m_Target.Role == Role.Witch || m_Target.Role == Role.Escort || m_Target.Role == Role.Consort || m_Target.Role == Role.Transporter)
                        await m_Target.House.SendMessageAsync("**Someone tried to roleblock you, but you are immune!**");
                    else await m_Target.House.SendMessageAsync("**Someone occupied your night. You were roleblocked!**");
                }
            }
        }
        async Task OnReactionAdded(MessageReactionAddEventArgs evt)
        {
            if (m_UpdateMessage == null)
                return;
            if (evt.User.IsBot || evt.Message.Id != m_UpdateMessage.Id)
                return;
            int idx = -1;
            for (int i = 0; i < VOTE_TARGETS.Length; i++)
            {
                if (evt.Emoji.Name.Contains((i + 1).ToString()) || evt.Emoji.Name == VOTE_TARGETS[i])
                {
                    idx = i;
                    break;
                }
            }
            if (idx != -1)
            {
                m_Target = BotInstance.CurrentGame.RuntimePlayers[idx];
                m_EscortQueue += $"You have decided to distract **{m_Target.Name}**\n";
                m_Dirty = true;
            }
            await Task.Delay(1);
        }
    }
}