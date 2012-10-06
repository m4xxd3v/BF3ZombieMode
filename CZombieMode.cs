/*  CZombieMode - Copyright 2012 m4xx

 */

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

enum NoticeDisplayType { yell, say };

namespace PRoConEvents
{
	
	public class CZombieMode : PRoConPluginAPI, IPRoConPluginInterface
	{
		#region Constants

		const string HUMAN_TEAM = "1";

		const string ZOMBIE_TEAM = "2";

		const string BLANK_SQUAD = "0";

		const string FORCE_MOVE = "true";

		#endregion

		#region PluginSettings
		
		private int DebugLevel = 3; // 3 while in development, 2 when released

		private string CommandPrefix = "!zombie";

		private int AnnounceDisplayLength = 10;

		private NoticeDisplayType AnnounceDisplayType = NoticeDisplayType.yell;

		private int WarningDisplayLength = 15;

		private List<String> AdminUsers = new List<String>();

		private List<String> PlayerKickQueue = new List<String>();

		private ZombieModeKillTracker KillTracker = new ZombieModeKillTracker();
		
		private bool RematchEnabled = true; // true: round does not end, false: round ends

		#endregion


		#region GamePlayVars

		private List<CPlayerInfo> PlayerList = new List<CPlayerInfo>();

		private bool ZombieModeEnabled = true;

		private int MaxPlayers = 32;

		private int MinimumHumans = 3;

		private int MinimumZombies = 1;

		private int DeathsNeededToBeInfected = 1;

		private int ZombiesKilledToSurvive = 50; // TBD: to be made adaptive

		private bool ZombieKillLimitEnabled = true;

		private bool InfectSuicides = true;
		
		private List<String> TeamHuman = new List<String>();
		
		private List<String> TeamZombie = new List<String>();
		
		private List<String> FreshZombie = new List<String>();
		
		private bool IsBetweenRounds = false;
		
		private List<String> PatientZeroes = new List<String>();
			/* PatientZeroes keeps track of all the players that have been selected to
			   be the first zombie, to prevent the same player from being selected
			   over and over again. */
		
		private int KnownPlayerCount = 0;
		
		private int ServerSwitchedCount = 0;
		
		private List<String> Lottery = new List<String>();
			/* Pool of players to select first zombie from */
			
		private string PatientZero = null; // name of first zombie for the round
		
		private ZombieModePlayerState PlayerState = new ZombieModePlayerState();
		
		private bool CountingDownToNextRound = false;
		
		private SynchronizedNumbers NumRulesThreads  = new SynchronizedNumbers();

		#endregion


		#region DamagePercentageVars

		int Against1Or2Zombies = 5;  // 4+ to 1 ratio humans(>=75%]:zombies

		int AgainstAFewZombies = 10; // 4:1 to 3:2 ratio humans(75%-60%]:zombies

		int AgainstEqualNumbers = 15; // 3:2 to 2:3 ratio humans(60%-40%]:zombies

		int AgainstManyZombies = 30; // 2:3 to 1:4 ratio humans(40%-25%):zombies

		int AgainstCountlessZombies = 100; // 1 to 4+ ratio humans[<=25%):zombies

		#endregion

		private string[] ZombieWeapons = 
	    {
	        "Melee",
	        "Defib",
	        "Knife_RazorBlade",
	        "Knife",
	        "Repair Tool"
	    };

		#region WeaponList
		private List<String> WeaponList = new List<String>(new string[] {
            "870MCS",
            "AEK-971",
            "AKS-74u",
            "AN-94 Abakan",
            "AS Val",
            "DAO-12",
            "Defib",
            "F2000",
            "FAMAS",
            "FGM-148",
            "FIM92",
            "Glock18",
            "HK53",
            "jackhammer",
            "JNG90",
            "Knife_RazorBlade",
            "L96",
            "LSAT",
            "M416",
            "M417",
            "M1014",
            "M15 AT Mine",
            "M16A4",
            "M1911",
            "M240",
            "M249",
            "M26Mass",
            "M27IAR",
            "M320",
            "M39",
            "M40A5",
            "M4A1",
            "M60",
            "M67",
            "M9",
            "M93R",
            "Melee",
            "MG36",
            "Mk11",
            "Model98B",
            "MP7",
            "Pecheneg",
            "PP-19",
            "PP-2000",
            "QBB-95",
            "QBU-88",
            "QBZ-95",
            "Repair Tool",
            "RoadKill",
            "RPG-7",
            "RPK-74M",
            "SCAR-L",
            "SG 553 LB",
            "Siaga20k",
            "SKS",
            "SMAW",
            "SPAS-12",
            "SV98",
            "SVD",
            "Steyr AUG",
            "Taurus .44",
            "Type88",
            "USAS-12",
            "Weapons/A91/A91",
            "Weapons/AK74M/AK74",
            "Weapons/G36C/G36C",
            "Weapons/G3A3/G3A3",
            "Weapons/Gadgets/C4/C4",
            "Weapons/Gadgets/Claymore/Claymore",
            "Weapons/KH2002/KH2002",
            "Weapons/Knife/Knife",
            "Weapons/MagpulPDR/MagpulPDR",
            "Weapons/MP412Rex/MP412REX",
            "Weapons/MP443/MP443",
            "Weapons/MP443/MP443_GM",
            "Weapons/P90/P90",
            "Weapons/P90/P90_GM",
            "Weapons/Sa18IGLA/Sa18IGLA",
            "Weapons/SCAR-H/SCAR-H",
            "Weapons/UMP45/UMP45",
            "Weapons/XP1_L85A2/L85A2",
            "Weapons/XP2_ACR/ACR",
            "Weapons/XP2_L86/L86",
            "Weapons/XP2_MP5K/MP5K",
            "Weapons/XP2_MTAR/MTAR"
        });
		#endregion

		#region ZombieWeaponList
		private List<String> ZombieWeaponsEnabled = new List<String>(new string[] {
			"Repair Tool",
			"Defib",
			"Melee",
			"Knife_RazorBlade"
		});
		#endregion

		#region HumanWeaponList
		private List<String> HumanWeaponsEnabled = new List<String>(new string[] {
			"870MCS",
            "AEK-971",
            "AKS-74u",
            "AN-94 Abakan",
            "AS Val",
            "DAO-12",
            "Defib",
            "F2000",
            "FAMAS",
            "FGM-148",
            "FIM92",
            "Glock18",
            "HK53",
            "jackhammer",
            "JNG90",
            "Knife_RazorBlade",
            "L96",
            "LSAT",
            "M416",
            "M417",
            "M1014",
            "M15 AT Mine",
            "M16A4",
            "M1911",
            "M240",
            "M249",
            "M26Mass",
            "M27IAR",
            "M320",
            "M39",
            "M40A5",
            "M4A1",
            "M60",
            "M67",
            "M9",
            "M93R",
            "Melee",
            "MG36",
            "Mk11",
            "Model98B",
            "MP7",
            "Pecheneg",
            "PP-19",
            "PP-2000",
            "QBB-95",
            "QBU-88",
            "QBZ-95",
            "Repair Tool",
            "RoadKill",
            "RPG-7",
            "RPK-74M",
            "SCAR-L",
            "SG 553 LB",
            "Siaga20k",
            "SKS",
            "SMAW",
            "SPAS-12",
            "SV98",
            "SVD",
            "Steyr AUG",
            "Taurus .44",
            "Type88",
            "USAS-12",
            "Weapons/A91/A91",
            "Weapons/AK74M/AK74",
            "Weapons/G36C/G36C",
            "Weapons/G3A3/G3A3",
            "Weapons/Gadgets/C4/C4",
            "Weapons/Gadgets/Claymore/Claymore",
            "Weapons/KH2002/KH2002",
            "Weapons/Knife/Knife",
            "Weapons/MagpulPDR/MagpulPDR",
            "Weapons/MP412Rex/MP412REX",
            "Weapons/MP443/MP443",
            "Weapons/MP443/MP443_GM",
            "Weapons/P90/P90",
            "Weapons/P90/P90_GM",
            "Weapons/Sa18IGLA/Sa18IGLA",
            "Weapons/SCAR-H/SCAR-H",
            "Weapons/UMP45/UMP45",
            "Weapons/XP1_L85A2/L85A2",
            "Weapons/XP2_ACR/ACR",
            "Weapons/XP2_L86/L86",
            "Weapons/XP2_MP5K/MP5K",
            "Weapons/XP2_MTAR/MTAR"
		});
		#endregion

		#region EventHandlers

		/** EVENT HANDLERS **/
		public override void OnPlayerKickedByAdmin(string SoldierName, string reason) 
		{
			if (ZombieModeEnabled == false)
				return;
			
			DebugWrite("OnPlayerKickedByAdmin: " + SoldierName + ", reason: " + reason, 1);

			KillTracker.RemovePlayer(SoldierName);

			for(int i = 0; i < PlayerKickQueue.Count;i++)
			{
				CPlayerInfo Player = PlayerList[i];
				if (Player.SoldierName.Equals(SoldierName))
				{
					PlayerKickQueue.RemoveAt(i);
				}
			}
		}

		public override void OnPlayerAuthenticated(string SoldierName, string guid)
		{
			// Comes after OnPlayerJoin
			if (ZombieModeEnabled == false)
				return;

			DebugWrite("OnPlayerAuthenticated: " + SoldierName, 4);
			
			if (PlayerList.Count <= MaxPlayers) 
			{
				DebugWrite("OnPlayerAuthenticated: making " + SoldierName + " human", 3);
				MakeHuman(SoldierName);
				
				PlayerState.AddPlayer(SoldierName);
				return;
			}
			

			base.OnPlayerAuthenticated(SoldierName, guid);
			
			PlayerKickQueue.Add(SoldierName);

			ThreadStart kickPlayer = delegate
			{
				try
				{
					Sleep(10);
					ExecuteCommand("procon.protected.tasks.add", "CZombieMode", "0", "1", "1", "procon.protected.send", "admin.kickPlayer", SoldierName, String.Concat("Sorry, zombie mode is enabled and all slots are full :( Please join when there are less than ", MaxPlayers.ToString(), " players"));
					while (true)
					{
						if (!PlayerKickQueue.Contains(SoldierName))
							break;

						ExecuteCommand("procon.protected.tasks.add", "CZombieMode", "0", "1", "1", "procon.protected.send", "admin.kickPlayer", SoldierName, String.Concat("Sorry, zombie mode is enabled and all slots are full :( Please join when there are less than ", MaxPlayers.ToString(), " players"));
						Thread.Sleep(500);
					}
				}
				catch (System.Exception e)
				{
					ConsoleException("kickPlayer: " + e.ToString());
				}
			};

			Thread t = new Thread(kickPlayer);

			t.Start();
		}
		
		public override void OnPlayerJoin(string SoldierName)
		{
			// Comes before OnPlayerAuthenticated
			if (ZombieModeEnabled)
			{
				KillTracker.AddPlayer(SoldierName);
			}
		}
		
		public override void OnPlayerKilled(Kill info)
		{
			if (ZombieModeEnabled == false || CountingDownToNextRound == true)
				return;

			DebugWrite("OnPlayerKilled: " + info.Killer.SoldierName + " killed " + info.Victim.SoldierName + " with " + info.DamageType, 3);
			
			// Killed by admin?
			if (info.DamageType == "Death")
				return;

			String KillerName = info.Killer.SoldierName;

			String KillerTeam = info.Killer.TeamID.ToString();

			String VictimName = info.Victim.SoldierName;
			
			String VictimTeam = info.Victim.TeamID.ToString();

			String DamageType = info.DamageType;
			
			String InfectMessage = null;
			
			int RemainingHumans = TeamHuman.Count - 1;
			
			if (RemainingHumans == 0) 
			{
				InfectMessage = "*** Only " + RemainingHumans + " humans left!"; // TBD - custom message
			}
			else
			{
				InfectMessage = "*** No humans left!"; // TBD - custom message
			}

			if (ValidateWeapon(info.DamageType, KillerTeam) == false)
			{
				DebugWrite(String.Concat(KillerName, " invalid kill with ", info.DamageType, "!"), 2);

				KillPlayer(KillerName, "Bad weapon choice!"); // TBD - custom message

				return;
			}



			if (KillerTeam == HUMAN_TEAM && VictimTeam == ZOMBIE_TEAM)
			{
				KillTracker.ZombieKilled(KillerName, VictimName);

				DebugWrite(String.Concat("Human ", KillerName, " just killed zombie ", VictimName, " with ", DamageType), 3);
				
				TellAll("*** Humans killed " + KillTracker.GetZombiesKilled() + " of " + ZombiesKilledToSurvive + " zombies needed to win!"); // TBD - custom message
			}
			else if (KillerTeam == ZOMBIE_TEAM && VictimTeam == HUMAN_TEAM)
			{
				DebugWrite(String.Concat("Zombie ", KillerName, " just killed human ", VictimName, " with ", DamageType), 2);

				KillTracker.HumanKilled(KillerName, VictimName);

				if (KillTracker.GetPlayerHumanDeathCount(VictimName) == DeathsNeededToBeInfected)
				{					
					Infect(KillerName, VictimName);
					TellAll(InfectMessage, false); // do not overwrite Infect yell
				}
			}
			else if (KillerName == VictimName)
			{
				if (InfectSuicides)
				{
					DebugWrite("Suicide infected: " + VictimName, 2);
					Infect("Suicide ", VictimName);
					TellAll(InfectMessage, false); // do not overwrite Infect yell
				}
			}
			else if (KillerName == "")
			{
				if (InfectSuicides)
				{
					DebugWrite("Misfortune infect: " + VictimName, 2);
					Infect("Misfortune ", VictimName);
					TellAll(InfectMessage, false); // do not overwrite Infect yell
				}
			}


			
			// Victory conditions
			
			if (KillTracker.GetZombiesKilled() >= ZombiesKilledToSurvive) // TBD: to be made adaptive
			{
				string msg = "HUMANS WIN with " + KillTracker.GetZombiesKilled() + " zombies killed!"; // TBD - custom message
				DebugWrite("^2" + msg + "^0", 1);
				TellAll(msg);
				CountdownNextRound(HUMAN_TEAM);
			}
			else if (TeamHuman.Count == 0 && TeamZombie.Count > MinimumZombies)
			{
				string msg = "ZOMBIES WIN, all humans infected!"; // TBD - custom message
				DebugWrite("^7" + msg + "^0", 1);
				TellAll(msg);
				CountdownNextRound(ZOMBIE_TEAM);
			}

		}

		public override void OnListPlayers(List<CPlayerInfo> Players, CPlayerSubset Subset)
		{
			PlayerList = Players;
			
			DebugWrite("OnListPlayers: " + Players.Count + " players", 3);
			
			if (ZombieModeEnabled == false) return;
			
			TeamHuman.Clear();
			TeamZombie.Clear();

			foreach (CPlayerInfo Player in Players)
			{
				KillTracker.AddPlayer(Player.SoldierName.ToString());
				// Team tracking
				if (Player.TeamID == 1 && !TeamHuman.Contains(Player.SoldierName)) {
					TeamHuman.Add(Player.SoldierName);
					DebugWrite("OnListPlayers: added " + Player.SoldierName + " to TeamHuman (" + TeamHuman.Count + ")", 7);
				}
				if (Player.TeamID == 2 && !TeamZombie.Contains(Player.SoldierName)) {
					TeamZombie.Add(Player.SoldierName);
					DebugWrite("OnListPlayers: added " + Player.SoldierName + " to TeamZombie (" + TeamZombie.Count + ")", 7);
				}					
			}
			
			if (IsBetweenRounds)
			{
				KnownPlayerCount = TeamZombie.Count + TeamHuman.Count;
			}
		}

		public override void OnGlobalChat(string PlayerName, string Message)
		{
			HandleChat(PlayerName, Message, -1, -1);
		}

		public override void OnTeamChat(string PlayerName, string Message, int TeamId)
		{
			HandleChat(PlayerName, Message, TeamId, -1);
		}

		public override void OnSquadChat(string PlayerName, string Message, int TeamId, int SquadId)
		{
			HandleChat(PlayerName, Message, TeamId, SquadId);
		}
		
		public void HandleChat(string PlayerName, string Message, int TeamId, int SquadId)
		{				
			String CleanMessage = Message.ToLower().Trim();

			List<string> MessagePieces = new List<string>(CleanMessage.Split(' '));

			String Command = MessagePieces[0];

			if (!Command.StartsWith(CommandPrefix.ToLower()))
				return;
				
			DebugWrite("Command: " + Message + " => " + CleanMessage, 3);
			
			if (CommandPrefix.Length > 1 && Command == CommandPrefix)
			{
				/*
				If Message is: !zombie command arg1 arg2
				Then remove "!zombie" from the MessagePieces and reset Command
				to be the value of 'command'.
				*/
				MessagePieces.Remove(CommandPrefix);
				Command = MessagePieces[0];
			}
			else
			{
				/*
				If command is: !zcmd arg1 arg2
				Then remove "!z" from Command
				*/
				Match CommandMatch = Regex.Match(Command, "^" + CommandPrefix + @"([^\s]+)", RegexOptions.IgnoreCase);
				if (CommandMatch.Success)
				{
					Command = CommandMatch.Groups[1].Value;
				}
			}
			
			DebugWrite("Command without prefix: " + Command, 4);
			
			switch (Command)
			{
				case "infect":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count != 2) return;
					Infect("Admin", MessagePieces[1]);
					break;
				case "heal":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count != 2) return;
					MakeHuman(MessagePieces[1]);
					break;
				case "rematch":
					if (!RematchEnabled)
					{
						TellPlayer("Rematch mode is not enabled, command ignored!", PlayerName);
						return;
					}
					MakeTeamsRequest(PlayerName);
					break;
				case "restart":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					RestartRound();
					break;
				case "next":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					NextRound();
					break;
				case "mode":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count < 2) return;
					if (MessagePieces[1] == "on")
						ZombieModeEnabled = true;
					else if (MessagePieces[1] == "off")
						ZombieModeEnabled = false;
					break;
				case "rules":
					TellRules(PlayerName);
					break;
				case "warn":
					if (MessagePieces.Count < 3) return;
					string WarningMessage = String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray());

					DebugWrite(WarningMessage, 1);
					Warn(MessagePieces[1], WarningMessage);
					break;

				case "kill":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count < 2) return;
					string KillMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";

					DebugWrite(KillMessage, 1);
					KillPlayer(MessagePieces[1], KillMessage);
					break;

				case "kick":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count < 2) return;
					string KickMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";

					KickPlayer(MessagePieces[1], KickMessage);
					break;
				case "test":
					DebugWrite("loopz", 2);
					DebugWrite(FrostbitePlayerInfoList.Values.Count.ToString(), 2);
					foreach (CPlayerInfo Player in FrostbitePlayerInfoList.Values)
					{
						DebugWrite("looping", 2);
						String testmessage = Player.SoldierName;
						DebugWrite(testmessage, 2);
					}
					break;
				default: // "help"
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Commands: rules, rematch, warn", PlayerName);
					}
					else
					{
						TellPlayer("Commands: infect, heal, rematch, restart, next, mode, rules, warn, kill, kick", PlayerName);
					}
					break;
			}

		}

		public override void OnServerInfo(CServerInfo serverInfo)
		{
			// This is just to test debug logging
			DebugWrite("Debug level = " + DebugLevel + " ...", 7);
			
			if (IsBetweenRounds)
			{
				KnownPlayerCount = TeamHuman.Count + TeamZombie.Count;
			}
		}

		public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
		{
			if (ZombieModeEnabled == false) return;

			bool wasZombie = TeamZombie.Contains(soldierName);
			bool wasHuman = TeamHuman.Contains(soldierName);

			// Ignore squad changes within team
			if (teamId == 1 && wasHuman) return;
			if (teamId == 2 && wasZombie) return;
			if (!(teamId == 1 || teamId == 2)) {
				ConsoleError("OnPlayerTeamChange unknown teamId = " + teamId);
				return;
			}
			
			string team = (wasHuman) ? "HUMAN" : "ZOMBIE";
			DebugWrite("OnPlayerTeamChange: " + soldierName + "(" + team + ") to " + teamId, 3);
			
			if (!IsBetweenRounds)
			{
				if (teamId == 1 && wasZombie) // to humans
				{
					// Switching to human team is not allowed
					TellPlayer("Don't switch to the human team! Sending you back to zombies!", soldierName); // TBD - custom message

					ForceMove(soldierName, ZOMBIE_TEAM);

					if (TeamHuman.Contains(soldierName)) TeamHuman.Remove(soldierName);
					if (!TeamZombie.Contains(soldierName)) TeamZombie.Add(soldierName);

				} 
				else if (teamId == 2 && wasHuman) // to zombies
				{
					// Switching to the zombie team is okay
					FreshZombie.Add(soldierName);

					if (TeamHuman.Contains(soldierName)) TeamHuman.Remove(soldierName);
					if (!TeamZombie.Contains(soldierName)) TeamZombie.Add(soldierName);
				}
			} else { // between rounds, server is swapping teams
				
				int ZombieCount = 0;
				
				if (teamId == 1) // to humans
				{
					++ServerSwitchedCount;
					
					// Add to the lottery if eligible
					if (!PatientZeroes.Contains(soldierName)) Lottery.Add(soldierName);

					if (TeamZombie.Contains(soldierName)) TeamZombie.Remove(soldierName);
					if (!TeamHuman.Contains(soldierName)) TeamHuman.Add(soldierName);
				} 
				else if (teamId == 2) // to zombies
				{
					++ServerSwitchedCount;

					// Switch back
					MakeHuman(soldierName);
				}
				
				// When the server is done swapping players, process patient zero
				if (ServerSwitchedCount >= KnownPlayerCount)
				{
					while (ZombieCount < MinimumZombies)
					{
						if (Lottery.Count == 0)
						{
							// loop through players, adding to Lottery if eligible
							foreach (CPlayerInfo p in PlayerList)
							{
								if (!PatientZeroes.Contains(p.SoldierName))
								{
									Lottery.Add(p.SoldierName);
								}
							}
						}
						
						if (Lottery.Count == 0)
						{
							ConsoleWarn("OnPlayerTeamChange, can't find an eligible player for patient zero!");
							PatientZeroes.Clear();
							Lottery.Add(soldierName);
						}
						
						Random rand = new Random();
						int choice = (Lottery.Count == 1) ? 0 : (rand.Next(Lottery.Count));
						PatientZero = Lottery[choice];
						DebugWrite("OnPlayerTeamChange: lottery selected " + PatientZero + " as a zombie!", 3);
						Lottery.Remove(PatientZero);
						

						MakeZombie(PatientZero);

						if (PatientZeroes.Count > (KnownPlayerCount/2)) PatientZeroes.Clear();

						PatientZeroes.Add(PatientZero);
						
						++ZombieCount;
					}
					
					DebugWrite("OnPlayerTeamChange: making " + PatientZero + " the first zombie!", 2);
					
					ServerSwitchedCount = 0;
				}

			}
		}


		public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
		{
			if (ZombieModeEnabled == false) 
			{
				IsBetweenRounds = false;
				return;
			}
			
			// Check if this is the first spawn of the round
			if (IsBetweenRounds) {
				IsBetweenRounds = false;
				DebugWrite("OnPlayerSpawned: announcing first zombie is " + PatientZero, 3);
				TellAll(PatientZero + " is the first zombie!"); // TBD - custom message
			}
			
			int n = PlayerState.GetSpawnCount(soldierName);
			
			// Tell zombies they can only use hand to hand weapons
			if (FreshZombie.Contains(soldierName)) 
			{
				DebugWrite("OnPlayerSpawned " + soldierName + " is fresh zombie!", 3);
				FreshZombie.Remove(soldierName);
				TellPlayer("You are now a zombie! Use a knife/defib/repair tool only!", soldierName); // TBD - custom message
			} else if (PlayerState.GetWelcomeCount(soldierName) == 0) {
				String Separator = " ";
				if (CommandPrefix.Length == 1) Separator = "";
				TellPlayer("Welcome to Zombie Mode! Type '" + CommandPrefix + Separator + "rules' for instructions on how to play", soldierName); // TBD - custom message
				PlayerState.SetWelcomeCount(soldierName, 1);
			} else if (n == 0) {
				if (!TeamHuman.Contains(soldierName)) ConsoleError("OnPlayerSpawned: " + soldierName + " should be human, but not present in TeamHuman list!");
				TellPlayer("You are a human! Shoot zombies, don't use explosives, don't let zombies get near you!", soldierName); // TBD - custom message
			}
			
			PlayerState.SetSpawnCount(soldierName, n+1);
		}

		public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
		{
			DebugWrite("OnLevelLoaded, updating player list", 3);
			
			// We have 5 seconds before the server swaps teams, make sure we are up to date
			ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
			
			// Reset the team switching counter
			ServerSwitchedCount = 0;
			
			// Reset the utility lists
			FreshZombie.Clear();
			Lottery.Clear();
			
			// Reset patient zero
			PatientZero = null;
			
			// Reset per-round player states
			PlayerState.ResetPerRound();
			
			// Reset kill tracker
			KillTracker.ResetPerRound();

			// Reset countdown
			CountingDownToNextRound = false;
		}

		public override void OnRoundOver(int winningTeamId)
		{
			DebugWrite("OnRoundOver, IsBetweenRounds set to True", 4);
			IsBetweenRounds = true;
		}


		#endregion


		#region PluginMethods
		/** PLUGIN RELATED SHIT **/
		#region PluginEventHandlers
		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
		{
			RegisterEvents(GetType().Name, 
				"OnPlayerKilled",
				"OnListPlayers",
				"OnSquadChat",
				"OnPlayerAuthenticated",
				"OnPlayerKickedByAdmin",
				"OnServerInfo",
				"OnPlayerTeamChange",
				"OnPlayerSpawned",
				"OnLevelLoaded"
				);
		}

		public void OnPluginEnable()
		{
			//System.Diagnostics.Debugger.Break();
			ConsoleLog("^b^2Enabled... It's Game Time!");
		}

		public void OnPluginDisable()
		{
			ConsoleLog("^b^2Disabled :(");
			Reset();
		}
		#endregion

		// Plugin details
		public string GetPluginName()
		{
			return "Zombie Mode";
		}

		public string GetPluginVersion()
		{
			return "0.1.0";
		}

		public string GetPluginAuthor()
		{
			return "m4xxd3v";
		}

		public string GetPluginWebsite()
		{
			return "http://www.phogue.net";
		}

		public string GetPluginDescription()
		{
			return "This plugin enables a zombie infection mode type game play";
		}


		// Plugin variables
		public List<CPluginVariable> GetDisplayPluginVariables()
		{
			List<CPluginVariable> lstReturn = new List<CPluginVariable>();

			lstReturn.Add(new CPluginVariable("Game Settings|Zombie Mode Enabled", typeof(enumBoolYesNo), ZombieModeEnabled ? enumBoolYesNo.Yes : enumBoolYesNo.No));

			lstReturn.Add(new CPluginVariable("Admin Settings|Command Prefix", CommandPrefix.GetType(), CommandPrefix));

			lstReturn.Add(new CPluginVariable("Admin Settings|Announce Display Length", AnnounceDisplayLength.GetType(), AnnounceDisplayLength));

			lstReturn.Add(new CPluginVariable("Admin Settings|Warning Display Length", WarningDisplayLength.GetType(), WarningDisplayLength));

			lstReturn.Add(new CPluginVariable("Admin Settings|Debug Level", DebugLevel.GetType(), DebugLevel));

			lstReturn.Add(new CPluginVariable("Admin Settings|Admin Users", typeof(string[]), AdminUsers.ToArray()));

			lstReturn.Add(new CPluginVariable("Game Settings|Max Players", MaxPlayers.GetType(), MaxPlayers));

			lstReturn.Add(new CPluginVariable("Game Settings|Minimum Zombies", MinimumZombies.GetType(), MinimumZombies));

			lstReturn.Add(new CPluginVariable("Game Settings|Minimum Humans", MinimumHumans.GetType(), MinimumHumans));

			lstReturn.Add(new CPluginVariable("Game Settings|Zombie Kill Limit Enabled", typeof(enumBoolOnOff), ZombieKillLimitEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));

			if (ZombieKillLimitEnabled)
				lstReturn.Add(new CPluginVariable("Game Settings|Zombies Killed To Survive", ZombiesKilledToSurvive.GetType(), ZombiesKilledToSurvive));

			lstReturn.Add(new CPluginVariable("Game Settings|Deaths Needed To Be Infected", DeathsNeededToBeInfected.GetType(), DeathsNeededToBeInfected));
			

			lstReturn.Add(new CPluginVariable("Game Settings|Infect Suicides", typeof(enumBoolOnOff), InfectSuicides ? enumBoolOnOff.On : enumBoolOnOff.Off));

			lstReturn.Add(new CPluginVariable("Game Settings|Rematch Enabled", typeof(enumBoolOnOff), RematchEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));
			
			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against 1 Or 2 Zombies", Against1Or2Zombies.GetType(), Against1Or2Zombies));

			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against A Few Zombies", AgainstAFewZombies.GetType(), AgainstAFewZombies));

			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Equal Numbers", AgainstEqualNumbers.GetType(), AgainstEqualNumbers));

			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Many Zombies", AgainstManyZombies.GetType(), AgainstManyZombies));

			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Countless Zombies", AgainstCountlessZombies.GetType(), AgainstCountlessZombies));

			foreach (PRoCon.Core.Players.Items.Weapon Weapon in WeaponDictionaryByLocalizedName.Values)
			{
				String WeaponDamage = Weapon.Damage.ToString();

				if (WeaponDamage.Equals("Nonlethal") || WeaponDamage.Equals("None") || WeaponDamage.Equals("Suicide"))
					continue;

				String WeaponName = Weapon.Name.ToString();
				lstReturn.Add(new CPluginVariable(String.Concat("Zombie Weapons|Z -", WeaponName), typeof(enumBoolOnOff), ZombieWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
				lstReturn.Add(new CPluginVariable(String.Concat("Human Weapons|H -", WeaponName), typeof(enumBoolOnOff), HumanWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
			}


			return lstReturn;
		}

		public List<CPluginVariable> GetPluginVariables()
		{
			List<CPluginVariable> lstReturn = GetDisplayPluginVariables();

			return lstReturn;
		}

		public void SetPluginVariable(string Name, string Value)
		{
			ThreadStart MyThread = delegate
			{
				try
				{
					int PipeIndex = Name.IndexOf('|');
					if (PipeIndex >= 0)
					{
						PipeIndex++;
						Name = Name.Substring(PipeIndex, Name.Length - PipeIndex);
					}

					BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

					String PropertyName = Name.Replace(" ", "");

					FieldInfo Field = GetType().GetField(PropertyName, Flags);

					Dictionary<int, Type> EasyTypeDict = new Dictionary<int, Type>();
					EasyTypeDict.Add(0, typeof(int));
					EasyTypeDict.Add(1, typeof(Int16));
					EasyTypeDict.Add(2, typeof(Int32));
					EasyTypeDict.Add(3, typeof(Int64));
					EasyTypeDict.Add(4, typeof(float));
					EasyTypeDict.Add(5, typeof(long));
					EasyTypeDict.Add(6, typeof(String));
					EasyTypeDict.Add(7, typeof(string));

					Dictionary<int, Type> BoolDict = new Dictionary<int, Type>();
					BoolDict.Add(0, typeof(Boolean));
					BoolDict.Add(1, typeof(bool));

					Dictionary<int, Type> ListStrDict = new Dictionary<int, Type>();
					ListStrDict.Add(0, typeof(List<String>));
					ListStrDict.Add(1, typeof(List<string>));
					
					

					if (Field != null)
					{
						
						Type FieldType = Field.GetValue(this).GetType();
						if (EasyTypeDict.ContainsValue(FieldType))
							Field.SetValue(this, TypeDescriptor.GetConverter(FieldType).ConvertFromString(Value));
						else if (ListStrDict.ContainsValue(FieldType))
							Field.SetValue(this, new List<string>(CPluginVariable.DecodeStringArray(Value)));
						else if (BoolDict.ContainsValue(FieldType))
							if (Value == "Yes" || Value == "On")
								Field.SetValue(this, true);
							else
								Field.SetValue(this, false);
					}
					else
					{
						String WeaponName = Name.Substring(3, Name.Length - 3);

						if (WeaponList.IndexOf(WeaponName) >= 0)
						{
							String WeaponType = Name.Substring(0, 3);

							if (WeaponType == "H -")
							{
								if (Value == "On")
									EnableHumanWeapon(WeaponName);
								else
									DisableHumanWeapon(WeaponName);
							}
							else
							{
								if (Value == "On")
									EnableZombieWeapon(WeaponName);
								else
									DisableZombieWeapon(WeaponName);
							}

						}
					}
				}
				catch (System.Exception e)
				{
					ConsoleException("MyThread: " + e.ToString());
				}
			};

			Thread t = new Thread(MyThread);

			t.Start();

			
		}
		#endregion



		/** PRIVATE METHODS **/

		#region RoundCommands

		private void RestartRound()
		{
			ExecuteCommand("procon.protected.send", "mapList.restartRound");
		}

		private void NextRound()
		{
			ExecuteCommand("procon.protected.send", "mapList.runNextRound");
		}
		
		private void CountdownNextRound(string WinningTeam)
		{
			
			CountingDownToNextRound = true;
			
			DebugWrite("CountdownNextRound started", 2);
			
			ThreadStart countdown = delegate
			{
				try
				{
					if (RematchEnabled)
					{
						String Separator = " ";
						if (CommandPrefix.Length == 1) Separator = "";
						TellAll("Type '" + CommandPrefix + Separator + " rematch' to start another match in the same round without changing the map"); // TBD - custom message
						
						DebugWrite("CountdownNextRound ended with rematch mode enabled", 2);
					}
					else
					{
						Sleep(AnnounceDisplayLength);
						TellAll("Next round will start in " + (2*AnnounceDisplayLength) + " seconds");
						Sleep(AnnounceDisplayLength);
						TellAll("Next round will start in " + (AnnounceDisplayLength) + " seconds");
						Sleep(AnnounceDisplayLength);
						TellAll("Next round will start now!");
						Sleep(5);
						
						DebugWrite("CountdownNextRound thread: end round with winner teamID = " + "WinningTeam", 3);
						ExecuteCommand("procon.protected.send", "mapList.endRound", WinningTeam);
					}
					
				}
				catch (Exception e)
				{
					ConsoleException("countdown: " + e.ToString());
				}
				finally
				{
					CountingDownToNextRound = false;
				}
			};

			Thread t = new Thread(countdown);

			t.Start();
			
			Thread.Sleep(2);
		}

		#endregion


		#region PlayerPunishmentCommands

		private void Warn(String PlayerName, String Message)
		{
			ExecuteCommand("procon.protected.send", "admin.yell", Message, WarningDisplayLength.ToString(), "all", PlayerName);
		}

		private void KillPlayerAfterDelay(string PlayerName, int Delay)
		{
			DebugWrite("KillPlayerAfterDelay: " + PlayerName + " after " + Delay + " seconds", 3);
			ExecuteCommand("procon.protected.tasks.add", "KillPlayerAfterDelay", Delay.ToString(), "0", "1", "procon.protected.send", "admin.killPlayer", PlayerName);
		}

		private void KillPlayer(string PlayerName, string Reason)
		{
			KillPlayerAfterDelay(PlayerName, 1);

			if (!String.IsNullOrEmpty(Reason))
				Announce(String.Concat(PlayerName, ": ", Reason));
		}

		private void KickPlayerDelayed(string PlayerName, string Reason, int SecsToDelay)
		{
			ExecuteCommand("procon.protected.tasks.add", "ZombieKickUser", SecsToDelay.ToString(), "1", "1", "procon.protected.send", "admin.kickPlayer", PlayerName, Reason);
		}

		private void KickPlayer(string PlayerName, string Reason)
		{
			ExecuteCommand("procon.protected.send", "admin.kickPlayer", PlayerName, Reason);

			if (Reason.Length > 0)
				Announce(String.Concat(PlayerName, "kicked for: ", Reason));
		}

		#endregion

		#region TeamMethods

		private void RequestPlayersList()
		{
			ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
		}

		private void MakeTeamsRequest(string PlayerName)
		{
			TellPlayer("*** Teams are being generated!", PlayerName); // TBD - custom message

			DebugWrite("Teams being generated", 2);

			MakeTeams();
		}

		public void Infect(string Carrier, string Victim)
		{
			Announce(String.Concat(Carrier, " just infected ", Victim)); // TBD - custom message

			MakeZombie(Victim);
			
			AdaptDamage();
		}

		private void MakeHuman(string PlayerName)
		{
			Announce(String.Concat(PlayerName, " has join the fight for survival!")); // TBD - custom message
			
			DebugWrite("MakeHuman: " + PlayerName, 3);

			ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, HUMAN_TEAM, BLANK_SQUAD, FORCE_MOVE);
			
			if (TeamZombie.Contains(PlayerName)) TeamZombie.Remove(PlayerName);
			if (!TeamHuman.Contains(PlayerName)) TeamHuman.Add(PlayerName);
		}
		
		private void ForceMove(string PlayerName, string TeamId)
		{
			ThreadStart forceMove = delegate
			{
				try
				{
					// Kill player requires a delay to work correctly
					
					ExecuteCommand("procon.protected.send", "admin.killPlayer", PlayerName);
					
					Thread.Sleep(200);
					
					// Now do the move

					ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, TeamId, BLANK_SQUAD, FORCE_MOVE);					
				}
				catch (Exception e)
				{
					ConsoleException("forceMove: " + e.ToString());
				}
			};
			
			Thread t = new Thread(forceMove);

			t.Start();
			
			Thread.Sleep(2);
			
			DebugWrite("ForceMove " + PlayerName + " to " + TeamId, 3);
		}

		private void MakeZombie(string PlayerName)
		{
			DebugWrite("MakeHuman: " + PlayerName, 3);

			ForceMove(PlayerName, ZOMBIE_TEAM);
			
			if (TeamHuman.Contains(PlayerName)) TeamHuman.Remove(PlayerName);
			if (!TeamZombie.Contains(PlayerName)) TeamZombie.Add(PlayerName);			
			
			FreshZombie.Add(PlayerName);
		}


		private void MakeTeams()
		{
			ThreadStart makeTeams = delegate
			{
				try
				{
					IsBetweenRounds = true;
					
					Sleep(5); // allow time to update player list
					
					// First, kill all the former zombies to prepare for team switches
					
					List<String> tmp = new List<String>();
					
					foreach (String z in TeamZombie)
					{
						// We are managing the delay manually, so don't use KillPlayerAfterDelay
						ExecuteCommand("procon.protected.send", "admin.killPlayer", z);
						tmp.Add(z);
						Thread.Sleep(100);
					}

					// Then, move them to human team
					// We can't use TeamZombie here, because MakeHuman modifies it
					
					foreach (String z in tmp)
					{
						MakeHuman(z);
						Thread.Sleep(25);
					}
					
					// Fill the lottery pool for selecting patient zero
					
					Lottery.Clear();
					
					foreach (String h in TeamHuman)
					{
						if (!PatientZeroes.Contains(h)) Lottery.Add(h);
					}

					// Sanity check
					
					if (Lottery.Count < MinimumZombies)
					{
						ConsoleWarn("makeTeams, can't find enough eligible players for patient zero!");
						
						PatientZeroes.Clear();
						Lottery.Clear();
						
						for (int i = 0; i < TeamHuman.Count; ++i)
						{
							Lottery.Add(TeamHuman[i]);
							if ((i + 1) >= MinimumZombies) break;
						}
					}
					
					// Choose patient zero randomly from lottery pool
					
					FreshZombie.Clear();
					
					Random rand = new Random();
					
					int ZombieCount = 0;
					
					while (ZombieCount < MinimumZombies)
					{
						int choice = (Lottery.Count == 1) ? 0 : (rand.Next(Lottery.Count));
						PatientZero = Lottery[choice];
						Lottery.Remove(PatientZero);
						
						Infect("Patient Zero ", PatientZero);
						++ZombieCount;
						
						if (PatientZeroes.Count > (KnownPlayerCount/2)) PatientZeroes.Clear();
						
						PatientZeroes.Add(PatientZero);
					}
					

					DebugWrite("makeTeams: lottery selected " + PatientZero + " as first zombie!", 2);

					DebugWrite("makeTeams: ready for another round!", 2);
					
					TellAll("*** Spawn now, Zombie Mode is on!"); // TBD - custom message
					
					// Reset state

					Lottery.Clear();
					KnownPlayerCount = 0;
					ServerSwitchedCount = 0;
					CountingDownToNextRound = false;
					
					PlayerState.ResetPerMatch();
					KillTracker.ResetPerMatch();

					/* IsBetweenRounds is set back to false in OnPlayerSpawned */
					
				} 
				catch (Exception e)
				{
					ConsoleException("nukeZombies: " + e.ToString());
				}
			};
			
			// Update the player lists
			
			RequestPlayersList();
			
			// Tell everyone to hold on tight
			
			TellAll("*** PREPARE TO BE MOVED, new round starting, same map level!"); // TBD - custom message
			
			Thread t = new Thread(makeTeams);

			t.Start();
			
			Thread.Sleep(2);
		}

		#endregion

		#region WeaponMethods

		private void DisableZombieWeapon(String WeaponName)
		{
			int Index = ZombieWeaponsEnabled.IndexOf(WeaponName);
			if (Index >= 0)
				ZombieWeaponsEnabled.RemoveAt(Index);
		}

		private void DisableHumanWeapon(String WeaponName)
		{
			int Index = HumanWeaponsEnabled.IndexOf(WeaponName);
			if (Index >= 0)
				HumanWeaponsEnabled.RemoveAt(Index);
		}

		private void EnableZombieWeapon(String WeaponName)
		{
			int Index = ZombieWeaponsEnabled.IndexOf(WeaponName);
			if (Index < 0)
				ZombieWeaponsEnabled.Add(WeaponName);
		}

		private void EnableHumanWeapon(String WeaponName)
		{
			int Index = HumanWeaponsEnabled.IndexOf(WeaponName);
			if (Index < 0)
				HumanWeaponsEnabled.Add(WeaponName);

		}

		private bool ValidateWeapon(string Weapon, string TEAM_CONST)
		{

			if (
				(TEAM_CONST == HUMAN_TEAM && HumanWeaponsEnabled.IndexOf(Weapon) >= 0) || 
				(TEAM_CONST == ZOMBIE_TEAM && ZombieWeaponsEnabled.IndexOf(Weapon) >= 0)
				)
				return true;
			
			return false;
		}
		
		private void AdaptDamage()
		{
			double HumanCount = (TeamHuman.Count == 0) ? 1 : TeamHuman.Count;
			double ZombieCount = (TeamZombie.Count == 0) ? 1 : TeamZombie.Count;
			double RatioHumansToZombies = (HumanCount / ZombieCount) * 100.0;
			int BulletDamage = 5;
			
			
			if (RatioHumansToZombies >= 75.0)
			{
				BulletDamage = Against1Or2Zombies;
			}
			else if (RatioHumansToZombies < 75.0 && RatioHumansToZombies >= 60.0)
			{
				BulletDamage = AgainstAFewZombies;
			}
			else if (RatioHumansToZombies < 60.0 && RatioHumansToZombies >= 40.0)
			{
				BulletDamage = AgainstEqualNumbers;
			}
			else if (RatioHumansToZombies < 40.0 && RatioHumansToZombies > 25.0)
			{
				BulletDamage = AgainstManyZombies;
			}
			else // <= 25.0
			{
				BulletDamage = AgainstCountlessZombies;
			}
			
			ExecuteCommand("procon.protected.send", "vars.bulletDamage", BulletDamage.ToString());
			DebugWrite("AdaptDamage: Humans(" + HumanCount + "):Zombies(" + ZombieCount + "), bullet damage set to " + BulletDamage + "%", 3);

		}

		#endregion


		#region Utilities

		private bool IsAdmin(string PlayerName)
		{
			bool AdminFlag = AdminUsers.Contains(PlayerName);
			if (AdminFlag)
			{
				TellAll(PlayerName + " is an admin");
				DebugWrite("IsAdmin: " + PlayerName + " is an admin", 3);
			}
			return AdminFlag;
		}

		private void ConsoleWrite(string str)
		{
			ExecuteCommand("procon.protected.pluginconsole.write", str);
		}

		private void Announce(string Message)
		{
			if (IsBetweenRounds) return;
			ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), AnnounceDisplayType.ToString());
		}

		private void TellAll(string Message, bool AlsoYell)
		{
			// Yell and say
			if (IsBetweenRounds) return;
			if (AlsoYell) Announce(Message);
			ExecuteCommand("procon.protected.send", "admin.say", Message, "all");
		}

		private void TellAll(string Message)
		{
			TellAll(Message, true);
		}
		
		private void TellTeam(string Message, string TeamId, bool AlsoYell)
		{
			// Yell and say
			if (IsBetweenRounds) return;
			if (AlsoYell) ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), "team", TeamId);
			ExecuteCommand("procon.protected.send", "admin.say", Message, "team", TeamId);
		}

		private void TellTeam(string Message, string TeamId)
		{
			TellTeam(Message, TeamId, true);
		}
		
		private void TellPlayer(string Message, string SoldierName, bool AlsoYell)
		{
			// Yell and say
			if (IsBetweenRounds) return;
			if (AlsoYell) ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), "player", SoldierName);
			ExecuteCommand("procon.protected.send", "admin.say", Message, "player", SoldierName);
		}
				
		private void TellPlayer(string Message, string SoldierName)
		{
			TellPlayer(Message, SoldierName, true);
		}

		private void TellRules(string SoldierName)
		{
			int Delay = 5;
			List<String> Rules = new List<String>();
			// TBD - custom message
			Rules.Add("US team are humans, RU are zombies");
			Rules.Add("Round starts with only one zombie");
			Rules.Add("Zombies are hard to kill");
			Rules.Add("Zombies use knife/defib/repair tool only!");
			Rules.Add("Humans use guns only, no explosives!");
			Rules.Add("Every human a zombie kills becomes a zombie!");
			Rules.Add("Humans win by killing " + ZombiesKilledToSurvive + " zombies");
			Rules.Add("Zombies win by killing all humans");
			
			String RuleNum = null;
			int i = 1;
			
			ThreadStart tellRules = delegate
			{
				try
				{
					foreach (String r in Rules)
					{
						RuleNum = "R" + i + " of " + Rules.Count + ") ";
						i = i + 1;
						Sleep(Delay);
						TellPlayer(r, SoldierName);
					}
				}
				catch (Exception e)
				{
					ConsoleException("tellRules: " + e.ToString());
				}
				finally
				{
					lock (NumRulesThreads)
					{
						NumRulesThreads.IntVal = NumRulesThreads.IntVal - 1;
						if (NumRulesThreads.IntVal < 0) NumRulesThreads.IntVal = 0;
					}
				}
			};
			
			bool IsTooMany = false;
			
			lock (NumRulesThreads)
			{
				if (NumRulesThreads.IntVal >= 4) 
				{
					IsTooMany = true;
				}
				else
				{
					NumRulesThreads.IntVal = NumRulesThreads.IntVal + 1;
				}
			}
			
			if (IsTooMany)
			{
				TellPlayer("Rules plugin is busy, try again in 15 seconds", SoldierName);
				return;
			}
			
			Thread t = new Thread(tellRules);

			t.Start();
			
			Thread.Sleep(2);
		}

		private void Reset()
		{
			PlayerList.Clear();
			TeamHuman.Clear();
			TeamZombie.Clear();
			FreshZombie.Clear();
			PatientZeroes.Clear();
			Lottery.Clear();
			PlayerState.ClearAll();
			KnownPlayerCount = 0;
			ServerSwitchedCount = 0;
			PatientZero = null;
			CountingDownToNextRound = false;
		}

		private enum MessageType { Warning, Error, Exception, Normal };

		private String FormatMessage(String msg, MessageType type)
		{
			String prefix = "[^b" + GetPluginName() + "^n] ";

			if (type.Equals(MessageType.Warning))
				prefix += "^1^bWARNING^0^n: ";
			else if (type.Equals(MessageType.Error))
				prefix += "^1^bERROR^0^n: ";
			else if (type.Equals(MessageType.Exception))
				prefix += "^1^bEXCEPTION^0^n: ";

			return prefix + msg;
		}


		private void ConsoleLog(string msg, MessageType type)
		{
			ConsoleWrite(FormatMessage(msg, type));
		}

		private void ConsoleLog(string msg)
		{
			ConsoleLog(msg, MessageType.Normal);
		}

		private void ConsoleWarn(String msg)
		{
			ConsoleLog(msg, MessageType.Warning);
		}

		private void ConsoleError(String msg)
		{
			ConsoleLog(msg, MessageType.Error);
		}

		private void ConsoleException(String msg)
		{
			ConsoleLog(msg, MessageType.Exception);
		}

		private void DebugWrite(string msg, int level)
		{
			if (DebugLevel >= level) ConsoleLog(msg, MessageType.Normal);
		}
		
		private void Sleep(int Seconds)
		{
			Thread.Sleep(Seconds * 1000);
		}

		#endregion

	}

	enum ZombieModeTeam  {Human,Zombie};

	struct ZombieModeKillTrackerKills
	{
		public int KillsAsZombie;

		public int KillsAsHuman;

		public int DeathsAsZombie;

		public int DeathsAsHuman;
	}

	class ZombieModeKillTracker
	{
		protected Dictionary<String, ZombieModeKillTrackerKills> Kills = new Dictionary<String, ZombieModeKillTrackerKills>();

		protected int ZombiesKilled = 0;

		protected int HumansKilled = 0;

		public void HumanKilled(String KillerName, String VictimName)
		{
			ZombieModeKillTrackerKills Killer = Kills[KillerName];
			Killer.KillsAsZombie++;

			ZombieModeKillTrackerKills Victim = Kills[VictimName];
			Victim.DeathsAsHuman++;

			HumansKilled++;
		}

		public void ZombieKilled(String KillerName, String VictimName)
		{
			ZombieModeKillTrackerKills Killer = Kills[KillerName];
			Killer.KillsAsHuman++;

			ZombieModeKillTrackerKills Victim = Kills[VictimName];
			Victim.DeathsAsZombie++;

			ZombiesKilled++;
		}

		protected Boolean PlayerExists(String PlayerName)
		{
			return Kills.ContainsKey(PlayerName);
		}

		public void AddPlayer(String PlayerName)
		{
			if (!PlayerExists(PlayerName))
				Kills.Add(PlayerName, new ZombieModeKillTrackerKills());
		}

		public void RemovePlayer(String PlayerName)
		{
			if (!PlayerExists(PlayerName))
				return;

			Kills.Remove(PlayerName);
		}

		public int GetZombiesKilled()
		{
			return ZombiesKilled;
		}

		public int GetHumansKilled()
		{
			return HumansKilled;
		}

		public int GetPlayerHumanDeathCount(String PlayerName)
		{
			return Kills[PlayerName].DeathsAsHuman;
		}

		public void ResetPerMatch()
		{
			HumansKilled = 0;
			ZombiesKilled = 0;
			
			foreach (String key in Kills.Keys)
			{
				ZombieModeKillTrackerKills Tracker = Kills[key];
				Tracker.KillsAsZombie = 0;
				Tracker.KillsAsHuman = 0;
				Tracker.DeathsAsZombie = 0;
				Tracker.DeathsAsHuman = 0;
			}
		}
		
		public void ResetPerRound()
		{
			ResetPerMatch();
		}

	}
	
	class APlayerState
	{
		// A bunch of counters and flags
		
		public int WelcomeCount = 0;
		
		public int SpawnCount = 0;
	}

	class ZombieModePlayerState
	{
		protected Dictionary<String, APlayerState> AllPlayerStates = new Dictionary<String, APlayerState>();
		
		public void AddPlayer(String soldierName)
		{
			if (AllPlayerStates.ContainsKey(soldierName)) return;
			AllPlayerStates[soldierName] = new APlayerState();
		}

		public int GetWelcomeCount(String soldierName)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			return AllPlayerStates[soldierName].WelcomeCount;
		}
		
		public void SetWelcomeCount(String soldierName, int n)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			AllPlayerStates[soldierName].WelcomeCount = n;
		}
		
		public int GetSpawnCount(String soldierName)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			return AllPlayerStates[soldierName].SpawnCount;
		}
		
		public void SetSpawnCount(String soldierName, int n)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			AllPlayerStates[soldierName].SpawnCount = n;
		}
		
		public void ResetPerMatch()
		{
			foreach (String key in AllPlayerStates.Keys)
			{
				SetSpawnCount(key, 0);
			}
		}
		
		public void ResetPerRound()
		{
			ResetPerMatch();
			
			foreach (String key in AllPlayerStates.Keys)
			{
				SetSpawnCount(key, 0);
			}
		}

		public void ClearAll()
		{
			AllPlayerStates.Clear();
		}
	}
	
	class SynchronizedNumbers
	{
		public int IntVal = 0;
		public double DoubleVal = 0;
	}
}


