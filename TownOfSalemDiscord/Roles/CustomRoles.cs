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
    public class Dragon : BaseRole
    {
        DiscordMessage m_UpdateMessage;
        string m_DragonQueue;
        bool m_Dirty;

        public override int RoleExecutionPriority
        {
            get
            {
                return (int)RoleExecution.DragonAction;
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
        public override AttackType Attack
        {
            get
            {
                return AttackType.Basic;
            }
        }
        public override string Abilities
        {
            get
            {
                return "You may either protect all town members who visited your house or you may attack 1 person.";
            }
        }
        public override string Attributes
        {
            get
            {
                return "None";
            }
        }
        public override RoleAlignment Alignment
        {
            get
            {
                return RoleAlignment.NeutralKilling;
            }
        }
        protected override RoleEventFlags EventFlags
        {
            get
            {
                return RoleEventFlags.NightStart | RoleEventFlags.NightUpdate | RoleEventFlags.NightEnd;
            }
        }

        public Dragon(PlayerInfo player) : base(player, typeof(Dragon))
        {
        }
        new async Task OnNightStart()
        {
            m_Target = null;
            m_DragonQueue = "";
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
                await m_UpdateMessage.ModifyAsync($"{m_DragonQueue}Click to choose a target!");
            }
        }
        new async Task OnNightEnd()
        {
            BotInstance.UnregisterReactionAddHandler(this.InstanceId());
            if (m_Target != null && !m_Player.NightInfo.Roleblocked && !m_Player.NightInfo.Jailed)
            {
                if (m_Target == m_Player)
                {
                    //heal all visitors
                    foreach (PlayerInfo info in m_Player.NightInfo.Visitors)
                        info.NightInfo.DoctorsLeft.Add(m_Player);
                }
                else
                    await m_Target.RoleObject.PerformAttack(Attack, m_Player);
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
                m_DragonQueue += m_Target == m_Player ? "You have decided to heal visitors tonight!\n" : $"You have decided to kill **{m_Target.Name}**\n";
                m_Dirty = true;
            }
            await Task.Delay(1);
        }
    }
}