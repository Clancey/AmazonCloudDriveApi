using System;
using Newtonsoft.Json;

namespace Amazon
{
	public class ApiResponse
	{
		public string Error { get; set;}
		[JsonProperty("error_description")]
		public string ErrorDescription {get;set;}

		public bool HasError
		{
			get{ return !Sucess; }
		}
		public bool Sucess
		{
			get{ return string.IsNullOrWhiteSpace (Error); }
		}
	}
}

