using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Amazon
{
	public class AmazonAuthenticator : Authenticator
	{
		public override string BaseUrl => "https://www.amazon.com/ap/oa?";
		public override Uri RedirectUrl => new Uri("http://localhost");
		public override async Task<Dictionary<string, string>> GetTokenPostData(string clientSecret)
		{
			var data = await base.GetTokenPostData(clientSecret);
			data["redirect_uri"] = RedirectUrl.AbsoluteUri;
			return data;
		}
	}
}
