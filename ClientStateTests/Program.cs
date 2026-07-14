using System;
using System.Collections.Generic;

// Deterministic verification of the PRODUCTION change-calculation
// (GamePlayManager.GameStateChangeSet.Create) — referenced from the real Assembly-CSharp,
// never reimplemented here. Nonzero exit on any failure.
//
// Types used (all public, global namespace, defined in Assembly-CSharp):
//   GamePlayManager.GameState (+ nested Player / FleetInfo / Planet / ProductionQueueInfo)
//   GamePlayManager.GameStateChangeSet (+ Create)
//   GamePlayManager.FleetChange / PlanetOwnerChange / ResourceChange
using GameState = GamePlayManager.GameState;
using ChangeSet = GamePlayManager.GameStateChangeSet;

namespace ClientStateTests
{
    internal static class Program
    {
        private static int _failed;

        private static readonly (string Name, Action Run)[] Tests =
        {
            ("initial snapshot (previous == null)", TestInitialSnapshot),
            ("unchanged snapshot", TestUnchangedSnapshot),
            ("fleet added", TestFleetAdded),
            ("fleet removed", TestFleetRemoved),
            ("fleet updated (state transition)", TestFleetUpdated),
            ("planet added", TestPlanetAdded),
            ("planet removed", TestPlanetRemoved),
            ("planet owner update", TestPlanetOwnerUpdate),
            ("resource-only change", TestResourceOnlyChange),
            ("baseline reset / new game (no stale carryover)", TestBaselineReset),
            ("combined multi-domain change (single change set)", TestCombinedMultiDomainChange),
            ("fleet movement + HP transition via UpdatedFleets", TestFleetMovementAndHpTransition),
            ("planet add + remove + owner change combined", TestPlanetAddRemoveOwnerCombined),
            ("no resource change on first snapshot", TestNoResourceChangeOnFirstSnapshot),
            ("null collections handled safely", TestNullCollectionsSafe),
            ("cross-player fleet identity by fleetId", TestMultiPlayerFleetIdentity),
            ("change set aliasing + default serverTick", TestChangeSetAliasingAndDefaults),
        };

        private static int Main()
        {
            foreach (var (name, run) in Tests)
            {
                try
                {
                    run();
                    Console.WriteLine($"PASS: {name}");
                }
                catch (Exception ex)
                {
                    _failed++;
                    Console.Error.WriteLine($"FAIL: {name}\n  {ex.Message}");
                }
            }

            Console.WriteLine($"Completed {Tests.Length} tests: {Tests.Length - _failed} passed, {_failed} failed.");
            return _failed == 0 ? 0 : 1;
        }

        // ---- tests ----

        private static void TestInitialSnapshot()
        {
            var current = State(
                players: new[] { Player(0, Fleet(1), Fleet(2)) },
                planets: new[] { Planet(10, 0), Planet(11, 1) });

            var cs = ChangeSet.Create(null, current, serverTick: 5);

            AssertEqual(false, cs.HasPrevious, "HasPrevious");
            AssertEqual(5L, cs.ServerTick, "ServerTick");
            AssertSame(current, cs.Current, "Current");
            AssertEqual(2, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(0, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(0, cs.UpdatedFleets.Count, "UpdatedFleets");
            AssertEqual(2, cs.AddedPlanets.Count, "AddedPlanets");
            AssertEqual(0, cs.RemovedPlanets.Count, "RemovedPlanets");
            AssertEqual(0, cs.PlanetOwnerChanges.Count, "PlanetOwnerChanges");
            AssertEqual(0, cs.ResourceChanges.Count, "ResourceChanges");
        }

        private static void TestUnchangedSnapshot()
        {
            var prev = State(
                players: new[] { Player(0, Fleet(1), Fleet(2)) },
                planets: new[] { Planet(10, 0) });
            var curr = State(
                players: new[] { Player(0, Fleet(1), Fleet(2)) },
                planets: new[] { Planet(10, 0) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(true, cs.HasPrevious, "HasPrevious");
            AssertEqual(0, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(0, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(2, cs.UpdatedFleets.Count, "UpdatedFleets");
            AssertEqual(0, cs.AddedPlanets.Count, "AddedPlanets");
            AssertEqual(0, cs.RemovedPlanets.Count, "RemovedPlanets");
            AssertEqual(0, cs.PlanetOwnerChanges.Count, "PlanetOwnerChanges");
            AssertEqual(0, cs.ResourceChanges.Count, "ResourceChanges");
        }

        private static void TestFleetAdded()
        {
            var prev = State(players: new[] { Player(0, Fleet(1)) });
            var curr = State(players: new[] { Player(0, Fleet(1), Fleet(2)) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(1, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(2L, cs.AddedFleets[0].fleetId, "added fleetId");
            AssertEqual(1, cs.UpdatedFleets.Count, "UpdatedFleets");
            AssertEqual(0, cs.RemovedFleets.Count, "RemovedFleets");
        }

        private static void TestFleetRemoved()
        {
            var prev = State(players: new[] { Player(0, Fleet(1), Fleet(2)) });
            var curr = State(players: new[] { Player(0, Fleet(1)) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(0, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(1, cs.UpdatedFleets.Count, "UpdatedFleets");
            AssertEqual(1, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(2L, cs.RemovedFleets[0].fleetId, "removed fleetId");
        }

        private static void TestFleetUpdated()
        {
            var prev = State(players: new[] { Player(0, Fleet(1, state: 0)) });
            var curr = State(players: new[] { Player(0, Fleet(1, state: 2)) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(0, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(0, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(1, cs.UpdatedFleets.Count, "UpdatedFleets");
            AssertEqual(0, cs.UpdatedFleets[0].Previous.state, "previous state");
            AssertEqual(2, cs.UpdatedFleets[0].Current.state, "current state");
        }

        private static void TestPlanetAdded()
        {
            var prev = State(planets: new[] { Planet(10, 0) });
            var curr = State(planets: new[] { Planet(10, 0), Planet(11, 0) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(1, cs.AddedPlanets.Count, "AddedPlanets");
            AssertEqual(11, cs.AddedPlanets[0].planetId, "added planetId");
            AssertEqual(0, cs.RemovedPlanets.Count, "RemovedPlanets");
            AssertEqual(0, cs.PlanetOwnerChanges.Count, "PlanetOwnerChanges");
        }

        private static void TestPlanetRemoved()
        {
            var prev = State(planets: new[] { Planet(10, 0), Planet(11, 0) });
            var curr = State(planets: new[] { Planet(10, 0) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(0, cs.AddedPlanets.Count, "AddedPlanets");
            AssertEqual(1, cs.RemovedPlanets.Count, "RemovedPlanets");
            AssertEqual(11, cs.RemovedPlanets[0].planetId, "removed planetId");
        }

        private static void TestPlanetOwnerUpdate()
        {
            var prev = State(planets: new[] { Planet(10, 0) });
            var curr = State(planets: new[] { Planet(10, 1) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(0, cs.AddedPlanets.Count, "AddedPlanets");
            AssertEqual(0, cs.RemovedPlanets.Count, "RemovedPlanets");
            AssertEqual(1, cs.PlanetOwnerChanges.Count, "PlanetOwnerChanges");
            AssertEqual(10, cs.PlanetOwnerChanges[0].PlanetId, "planetId");
            AssertEqual(0, cs.PlanetOwnerChanges[0].PreviousOwner, "previousOwner");
            AssertEqual(1, cs.PlanetOwnerChanges[0].NewOwner, "newOwner");
        }

        private static void TestResourceOnlyChange()
        {
            var prev = State(players: new[] { PlayerRes(0, gas: 10, mineral: 5, supply: 1, Fleet(1)) });
            var curr = State(players: new[] { PlayerRes(0, gas: 20, mineral: 5, supply: 1, Fleet(1)) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(0, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(0, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(1, cs.ResourceChanges.Count, "ResourceChanges");
            AssertEqual(0, cs.ResourceChanges[0].PlayerId, "playerId");
            AssertEqual(20, cs.ResourceChanges[0].Gas, "gas");
            AssertEqual(5, cs.ResourceChanges[0].Mineral, "mineral");
        }

        private static void TestBaselineReset()
        {
            // Old game ended with fleets {1,2}. On GAME_STARTED the owner (GamePlayManager) resets
            // the previous snapshot to null. The first new-game snapshot has a fresh fleet {1}.
            var oldGameFinal = State(players: new[] { Player(0, Fleet(1), Fleet(2)) });
            var newGameFirst = State(players: new[] { Player(0, Fleet(1)) });

            // With reset (previous == null): fresh baseline, no stale removals cross the boundary.
            var afterReset = ChangeSet.Create(null, newGameFirst);
            AssertEqual(1, afterReset.AddedFleets.Count, "afterReset AddedFleets");
            AssertEqual(0, afterReset.RemovedFleets.Count, "afterReset RemovedFleets (no stale carryover)");

            // Sanity: WITHOUT reset the stale fleet would be reported removed, proving the reset matters.
            var withoutReset = ChangeSet.Create(oldGameFinal, newGameFirst);
            AssertEqual(1, withoutReset.RemovedFleets.Count, "withoutReset RemovedFleets (stale)");
        }

        private static void TestCombinedMultiDomainChange()
        {
            // One GAME_STATE that simultaneously adds a fleet, flips a planet owner, and changes resources.
            // A single Create must capture all three domains at once.
            var prev = State(
                players: new[] { PlayerRes(0, gas: 10, mineral: 5, supply: 1, Fleet(1)) },
                planets: new[] { Planet(10, 0) });
            var curr = State(
                players: new[] { PlayerRes(0, gas: 25, mineral: 5, supply: 1, Fleet(1), Fleet(2)) },
                planets: new[] { Planet(10, 1) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(1, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(2L, cs.AddedFleets[0].fleetId, "added fleetId");
            AssertEqual(1, cs.UpdatedFleets.Count, "UpdatedFleets (fleet 1 persists)");
            AssertEqual(0, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(1, cs.PlanetOwnerChanges.Count, "PlanetOwnerChanges");
            AssertEqual(10, cs.PlanetOwnerChanges[0].PlanetId, "planetId");
            AssertEqual(1, cs.PlanetOwnerChanges[0].NewOwner, "newOwner");
            AssertEqual(1, cs.ResourceChanges.Count, "ResourceChanges");
            AssertEqual(25, cs.ResourceChanges[0].Gas, "gas");
        }

        private static void TestFleetMovementAndHpTransition()
        {
            // Movement/combat are represented through UpdatedFleets (state/HP transitions), never invented.
            var prev = State(players: new[] { Player(0, FleetHp(7, state: 0, hp: 100f)) });
            var curr = State(players: new[] { Player(0, FleetHp(7, state: 2, hp: 60f)) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(0, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(0, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(1, cs.UpdatedFleets.Count, "UpdatedFleets");
            AssertEqual(7L, cs.UpdatedFleets[0].Current.fleetId, "fleetId");
            AssertEqual(0, cs.UpdatedFleets[0].Previous.state, "previous state (idle)");
            AssertEqual(2, cs.UpdatedFleets[0].Current.state, "current state (moving)");
            AssertEqual(100f, cs.UpdatedFleets[0].Previous.HP, "previous HP");
            AssertEqual(60f, cs.UpdatedFleets[0].Current.HP, "current HP");
        }

        private static void TestPlanetAddRemoveOwnerCombined()
        {
            // Distinct planet ids: 10 owner-changes, 11 removed, 12 added — all in one diff.
            var prev = State(planets: new[] { Planet(10, 0), Planet(11, 0) });
            var curr = State(planets: new[] { Planet(10, 1), Planet(12, 0) });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(1, cs.AddedPlanets.Count, "AddedPlanets");
            AssertEqual(12, cs.AddedPlanets[0].planetId, "added planetId");
            AssertEqual(1, cs.RemovedPlanets.Count, "RemovedPlanets");
            AssertEqual(11, cs.RemovedPlanets[0].planetId, "removed planetId");
            AssertEqual(1, cs.PlanetOwnerChanges.Count, "PlanetOwnerChanges");
            AssertEqual(10, cs.PlanetOwnerChanges[0].PlanetId, "owner-changed planetId");
            AssertEqual(0, cs.PlanetOwnerChanges[0].PreviousOwner, "previousOwner");
            AssertEqual(1, cs.PlanetOwnerChanges[0].NewOwner, "newOwner");
        }

        private static void TestNoResourceChangeOnFirstSnapshot()
        {
            // previous == null: resources must NOT be reported (no reliable baseline), even with players present.
            var curr = State(players: new[] { PlayerRes(0, gas: 99, mineral: 7, supply: 3, Fleet(1)) });

            var cs = ChangeSet.Create(null, curr);

            AssertEqual(false, cs.HasPrevious, "HasPrevious");
            AssertEqual(0, cs.ResourceChanges.Count, "ResourceChanges (none on first snapshot)");
            AssertEqual(1, cs.AddedFleets.Count, "AddedFleets");
        }

        private static void TestNullCollectionsSafe()
        {
            // Both snapshots carry null players and planets: the algorithm must not throw and reports no changes.
            var prev = State();
            var curr = State();

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(true, cs.HasPrevious, "HasPrevious");
            AssertEqual(0, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(0, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(0, cs.UpdatedFleets.Count, "UpdatedFleets");
            AssertEqual(0, cs.AddedPlanets.Count, "AddedPlanets");
            AssertEqual(0, cs.RemovedPlanets.Count, "RemovedPlanets");
            AssertEqual(0, cs.PlanetOwnerChanges.Count, "PlanetOwnerChanges");
            AssertEqual(0, cs.ResourceChanges.Count, "ResourceChanges");
        }

        private static void TestMultiPlayerFleetIdentity()
        {
            // Fleets are diffed by global fleetId across BOTH players, independent of which player owns them.
            var prev = State(players: new[]
            {
                Player(0, Fleet(1), Fleet(2)),
                Player(1, Fleet(3)),
            });
            var curr = State(players: new[]
            {
                Player(0, Fleet(1)),          // fleet 2 removed
                Player(1, Fleet(3), Fleet(4)) // fleet 4 added
            });

            var cs = ChangeSet.Create(prev, curr);

            AssertEqual(1, cs.AddedFleets.Count, "AddedFleets");
            AssertEqual(4L, cs.AddedFleets[0].fleetId, "added fleetId");
            AssertEqual(1, cs.RemovedFleets.Count, "RemovedFleets");
            AssertEqual(2L, cs.RemovedFleets[0].fleetId, "removed fleetId");
            AssertEqual(2, cs.UpdatedFleets.Count, "UpdatedFleets (fleets 1 and 3 persist)");
        }

        private static void TestChangeSetAliasingAndDefaults()
        {
            // Contract: Current/Previous alias the exact inputs; ServerTick defaults to 0; collections are never null.
            var prev = State(planets: new[] { Planet(10, 0) });
            var curr = State(planets: new[] { Planet(10, 0) });

            var cs = ChangeSet.Create(prev, curr);

            AssertSame(curr, cs.Current, "Current aliases input");
            AssertSame(prev, cs.Previous, "Previous aliases input");
            AssertEqual(0L, cs.ServerTick, "default ServerTick");
            AssertNotNull(cs.AddedFleets, "AddedFleets not null");
            AssertNotNull(cs.RemovedFleets, "RemovedFleets not null");
            AssertNotNull(cs.UpdatedFleets, "UpdatedFleets not null");
            AssertNotNull(cs.AddedPlanets, "AddedPlanets not null");
            AssertNotNull(cs.RemovedPlanets, "RemovedPlanets not null");
            AssertNotNull(cs.PlanetOwnerChanges, "PlanetOwnerChanges not null");
            AssertNotNull(cs.ResourceChanges, "ResourceChanges not null");
        }

        // ---- builders (construct real production types; no algorithm here) ----

        private static GameState State(GameState.Player[] players = null, GameState.Planet[] planets = null)
        {
            return new GameState { players = players, planets = planets };
        }

        private static GameState.Player Player(int id, params GameState.FleetInfo[] fleets)
        {
            return new GameState.Player { id = id, fleets = fleets };
        }

        private static GameState.Player PlayerRes(int id, int gas, int mineral, int supply, params GameState.FleetInfo[] fleets)
        {
            return new GameState.Player { id = id, Gas = gas, Mineral = mineral, Supply = supply, fleets = fleets };
        }

        private static GameState.FleetInfo Fleet(long fleetId, int state = 0, int ownerId = 0)
        {
            return new GameState.FleetInfo { fleetId = fleetId, state = state, ownerId = ownerId };
        }

        private static GameState.FleetInfo FleetHp(long fleetId, int state, float hp, int ownerId = 0)
        {
            return new GameState.FleetInfo { fleetId = fleetId, state = state, ownerId = ownerId, HP = hp };
        }

        private static GameState.Planet Planet(int planetId, int owner)
        {
            return new GameState.Planet { planetId = planetId, owner = owner };
        }

        // ---- assertions ----

        private static void AssertEqual<T>(T expected, T actual, string what)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"{what}: expected {expected}, actual {actual}");
        }

        private static void AssertSame(object expected, object actual, string what)
        {
            if (!ReferenceEquals(expected, actual))
                throw new Exception($"{what}: expected same reference");
        }

        private static void AssertNotNull(object actual, string what)
        {
            if (actual == null)
                throw new Exception($"{what}: expected non-null");
        }
    }
}
