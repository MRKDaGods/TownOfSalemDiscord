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
    public class Godfather : BaseRole
    {
        DiscordMessage m_UpdateMessage;
        string m_GfQueue;
        bool m_Dirty;

        public override int RoleExecutionPriority
        {
            get
            {
                return (int)RoleExecution.MafiaAttack;
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
                return DefenceType.Basic;
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
                return "Kill someone each night.";
            }
        }
        public override string Attributes
        {
            get
            {
                return "You can't be killed at night.\n"
                    + "If there is a Mafioso he will attack the target instead of you.\n"
                    + "You will appear to be a Town member to the Sheriff.\n"
                    + "You can talk with the other Mafia at night.";
            }
        }
        public override RoleAlignment Alignment
        {
            get
            {
                return RoleAlignment.MafiaKilling;
            }
        }
        protected override RoleEventFlags EventFlags
        {
            get
            {
                return RoleEventFlags.NightStart | RoleEventFlags.NightUpdate | RoleEventFlags.NightEnd;
            }
        }

        public Godfather(PlayerInfo player) : base(player, typeof(Godfather))
        {
        }
        new async Task OnNightStart()
        {
            m_Target = null;
            m_GfQueue = "";
            m_Dirty = true;
            BotInstance.RegisterReactionAddHandler(this.InstanceId(), OnReactionAdded);
            m_UpdateMessage = await m_Player.House.SendMessageAsync("Click to choose a target!");
            if (!m_Player.NightInfo.Jailed)
                foreach (PlayerInfo info in BotInstance.CurrentGame.AlivePlayers)
                    if (!IsMafia(info))
                        await m_UpdateMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, $":{VOTE_TARGETS[info.Index]}:"));
        }
        new async Task OnNightUpdate()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                await m_UpdateMessage.ModifyAsync($"{m_GfQueue}Click to choose a target!");
            }
        }
        new async Task OnNightEnd()
        {
            BotInstance.UnregisterReactionAddHandler(this.InstanceId());
            if (m_Target != null && !m_Player.NightInfo.Roleblocked && !m_Player.NightInfo.Jailed)
            {
                List<PlayerInfo> res = (from pl in BotInstance.CurrentGame.AlivePlayers where pl.Role == Role.Mafioso select pl).ToList();
                PlayerInfo mafioso = null;
                if (res.Count > 0)
                    mafioso = res[0];
                if (mafioso != null && !mafioso.NightInfo.Roleblocked && !mafioso.NightInfo.Jailed)
                {
                    await mafioso.House.SendMessageAsync("**The godfather has ordered you to kill their target!**");
                    await m_Target.RoleObject.PerformAttack(AttackType.Basic, mafioso);
                    m_Target.NightInfo.AttackersLeft.Add(mafioso);
                    return;
                }
                m_Target.NightInfo.AttackersLeft.Add(m_Player);
                await m_Target.RoleObject.PerformAttack(AttackType.Basic, m_Player);
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
                m_GfQueue += $"You have decided to kill **{m_Target.Name}**\n";
                await BotInstance.CurrentGame.SendMafiaMessage($"**{m_Player.Name}** has decided to kill **{m_Target.Name}**");
                m_Dirty = true;
            }
            await Task.Delay(1);
        }
    }
}
