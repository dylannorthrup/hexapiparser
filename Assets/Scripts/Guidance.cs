using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace HexAPIParser
{
	public class Guidance : ScriptableObject
	{
		private static DateTime beginGame;
		private static DateTime endGame;
		public static Dictionary<string, Card> cards;
		private string specificGuidance;
		public bool collectionCachePending = false;
		private List<JSONObject> collList;
		private string collectionCacheFile;

		// Something we can use to see the prices we use
		public string dumpPrices ()
		{
			string retStr = "";
			foreach (Card c in cards.Values) {
				string price_info = c.qty + " - '" + c.name + "' - " + c.plat + "p - " + c.gold + "g\n";
				retStr += price_info;
			}
			return retStr;
		}
	
		// Same thing, but for the collection. Skip things we have '0' of
		public string dumpCollection ()
		{
			string retStr = "";
			foreach (Card c in cards.Values) {
				if (c.qty == 0) {
					continue;
				}
				retStr += c + "\n";
			}
			return retStr;
		}

		// Load collection information from collection cache file
		public void loadCollection (string collectionCache)
		{
			if (! File.Exists (collectionCache)) {
				string path = Application.dataPath;
				Debug.Log ("Collection Cache '" + collectionCache + "' does not exist in my application data path of " + path);
				return;
			}
			// Save this info for later
			collectionCacheFile = collectionCache;
			string collectionLines = File.ReadAllText (collectionCache);
			Regex pattern = new Regex (@"^(\d+)\s:\s(.*?)\s*$");
			foreach (string line in collectionLines.Split("\n"[0])) {
				// Example line:
				// 5 : Adamanthian Scrivener
				if (pattern.IsMatch (line)) {
					Match match = pattern.Match (line);
					int qty = IntFromString (match.Groups [1].Value);
					string cardName = match.Groups [2].Value;
//					Debug.Log ("Checking if cards contains the key for " + cardName + " to add " + qty + " copies");
					if (cards.ContainsKey (cardName)) {
//						Debug.Log ("Adding " + qty + " copies of " + cardName);
						cards [cardName].qty = qty;
					} else {
//						Debug.Log ("cards Dict has " + cards.Count + " entries, but none for our card. Creating new card for " + cardName + " and adding " + qty + " copies of it");
						Card c = new Card (cardName);
						c.qty = qty;
						cards.Add (cardName, c);
					}
				}
			}
		}

		// Save current collection information to collection cache file
		// We only save items we have 1 or more of
		public void saveCollectionToCache (string collectionCache)
		{
			string collectionOutputStr = "";
			foreach (Card c in cards.Values) {
				if (c.qty == 0) {
					continue;
				}
				string price_info = c.qty + " : " + c.name + "\n";
				collectionOutputStr += price_info;
			}
			File.WriteAllText (collectionCache, collectionOutputStr);
		}

		// Override this so we can call it without passing a file
		public void saveCollectionToCache() {
			if(String.IsNullOrEmpty(collectionCacheFile)) {
				Debug.Log ("collectionCacheFile is empty, so I cannot save info to a file");
				return;
			}
			saveCollectionToCache(collectionCacheFile);
		}

		// Take the prices we get from the web, turn them into Card objects and add them to our cards Dict
		public void loadPrices (string priceContents)
		{
			Regex pattern = new Regex (@"^(.*)\s+\.\.\. (\d+) PLATINUM.*\.\.\.\s+(\d+) GOLD.*");

			if (cards == null) {
				cards = new Dictionary<string, Card> ();  // Shut up error bits
			}
			string cname;
			int plat;
			int gold;
			foreach (string line in priceContents.Split("\n"[0])) {
				// Example Data:
				// Accursed Jerkin ... 1 PLATINUM [23 auctions] ... 247 GOLD [50 auctions]
				if (pattern.IsMatch (line)) {
					Match match = pattern.Match (line);
					cname = match.Groups [1].Value;
					plat = IntFromString (match.Groups [2].Value);
					gold = IntFromString (match.Groups [3].Value);
				} else {
					Debug.Log ("Did not match line " + line);
					continue;
				}
				if (cname == "" || cname == null) {
					continue;
				}
        
				if (cards.ContainsKey (cname)) {
					cards [cname].gold = gold;
					cards [cname].plat = plat;
				} else {
					Card c = new Card (cname, gold, plat);
					cards.Add (c.name, c);
				}
				// Something in here to make sure we have a least 1 of each Booster Pack for
				// price computations
				if (cname.Contains("Booster Pack")){
					cards [cname].qty = 1;
				}
			}
		}

		// Get some guidance on what to do next depending on what API message we got
		public string getSome (string data)
		{
			string retValue;  // What we'll return to the calling folks

			// Take data and parse it as a JSON object
			JSONObject js = new JSONObject (data);
			string messageType = "" + js.list [0];
			JSONObject goodBits = js.list [2];
			messageType = messageType.Replace ("\"", "");  // Get rid of pesky quotes
			switch (messageType) {
			case "GameStarted":
				startTheGame ();
				break;
			case "GameEnded":
				endTheGame (goodBits);
				break;
			case "DraftPack":
				draftPack (js);
				break;
			case "DaraftCardPicked":
			case "DraftCardPicked":
				pickDraftCard (js);
				break;
			case "Collection":
				collectionEvent (js);
				break;
			case "SaveDeck":
				saveDeck (js);
				break;
			case "Logout":
				logoutEvent(js);
				break;
			default:
				specificGuidance = "Unknown Message Type: " + messageType;
				Debug.Log ("Unknown Message Type: " + messageType);
				break;
			}
			retValue = "RETURN: JSON object has " + js.Count + " fields";
			retValue = retValue + " -- " + js.list [0];
			retValue = retValue + " " + specificGuidance;
			return specificGuidance;
		}

		// Put up a message saying "Buh Bye"
		private void logoutEvent(JSONObject js) {
			string player = js.list[1].str;
			specificGuidance = "Logging out player " + player + ". Hope to see you again soon!";
		}

		// Update the collection information
		// Flip boolean to say "Hey, we need a collection cache scheduled"
		// Then update the contents with the new bits
		private void collectionEvent (JSONObject js)
		{
			// Example line:
			// ["Collection","Dylan",["Alwyn", "Alwyn","Burn","Burn","Burn",...,"Zoltog"]]
			specificGuidance = "Processing Collection Event";
			collectionCachePending = true;
			collList = js.list [2].list;
			Debug.Log ("Added collection to collList ");
		}

		// We've already got a dumpCollection method that takes a string
		// What we need to do here is take the collectionContents, parse that into a 
		// proper string with numbers and card names, then call dumpCollection with
		// that string.
		public void saveCollection ()
		{
			Debug.Log ("Guidance.saveCollection called and first card is " + collList [0]);
			Dictionary<string, int> collDict = new Dictionary<string, int> ();
			foreach (JSONObject jo in collList) {
				if (jo.IsString) {
					string jos = jo.str;
					// Going to strip out ',' characters from names, because they aren't in the price data
					jos = jos.Replace(",", "");
					// Also, we skip the standard shards
					if( Regex.IsMatch(jos, @"^(Blood|Ruby|Sapphire|Diamond|Wild) Shard$") ){
						continue;
					}
					if (collDict.ContainsKey (jos)) {
						collDict [jos] += 1;
					} else {
						collDict [jos] = 1;
					}
				}
			}
			// We want to zero out the values for all the cards in the cards array so we don't
			// accidentally think we have cards that we don't
			foreach (KeyValuePair<string, Card> entry in cards) { 
				//				Debug.Log ("Setting qty for " + entry.Key + " to 0");
				entry.Value.qty = 0;
			}
			// Now, update the cards array with actual quantities
			foreach (KeyValuePair<string, int> entry in collDict) {
				if (cards.ContainsKey (entry.Key)) {
					Card c = cards [entry.Key];
					c.qty = entry.Value;
				} else {
					Debug.Log("DID NOT FIND " + entry.Key + " IN CARDS ARRAY even though it was in collDict for some reason");
				}
			}
			// And, now that the cards array's updated, go ahead and save that to the collection.cache file
			saveCollectionToCache();
		}

		// Do stuff based on the card you drafted
		private void pickDraftCard (JSONObject js)
		{
			// Example line:
			// ["DaraftCardPicked","Dylan",["Xentoth's Inquisitor"]]
			string cardName = js.list [2].list [0].str;
			Card c = cards [cardName];
			string info = "Drafted card '" + cardName + "' : " + c;
			specificGuidance = info;
		}
		
		// Handle deck saving
		private void saveDeck (JSONObject js)
		{
			// Example line:
			// ["SaveDeck","Dylan",["_PVE [BLOOD] Orcs","Bunoshi the Ruthless ",[["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Blood Shard"],["Darkspire Priestess"],["Darkspire Priestess"],["Darkspire Priestess"],["Claw of the Mountain God"],["Infernal Professor"],["Infernal Professor"],["Infernal Professor"],["Claw of the Mountain God"],["Claw of the Mountain God"],["Darkspire Priestess"],["Fang of the Mountain God"],["Fang of the Mountain God"],["Fang of the Mountain God"],["Fang of the Mountain God"],["Infernal Professor"],["Fury of the Mountain God"],["Giant Mosquito"],["Fury of the Mountain God"],["Giant Mosquito"],["Darkspire Tyrant"],["Fury of the Mountain God"],["Darkspire Tyrant"],["Giant Mosquito"],["Giant Mosquito"],["Fury of the Mountain God"],["Gortezuma, High Cleric"],["Darkspire Punisher"],["Darkspire Punisher"],["Darkspire Punisher"],["Darkspire Punisher"],["Gortezuma, High Cleric"],["Crackling Vortex"],["Gortezuma, High Cleric"],["Crackling Vortex"],["Fertile Engorger"],["Fertile Engorger"],["Crackling Vortex"],["Crackling Vortex"],["Fertile Engorger"],["Fertile Engorger"]],[]]]
			Debug.Log ("Saving deck");
			string info = "Saved deck '" + js.list [2].list [0].str + "' with Champion of " + js.list [2].list [1].str;
			specificGuidance = info;
		}

		// Handle draft pack.
		private void draftPack (JSONObject js)
		{
			// Example line:
			// ["DraftPack","Dylan",["Elite Battle Tech","Infiltrator Bot", ... , "Experimental War Hulk"]]
			Debug.Log ("Got a draft pack");

			string currentLeastQty = "";
			string currentMostGold = "";
			string currentMostPlat = "";

			// DONE:
			// - Figure out which card we have the least of
			// - Figure out which card is the most valuable in terms of gold
			// - Figure out which card is the most valuable in terms of plat
			// Longer term...
			// - If last pick,
			//   - calculate total value of pack picks
			//   - add that value to total session value
			//   - Calculate pack profit by comparing pack value to calculated booster value
			// - If first pick, reset pack value
			// - Keep track of value through the session
			specificGuidance = "DraftPack: ";
			JSONObject packCards = js.list [2];
			foreach (JSONObject jCard in packCards.list) {
				if (jCard.IsString) {
					string jcName = "" + jCard;
					string debugOut = "Doing comparision for " + jcName + "\n";
					currentLeastQty = compareCards (currentLeastQty, jCard.str, "qty");
					debugOut += "CLQ: " + currentLeastQty + "\n";
					currentMostGold = compareCards (currentMostGold, jCard.str, "gold");
					debugOut += "CLG: " + currentMostGold + "\n";
					currentMostPlat = compareCards (currentMostPlat, jCard.str, "plat");
					debugOut += "CLP: " + currentMostPlat + "\n";
					Debug.Log (debugOut);
				}
			}
			// Now that we've got our stuff, let's add it to the specificGuidance
			Card c = cards [currentMostGold];
			specificGuidance += "\nWorth the most Gold: " + c; 
			c = cards [currentMostPlat];
			specificGuidance += "\nWorth the most Plat: " + c; 
			c = cards [currentLeastQty];
			specificGuidance += "\nFewest owned: " + c; 
		}

		// Do card comparisons for qty, gold and plat
		private string compareCards (string c1, string c2, string comparison)
		{
			// Shortcut for the first case where "currentMost*" is blank
			if (c1.Equals ("")) {
				return c2;
			}
			// Now, let's do comparing
			Card first = cards [c1];
			Card second = cards [c2];
			switch (comparison) {
			case "qty":
				if (first.qty < second.qty) {
					return c1;
				}
				break;
			case "gold":
				if (first.gold > second.gold) {
					return c1;
				}
				break;
			case "plat":
				if (first.plat > second.plat) {
					return c1;
				}
				break;
			default:
				return c1;
			}
			// If we drop through to here, we're returning the second card
			return c2;

		}

		private string massageCardName (string cName)
		{
			cName = cName.Replace ("\"", "");
			return cName;
		}

		// Game ended. Figure out how long the match lasted and congratulate or console the player as appropriate
		private void endTheGame (JSONObject js)
		{
			endGame = DateTime.Now;
			string retStr = "";
			retStr += "Ending game at " + endGame.ToString ();
			retStr += "\nBegan game at " + beginGame.ToString ();
			TimeSpan gameTime = endGame - beginGame;
			retStr += "\n" + timeSpanToString (gameTime);
			// Now, let's congratulate or comisserate depending on the game outcome
			string outcome = js.list [0].list [1].str;
			string opponent = js.list [1].list [0].str;
			if (outcome.Equals ("Won")) {
				retStr += "\nCongratulations on your win against " + opponent + "!";
			} else {
				retStr += "\nCondolences on your loss to " + opponent + ".";
			}

			specificGuidance = retStr;
		}

		// Game began. Record the time for later reporting
		private void startTheGame ()
		{
			beginGame = DateTime.Now;
			string retStr = "Starting game at " + beginGame.ToString ();
			specificGuidance = retStr;
		}
    
		// Takes a TimeSpan and prints it out all purty like
		// TODO: This has a +/- 1 sec difference between what's reported by TimeSpan and what the actual difference is between
		// the two DateTime objects. *sigh*
		private string timeSpanToString (TimeSpan tm)
		{
			int secs = (int)Math.Round (tm.TotalSeconds);         // Start off with seconds and build down from there
			int days = secs / 86400;       // Number of seconds in a day = 86400
			secs = secs - (86400 * days);  // Decrement secs by the number of days (likely 0)
			int hours = secs / 3600;       // Number of seconds in an hour = 3600
			secs = secs - (3600 * hours);  // Do the same with hours that we did with days
			int mins = secs / 60;          // Number of seconds in a minute = 60
			secs = secs - (60 * mins);     // And, lastly, get rid of minutes
			string timeElapsed = "Time elapsed: ";
			if (days > 0) {
				timeElapsed += days + " days, ";
			}
			if (hours > 0) {
				timeElapsed += hours + " hours, "; 
			}
			timeElapsed += mins + " minutes, ";
			timeElapsed += secs + " seconds ";
			return timeElapsed;
		}

		// Utility method to get an int from a string
		public static int IntFromString (string s)
		{
			int r = 0;
			for (int i=0; i < s.Length; i++) {
				char l = s [i];
				r = 10 * r + (l - 48);
			}
			return r;
		}
    
	}
}