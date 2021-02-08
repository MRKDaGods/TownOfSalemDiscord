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
    public class Janitor : BaseRole
    {
        DiscordMessage m_UpdateMessage;
        string m_MfQueue;
        bool m_Dirty;
        int m_CleansLeft;

        public override int RoleExecutionPriority
        {
            get
            {
                return (int)RoleExecution.Clean;
            }
        }
        protected override string NightText
        {
            get
            {
                return $"You have **{m_CleansLeft}** cleans left.";
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
                return AttackType.None;
            }
        }
        public override string Abilities
        {
            get
            {
                return "Choose a person to clean at night.";
            }
        }
        public override string Attributes
        {
            get
            {
                return "If your target dies their role and last will won't be revealed to the town.\n"
                    + "Only you will see the cleaned targets role and last will.\n"
                    + "You may only perform 3 cleanings.\n"
                    + "If there are no kill capable Mafia roles left you will become a Mafioso.\n"
                    + "You can talk with the other Mafia at night.";
            }
        }
        public override RoleAlignment Alignment
        {
            get
            {
                return RoleAlignment.MafiaDeception;
            }
        }
        protected override RoleEventFlags EventFlags
        {
            get
            {
                return RoleEventFlags.NightStart | RoleEventFlags.NightUpdate | RoleEventFlags.NightEnd;
            }
        }

        public Janitor(PlayerInfo player) : base(player, typeof(Janitor))
        {
            m_CleansLeft = 3;
        }
        new async Task OnNightStart()
        {
            m_Target = null;
            m_MfQueue = "";
            m_Dirty = true;
            BotInstance.RegisterReactionAddHandler(this.InstanceId(), OnReactionAdded);
            m_UpdateMessage = await m_Player.House.SendMessageAsync("Click to choose a target!");
            if (!m_Player.NightInfo.Jailed && m_CleansLeft > 0)
                foreach (PlayerInfo info in BotInstance.CurrentGame.AlivePlayers)
                    if (!IsMafia(info))
                        await m_UpdateMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, $":{VOTE_TARGETS[info.Index]}:"));
        }
        new async Task OnNightUpdate()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                await m_UpdateMessage.ModifyAsync($"{m_MfQueue}Click to choose a target!");
            }
        }
        new async Task OnNightEnd()
        {
            BotInstance.UnregisterReactionAddHandler(this.InstanceId());
            if (m_Target != null && !m_Player.NightInfo.Roleblocked && m_CleansLeft > 0)
            {
                if (m_Target.NightInfo.Jailed)
                {
                    await m_Player.House.SendMessageAsync("**Your ability failed because your target was jailed!**");
                    return;
                }
                m_Target.NightInfo.Cleaned = true;
                m_CleansLeft--;
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
                m_MfQueue += $"You have decided to clean **{m_Target.Name}**\n";
                await BotInstance.CurrentGame.SendMafiaMessage($"**{m_Player.Name}** has decided to clean **{m_Target.Name}**");
                m_Dirty = true;
            }
            await Task.Delay(1);
        }
    }
}