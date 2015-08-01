using System;
using SimpleAuth;

namespace Amazon.CloudDrive
{
	public class CloudEndpointResponse : ApiResponse
	{
		public bool CustomerExists { get; set; }
		public string ContentUrl { get; set; }
		public string MetadataUrl { get; set; }
	}
}

