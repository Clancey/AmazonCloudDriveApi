using System;
using System.Collections.Generic;

namespace Amazon.CloudDrive
{

	public class CloudNode
	{
		public List<string> Parents { get; set; }

		public NodeType Kind { get; set; }

		public int Version { get; set; }

		public string Id { get; set; }

		public string Name { get; set; }

		public string CreatedDate { get; set; }

		public string eTagResponse { get; set; }

		public CloudNodeStatus Status { get; set; }

		public List<string> Labels { get; set; }

		public bool Restricted { get; set; }

		public string ModifiedDate { get; set; }

		public string CreatedBy { get; set; }

		public bool IsShared { get; set; }

		public string TempLink { get; set; }

		public ContentNodeProperties ContentProperties { get; set; }

		public NodeProperties Properties { get; set; }

		//TODO: Unknown data type
		public List<object> Transforms { get; set; }

		public ParentMap ParentMap { get; set; }
	}
}

