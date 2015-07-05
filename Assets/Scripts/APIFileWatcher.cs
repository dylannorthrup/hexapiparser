using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace HexAPIParser
{
	public class APIFileWatcher : MonoBehaviour
	{

		public string filePath = "/Users/dnorthrup/temp/hex/dr/api.data";
		public string priceURL = "http://doc-x.net/hex/all_prices.txt";
		public float freq = 0.2f;
		public GUIStyle DataStyle;
		public GUIStyle GuidanceStyle;
		public Rect QuitButtonRect;
		public Rect APIDataRect;
		public Rect GuidanceRect;
		public Rect DebugButtonRect;
		public Rect DumpPricesButtonRect;
		public Rect DumpCollectionButtonRect;
		public Rect SaveCollectionToCache;
		public string collectionCache = "/Users/dnorthrup/temp/hex/dr/collection.cache";
		private DateTime lastAPIFileWrite;
		private Guidance g;
		private string prevAPIText = "";
		private string APIText = "";
		public string guidance = "Initial Guidance Empty";
		private string oldGuidance = "";
		private int messagesReceived = 0;
		private bool readyForBusiness = false;

		// This is used to check for updates to the API Data file
		IEnumerator CheckFile ()
		{
			float timer = 0; // This is to lower frequency of check
			string url = "file://" + filePath;
			while (true) {
				// Make sure we've done all our setup
				if (readyForBusiness) {
					// Check if the file status has been written to at all. If not, continue
					FileInfo fInfo = new FileInfo(filePath);
					DateTime recentAPIFileWriteTime = fInfo.LastWriteTime;
					if(lastAPIFileWrite.Equals(null)) {
						lastAPIFileWrite = recentAPIFileWriteTime;
					} else {
						// We've done stuff before, so we can compare fInfo details.
						// See if the last file write time is later than the current file write time
						if(lastAPIFileWrite > recentAPIFileWriteTime) {
							continue;
						} else {
							lastAPIFileWrite = recentAPIFileWriteTime;
						}
					}
					WWW www = new WWW (url);
				yield return www;
				APIText = www.text;
				// If we got something new, go into action!
				if (! APIText.Equals (prevAPIText)) {
					prevAPIText = APIText;
					messagesReceived++;
					guidance = g.getSome (APIText);
//					doStuffWithApiText (APIText);
				}
				// Check for www.error
				// Parse and check for info
				while (timer < freq) { // 2Hz frequency
					timer += Time.deltaTime;
					yield return null;
				}
				timer = 0f;
				yield return null;
				} else {
					Debug.Log ("Waiting for 3 secs");
					yield return new WaitForSeconds(1.0f);
				}
			}
		}

		// A little helper method to schedule the writing of a Collection event (since Guidance is
		// not derived from MonoBehavior, but this is)
		// We schedule a write for 10 seconds into the future. We coordinate with Guidance g to
		// say if writes are pending and to signal completion
		// Workflow for this
		// - Collection Event is noticed
		// - g.collectionContents is set to the value that needs to be written
		// - g.collectionCachePending set to true
		// - onGui checks for g.pendingWrite and, if it's true, scheduleCollectionCacheWrite is called 
		//    - set g.collectionCachePending = false
		//    - cancel any pending scheduled calles to g.saveCollectin
		//    - schedule a call to g.saveCollection in 10 secs
		// - after 10 sec delay, g.saveCollection takes value of g.collectionContents and writes it to collection.cache file
		void scheduleCollectionCacheWrite() {
			g.collectionCachePending = false;
			CancelInvoke("gSaveCollection");
			Invoke("gSaveCollection", 10);
		}

		// Since I need to invoke locally defined methods, we'll use this intermediary to trigger the
		// g.saveCollection
		void gSaveCollection() {
			g.saveCollection();
		}

		void OnGUI ()
		{
			// We don't want to go over the bounds of what we can display in the 
			string displayedText;
			if(APIText.Length > 1200) {
			 displayedText = APIText.Substring(0,1200);
			} else {
				displayedText = APIText;
			}
			GUI.Label (APIDataRect, "API Data [" + messagesReceived + "]: " + displayedText, DataStyle);
			GUI.Label (GuidanceRect, guidance, GuidanceStyle);
			if (! guidance.Equals (oldGuidance)) {
				oldGuidance = guidance;
				Debug.Log (guidance);
			}
			if (GUI.Button (QuitButtonRect, "Exit Program")) {
				Debug.Log ("Exiting program");
				Application.Quit();
			}
			if (GUI.Button (DumpPricesButtonRect, "Dump Price Data")) {
				string data = g.dumpPrices ();
				Debug.Log (data);
			}
			if (GUI.Button (DumpCollectionButtonRect, "Dump Collection Info")) {
				string data = g.dumpCollection ();
				Debug.Log (data);
			}
			if (GUI.Button (DebugButtonRect, "Dump Text to Debug")) {
				Debug.Log (guidance);
			}
			if (GUI.Button (SaveCollectionToCache, "Save Collection to Cache File")) {
				Debug.Log ("Saving to " + collectionCache);
				g.saveCollectionToCache (collectionCache);
			}
			if (g.collectionCachePending == true) {
				scheduleCollectionCacheWrite();
			}
		}
    
		// Wait on a web request
		IEnumerator WaitForRequest (WWW www, Guidance g)
		{
			yield return www;
			if (www.error == null) {
				Debug.Log ("WWW ok: ");
				Debug.Log ("Passing www.data on to guidance loading");
				g.loadPrices (www.text);
				Debug.Log ("Loaded www.data");
			} else {
				Debug.Log ("WWW Error: " + www.error);
			}
			g.loadCollection (collectionCache);
			readyForBusiness = true;
		}

		// Use this for initialization
		void Start ()
		{
			// Create my Guidance object here that we'll use over and over later on
			g = ScriptableObject.CreateInstance ("Guidance") as HexAPIParser.Guidance;
			// Load prices from remote URL
			WWW www = new WWW (priceURL);
			StartCoroutine (WaitForRequest (www, g));
			// Then move on to checking the file
			StartCoroutine (CheckFile ());
		}

	}
}