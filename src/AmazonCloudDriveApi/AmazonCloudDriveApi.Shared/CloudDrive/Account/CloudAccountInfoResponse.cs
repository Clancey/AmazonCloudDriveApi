using System;
using SimpleAuth;

namespace Amazon.CloudDrive
{
	public class CloudAccountInfoResponse : ApiResponse
	{
		public string TermsOfUse { get; set; }
		public string Status { get; set; }
	}
}

