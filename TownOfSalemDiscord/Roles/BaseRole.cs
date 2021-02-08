using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace TownOfSalemDiscord.Roles
{
    public enum AttackType
    {
        None = 0,
        Basic = 1,
        Powerful = 2,
        Unstoppable = 3
    }

    public enum DefenceType
    {
        None = 0,
        Basic = 1,
        Powerful = 2,
        Unstoppable = 3
    }

    public enum RoleExecution
    {
        Transportation,
        Roleblock,
        Incinerating,
        Execution,
        Guard,
        Heal,
        DragonAction,
        Clean,
        Blackmailing,
        Disguising,
        MafiaAttack,
        Dousing,
        SKStab,
        WerewolfMaul
    }

    public enum RoleAlignment
    {
        TownProtective,
        TownKilling,
        TownInvestigative,
        TownSupport,
        MafiaKilling,
        MafiaDeception,
        NeutralKilling
    }

    public enum DeathFlags
    {
        None = 0,
        Cleaned = 1,
        Stoned = 2,
        Forged = 4
    }

    public enum RoleEventFlags
    {
        None = 0,
        DayStart = 1,
        DayUpdate = 2,
        DayEnd = 4,
        NightStart = 8,
        NightUpdate = 16,
        NightEnd = 32
    }

    public abstract class BaseRole
    {
        const string TARGET_IMMUNE = "**Your target's defence was too strong to kill**";
        const string LOCAL_IMMUNE = "**Someone tried to attack you, but your defence is too high!**";
        const string TARGET_HEALED = "**Your target was attacked!**";
        const string LOCAL_HEALED = "**You were attacked but someone nursed you back to health!**";
        const string TARGET_GUARDED = "**You were killed protecting your target!**";
        const string LOCAL_GUARDED = "**You were attacked but someone fended off your attacked!**";
        const string LOCAL_ATTACKED_JAIL = "**Someone tried to attack you but you were jailed!**";
        const string TARGET_JAILED = "**Your ability failed because your target was jailed!**";
        const string KILLED_BY_ARSO = "**You were incinerated by an arsonist!**";
        const string KILLED_BY_SK = "**You were stabbed by a serial killer!**";
        const string KILLED_BY_MAFIA = "**You were attacked by a member of the mafia!**";
        const string KILLED_BY_WW = "**You were mauled by a werewolf!**";
        const string KILLED_BY_BG = "**You were killed by a bodyguard!**";
        const string KILLED_BY_VETERAN = "**You were shot by the veteran you visited!**";
        const string KILLED_BY_VIGILANTE = "**You were shot by a vigilante!**";
        const string KILLED_BY_JAILOR = "**You were executed by the jailor!**";

        protected PlayerInfo m_Player;
        protected PlayerInfo m_Target;
        MethodInfo m_AsyncNightUpdate;
        MethodInfo m_AsyncNightStart;
        MethodInfo m_AsyncNightEnd;
        MethodInfo m_AsyncDayStart;
        MethodInfo m_AsyncDayUpdate;
        MethodInfo m_AsyncDayEnd;
        Type m_Child;

        protected Bot BotInstance
        {
            get
            {
                return Bot.Instance;
            }
        }
        public abstract DefenceType Defence { get; }
        public virtual AttackType Attack { get { return AttackType.None; } }
        protected abstract string NightText { get; }
        public abstract int RoleExecutionPriority { get; }
        public abstract string Abilities { get; }
        public abstract string Attributes { get; }
        public abstract RoleAlignment Alignment { get; }
        protected virtual bool CanVisit { get { return true; } }
        protected abstract RoleEventFlags EventFlags { get; }

        public BaseRole(PlayerInfo info, Type child)
        {
            m_Player = info;
            m_Child = child;
            m_AsyncNightUpdate = m_Child.GetMethod("OnNightUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            m_AsyncNightStart = m_Child.GetMethod("OnNightStart", BindingFlags.NonPublic | BindingFlags.Instance);
            m_AsyncNightEnd = m_Child.GetMethod("OnNightEnd", BindingFlags.NonPublic | BindingFlags.Instance);
            m_AsyncDayStart = m_Child.GetMethod("OnDayStart", BindingFlags.NonPublic | BindingFlags.Instance);
            m_AsyncDayUpdate = m_Child.GetMethod("OnDayUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            m_AsyncDayEnd = m_Child.GetMethod("OnDayEnd", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        bool HasEventFlag(RoleEventFlags flags)
        {
            return (EventFlags & flags) != 0;
        }
        public async Task OnNightStart()
        {
            if (!HasEventFlag(RoleEventFlags.NightStart))
                return;
            if (NightText.Length > 0)
                await m_Player.House.SendMessageAsync(NightText);
            await (Task)m_AsyncNightStart.Invoke(this, new object[0]);
        }
        public async Task OnNightUpdate()
        {
            if (HasEventFlag(RoleEventFlags.NightUpdate))
                await (Task)m_AsyncNightUpdate.Invoke(this, new object[0]);
        }
        public async Task OnNightEnd()
        {
            if (HasEventFlag(RoleEventFlags.NightEnd))
                await (Task)m_AsyncNightEnd.Invoke(this, new object[0]);
        }
        public async Task OnDayStart()
        {
            if (HasEventFlag(RoleEventFlags.DayStart))
                await (Task)m_AsyncDayStart.Invoke(this, new object[0]);
        }
        public async Task OnDayUpdate()
        {
            if (HasEventFlag(RoleEventFlags.DayUpdate))
                await (Task)m_AsyncDayUpdate.Invoke(this, new object[0]);
        }
        public async Task OnDayEnd()
        {
            if (HasEventFlag(RoleEventFlags.DayEnd))
                await (Task)m_AsyncDayEnd.Invoke(this, new object[0]);
        }
        public void RegisterVisits()
        {
            if (CanVisit && m_Target != null)
                m_Target.NightInfo.Visitors.Add(m_Player);
        }
        public async Task PerformAttack(AttackType type, PlayerInfo attacker)
        {
            NightInfo ninfo = m_Player.NightInfo;
            if (attacker.Role != Role.Arsonist && attacker.Role != Role.Jailor)
            {
                if (ninfo.Jailed)
                {
                    await m_Player.House.SendMessageAsync(LOCAL_ATTACKED_JAIL);
                    await attacker.House.SendMessageAsync(TARGET_JAILED);
                    return;
                }
                if (ninfo.DoctorsLeft.Count > 0)
                {
                    PlayerInfo doc = ninfo.DoctorsLeft[0];
                    ninfo.DoctorsLeft.RemoveAt(0);
                    if (doc.Role == Role.Dragon)
                        await doc.House.SendMessageAsync("**One of your visitors were attacked last night!**");
                    else await doc.House.SendMessageAsync(TARGET_HEALED);
                    await m_Player.House.SendMessageAsync(LOCAL_HEALED);
                    return;
                }
                if (ninfo.BGsLeft.Count > 0)
                {
                    if (this is BodyGuard && (this as BodyGuard).IsVesting)
                    {
                        await m_Player.House.SendMessageAsync("**You were attacked but your bulletproof vest saved you!**");
                        await attacker.House.SendMessageAsync(TARGET_IMMUNE);
                        return;
                    }
                    PlayerInfo bg = ninfo.BGsLeft[0];
                    ninfo.BGsLeft.RemoveAt(0);
                    await bg.House.SendMessageAsync(TARGET_GUARDED);
                    await m_Player.House.SendMessageAsync(LOCAL_GUARDED);
                    await bg.RoleObject.PerformAttack(AttackType.Basic, bg);
                    await attacker.RoleObject.PerformAttack(AttackType.Basic, bg);
                    return;
                }
                if ((int)Defence >= (int)type && attacker.Role != Role.BodyGuard)
                {
                    await attacker.House.SendMessageAsync(TARGET_IMMUNE);
                    await m_Player.House.SendMessageAsync(LOCAL_IMMUNE);
                    return;
                }
            }
            string msg = "INVALID";
            switch (attacker.Role)
            {
                case Role.Arsonist:
                    msg = KILLED_BY_ARSO;
                    break;
                case Role.Godfather:
                case Role.Mafioso:
                    msg = KILLED_BY_MAFIA;
                    break;
                case Role.SerialKiller:
                    msg = KILLED_BY_SK;
                    break;
                case Role.Werewolf:
                    msg = KILLED_BY_WW;
                    break;
                case Role.BodyGuard:
                    if (m_Player.Role != Role.BodyGuard)
                        msg = KILLED_BY_BG;
                    break;
                case Role.Veteran:
                    msg = KILLED_BY_VETERAN;
                    break;
                case Role.Vigilante:
                    msg = KILLED_BY_VIGILANTE;
                    break;
                case Role.Jailor:
                    msg = KILLED_BY_JAILOR;
                    break;
                case Role.Dragon:
                    msg = "**You were torn by a dragon!**";
                    break;
            }
            DeathFlags flags = DeathFlags.None;
            if (attacker.Role == Role.Medusa)
                flags |= DeathFlags.Stoned;
            else if (m_Player.NightInfo.Cleaned)
                flags |= DeathFlags.Cleaned;
            if (m_Player.NightInfo.Forged)
                flags |= DeathFlags.Forged;
            BotInstance.CurrentGame.QueueDeath(attacker.Role, m_Player, flags);
            await m_Player.House.SendMessageAsync(msg + "\n**You have died!**");
        }
    }
}
