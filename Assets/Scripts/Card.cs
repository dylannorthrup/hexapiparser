using UnityEngine;
using System.Collections;
using System;
using System.Text.RegularExpressions;

namespace HexAPIParser
{
	public class Card
	{
		public string name { get; set; }

		public int plat { get; set; }

		public int gold { get; set; }

		public int qty { get; set; }

		public Card ()
		{
		}

		public Card (string n)
		{
			name = n;
			plat = 0;
			gold = 0;
			qty = 0;
		}

		public Card (string n, int g, int p)
		{
			name = n;
			plat = p;
			gold = g;
			qty = 0;
		}

		public override string ToString ()
		{
			string r = "'" + this.name + "' [Qty: " + this.qty + "] - " + this.plat + "p and " + this.gold + "g";
			return r;
		}

	}
}
