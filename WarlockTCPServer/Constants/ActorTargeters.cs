﻿using System.Collections.Generic;
using WarlockTCPServer.GameLogic.ActorComponents;

namespace WarlockTCPServer.Constants
{
    public static class ActorTargeters
    {
        public delegate IEnumerable<Actor> Targeter(IEnumerable<Actor> friendlyParty, IEnumerable<Actor> enemyParty);

        #region Targeter Methods
        public static IEnumerable<Actor> FirstAlive(IEnumerable<Actor> friendlyParty, IEnumerable<Actor> enemyParty)
        {
            foreach (var target in enemyParty)
            {
                if (target.Health.CurrentValue > 0)
                    return new Actor[] { target };
            }
            return new Actor[] { null };
        }
        #endregion

        public static Dictionary<DeckConstants.Targeter, Targeter> Targeters = new Dictionary<DeckConstants.Targeter, Targeter>()
        {
            { DeckConstants.Targeter.FirstAlive, FirstAlive }
        };


    }
}
