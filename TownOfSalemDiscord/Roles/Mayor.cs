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
    public class Mayor : BaseRole
    {
        DiscordMessage m_UpdateMessage;
        bool m_Dirty;
        bool m_Revealed;

        protected override bool CanVisit
        {
            get
            {
                return false;
            }
        }
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
        public bool Revealed
        {
            get
            {
                return m_Revealed;
            }
        }
        public override string Abilities
        {
            get
            {
                return "You may reveal yourself as the Mayor of the Town.";
            }
        }
        public override string Attributes
        {
            get
            {
                return "Once you have revealed yourself as the Mayor your vote counts as 3 votes.\n"
                    + "You may not be healed once you have revealed.\n"
                    + "Once revealed you can't whisper, or be whispered to.";
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
                return RoleEventFlags.DayStart | RoleEventFlags.DayEnd;
            }
        }

        public Mayor(PlayerInfo player) : base(player, typeof(Mayor))
        {
        }
        new async Task OnDayStart()
        {
            if (m_Revealed)
                return;
            m_UpdateMessage = await m_Player.House.SendMessageAsync("**Click to reveal!**");
            await m_UpdateMessage.CreateReactionAsync(DiscordEmoji.FromName(Bot.Client, $":{VOTE_TARGETS[m_Player.Index]}:"));
            BotInstance.RegisterReactionAddHandler(this.InstanceId(), OnReactionAdded);
        }
        new async Task OnDayEnd()
        {
            if (!m_Revealed)
                await m_Player.House.SendMessageAsync("**You did not perform your day ability!**");
            BotInstance.UnregisterReactionAddHandler(this.InstanceId());
        }
        async Task Reveal()
        {
            if (m_Revealed || BotInstance.CurrentGame.DayState == DayState.Night || BotInstance.CurrentGame.DayState == DayState.Announcing)
                return;
            m_Revealed = true;
            await DayChannel.SendMessageAsync($"**{m_Player.Name}** has revealed themselves as the __**mayor**__!");
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
                if (idx == m_Player.Index && !m_Revealed)
                    await Reveal();
            }
            await Task.Delay(1);
        }
    }
}