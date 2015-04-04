using System;

namespace Amazon.CloudDrive
{
	public class CloudDriveFilesProperties
	{
		public string PayerId { get; set; }

		public string Migrated { get; set; }

		public string MetadataVersion { get; set; }

		public DateTime LastUpdateDate { get; set; }

		[Newtonsoft.Json.JsonProperty ("s3_isEncrypted")]
		public string IsEncrypted { get; set; }

		[Newtonsoft.Json.JsonProperty ("s3_storageType")]
		public string StorageType { get; set; }

		public string BlockedFromSharing { get; set; }

		public string Asin { get; set; }
	}
}

