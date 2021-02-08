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
    public class Jailor : BaseRole
    {
        DiscordMessage m_UpdateMessage;
        string m_JailorQueue;
        bool m_Dirty;
        bool m_IsExecuting;
        int m_Executions;
        bool m_ExecutedTown;

        public override int RoleExecutionPriority
        {
            get
            {
                return (int)RoleExecution.Execution;
            }
        }
        protected override string NightText
        {
            get
            {
                return $"You have **{m_Executions}** executions left!\nYou have dragged your target off to jail!";
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
                return AttackType.Unstoppable;
            }
        }
        public override string Abilities
        {
            get
            {
                return "You may choose one person during the day to jail for the night.";
            }
        }
        public override string Attributes
        {
            get
            {
                return "You may anonymously talk with your prisoner.\n"
                    + "You may choose to execute your prisoner.\n"
                    + "The jailed target cannot perform their night ability.\n"
                    + "While jailed the prisoner is safe from all attacks.";
            }
        }
        public override RoleAlignment Alignment
        {
            get
            {
                return RoleAlignment.TownKilling;
            }
        }
        protected override RoleEventFlags EventFlags
        {
            get
            {
                return RoleEventFlags.DayStart | RoleEventFlags.DayUpdate | RoleEventFlags.DayEnd
                    | RoleEventFlags.NightStart | RoleEventFlags.NightUpdate | RoleEventFlags.NightEnd;
            }
        }
        public int ExecutionsLeft
        {
            get
            {
                return m_Executions;
            }
        }
        public bool ExecutedTown
        {
            get
            {
                return m_ExecutedTown;
            }
        }

        public Jailor(PlayerInfo player) : base(player, typeof(Jailor))
        {
            m_Executions = 3;
        }
        new async Task OnNightStart()
        {
            m_JailorQueue = "";
            m_Dirty = true;
            m_IsExecuting = false;
            m_UpdateMessage = await m_Player.House.SendMessageAsync(m_ExecutedTown ? "You have slain a town member so you can not attack again." 
                : "Click to execute your target!");
            if (m_Target != null)
            {
                await m_Target.House.SendMessageAsync("**You were hauled off to jail!**");
                if (!m_ExecutedTown)
                    await m_UpdateMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, $":{VOTE_TARGETS[m_Target.Index]}:"));
                m_Target.NightInfo.Jailed = true;
            }
        }
        new async Task OnNightUpdate()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                await m_UpdateMessage.ModifyAsync($"{m_JailorQueue}Click to execute your target!");
            }
        }
        new async Task OnNightEnd()
        {
            BotInstance.UnregisterReactionAddHandler(this.InstanceId());
            if (m_IsExecuting && m_Target != null && !m_Player.NightInfo.Roleblocked)
            {
                await m_Target.RoleObject.PerformAttack(AttackType.Unstoppable, m_Player);
                m_Executions--;
                if (IsTown(m_Target))
                    m_ExecutedTown = true;
            }
        }
        new async Task OnDayStart()
        {
	        m_Target = null;
            BotInstance.RegisterReactionAddHandler(this.InstanceId(), OnReactionAdded);
            m_UpdateMessage = await m_Player.House.SendMessageAsync("**Choose a player to jail!**");
            foreach (PlayerInfo info in BotInstance.CurrentGame.AlivePlayers)
            {
                if (info != m_Player)
                    await m_UpdateMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, $":{VOTE_TARGETS[info.Index]}:"));
            }
        }
        new async Task OnDayUpdate()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                await m_UpdateMessage.ModifyAsync($"{m_JailorQueue}**Choose a player to jail!**");
            }
        }
        new async Task OnDayEnd()
        {
            if (m_Target == null)
                m_UpdateMessage = await m_Player.House.SendMessageAsync("**Click to reveal!**");
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
                m_JailorQueue += BotInstance.CurrentGame.DayState != DayState.Night ? $"You have decided to jail **{m_Target.Name}**\n"
                    : m_IsExecuting ? "You have changed your mind\n" : $"You have decided to execute **{m_Target.Name}**\n";
                if (BotInstance.CurrentGame.DayState == DayState.Night)
                    m_IsExecuting = !m_IsExecuting;
                m_Dirty = true;
            }
            await Task.Delay(1);
        }
    }
}
