using System;
using System.Collections.Generic;
using SimpleAuth;

namespace Amazon.CloudDrive
{
	public class CloudChangesResult : ApiResponse
	{
		public bool Reset { get; set; }

		public string Checkpoint { get; set; }

		public List<CloudNode> Nodes { get; set; }

		public int StatusCode { get; set; }
	}
}

