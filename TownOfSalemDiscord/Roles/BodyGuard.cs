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
    public class BodyGuard : BaseRole
    {
        int m_VestsLeft;
        DiscordMessage m_UpdateMessage;
        string m_BgQueue;
        bool m_Dirty;

        public override int RoleExecutionPriority
        {
            get
            {
                return (int)RoleExecution.Guard;
            }
        }
        protected override string NightText
        {
            get
            {
                return $"You have __**{m_VestsLeft}**__ vests left";
            }
        }
        public override DefenceType Defence
        {
            get
            {
                return DefenceType.None;
            }
        }
        public bool IsVesting
        {
            get
            {
                return m_Target == m_Player;
            }
        }
        public override string Abilities
        {
            get
            {
                return "Protect one person from death each night.";
            }
        }
        public override string Attributes
        {
            get
            {
                return "If your target is attacked, both you and your attacker will die instead.\n"
                    + "If you successfully protect someone, you can't be saved from death.\n"
                    + "Your counterattack ignores night immunity.";
            }
        }
        public override RoleAlignment Alignment
        {
            get
            {
                return RoleAlignment.TownProtective;
            }
        }
        protected override RoleEventFlags EventFlags
        {
            get
            {
                return RoleEventFlags.NightStart | RoleEventFlags.NightUpdate | RoleEventFlags.NightEnd;
            }
        }

        public BodyGuard(PlayerInfo player) : base(player, typeof(BodyGuard))
        {
            m_VestsLeft = 1;
        }
        new async Task OnNightStart()
        {
            m_Target = null;
            m_BgQueue = "";
            m_Dirty = true;
            BotInstance.RegisterReactionAddHandler(this.InstanceId(), OnReactionAdded);
            m_UpdateMessage = await m_Player.House.SendMessageAsync("Click to choose a target!");
            if (!m_Player.NightInfo.Jailed)
            {
                foreach (PlayerInfo info in BotInstance.CurrentGame.AlivePlayers)
                {
                    if (info == m_Player && m_VestsLeft <= 0)
                        continue;
                    await m_UpdateMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, $":{VOTE_TARGETS[info.Index]}:"));
                }
            }
        }
        new async Task OnNightUpdate()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                await m_UpdateMessage.ModifyAsync($"{m_BgQueue}Click to choose a target!");
            }
        }
        new async Task OnNightEnd()
        {
            BotInstance.UnregisterReactionAddHandler(this.InstanceId());
            if (m_Target != null && !m_Player.NightInfo.Roleblocked)
            {
                if (m_Target.NightInfo.Jailed)
                    await m_Player.House.SendMessageAsync("**Your ability failed because your target was jailed!");
                else if (m_Target != m_Player)
                    m_Target.NightInfo.BGsLeft.Add(m_Player);
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
                m_BgQueue += m_Target == m_Player ? "You have decided to put on your bulletproof vest tonight.\n"
                    : $"You have decided to guard **{m_Target.Name}**\n";
                m_Dirty = true;
            }
            await Task.Delay(1);
        }
    }
}
