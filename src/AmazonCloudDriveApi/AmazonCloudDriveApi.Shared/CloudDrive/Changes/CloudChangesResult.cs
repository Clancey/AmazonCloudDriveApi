using System;
using System.Collections.Generic;
using SimpleAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.CloudDrive
{
	public class CloudChangesResult : ApiResponse
	{
		public bool Reset { get; set; }

		public string Checkpoint { get; set; }

		public List<JObject> Nodes { get; set; }

		public int StatusCode { get; set; }

        public bool End { get; set; }
	}
}

