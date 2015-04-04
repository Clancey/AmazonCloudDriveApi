using System;

namespace Amazon.CloudDrive
{
	public class CloudDriveDesktop
	{
		[Newtonsoft.Json.JsonProperty ("pathClient")]
		public string ClientPath { get; set; }

		[Newtonsoft.Json.JsonProperty ("pathServer")]
		public string ServerPath { get; set; }
	}
}

