using System;
using SimpleAuth;

namespace Amazon.CloudDrive
{
	public class CloudUsageResponse : ApiResponse
	{
		public DateTime LastCalculated { get; set; }
		public CloudDataUsage Other { get; set; }
		public CloudDataUsage Doc { get; set; }
		public CloudDataUsage Photo { get; set; }
		public CloudDataUsage Video { get; set; }
	}
}

