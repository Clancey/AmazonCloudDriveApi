using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Amazon
{
	public class Account
	{
		public Account ()
		{
			Scope = new string[0];
			UserData = new Dictionary<string, string> ();
		}

		public string Identifier { get; set; }

		string tokenType;
		public string TokenType {
			get {
				return tokenType;
			}
			set {
				if (value == "bearer")
					value = "Bearer";
				tokenType = value;
			}
		}

		public string Token { get; set; }

		public string RefreshToken { get; set; }

		public long ExpiresIn { get; set; }
		//UTC Datetime created
		public DateTime Created { get; set; }

		public string[] Scope { get; set; }

		public string ClientId { get; set; }

		public Dictionary<string,string> UserData { get; set; }

		public bool IsValid ()
		{
			if (string.IsNullOrWhiteSpace (Token))
				return false;
			var expireTime = Created.AddSeconds (ExpiresIn);
			return expireTime > DateTime.UtcNow;
		}
	}
}

